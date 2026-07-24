# F-SE-002 — PII via LLM-Args im Serilog-File

> **Schweregrad:** High
> **Dimension:** 2 — Sicherheit
> **Datei:** `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs:19, 28, 37`
> **Querverweise:** F-MC-001 (Tool-Description-Qualität), F-AR-002 (Logger-Injection in Core)

## Problem

Die drei MCP-Tools loggen via `ILogger<DocsMcpTools>.LogInformation(...)` ihre
Argumente ungekürzt und ungefiltert in die Serilog-Datei:

```csharp
logger.LogInformation("list_children(parentSlug={ParentSlug})", parentSlug);
logger.LogInformation("search_docs(query={Query})", query);
logger.LogInformation("get_doc(slug={Slug})", slug);
```

Serilog schreibt in `AppContext.BaseDirectory/Logs/knowhowtoai-<datum>.log` mit
`RetainedFileCountLimit = 14` Tagen Retention. Andere Apps auf demselben Rechner
mit Lesezugriff auf das User-Account-Verzeichnis können die Logs lesen.

## Vektoren

### Vektor 1 — PII via `query`
LLM-User fragt: "Wer hat Personalnummer 4711?" → LLM ruft
`search_docs(query="personalnummer 4711")` → die Suchanfrage enthält die
Personalnummer im Klartext → landet im Log.

Wenn andere Apps (z.B. Log-Aggregation, Support-Diagnose-Tools) die Logs lesen,
wird die PII gestreut. Insbesondere wenn Logs an einen zentralen Syslog-Server
gehen (was in größeren Setups typisch ist).

### Vektor 2 — Klartext-Indikatoren via `slug`
Slugs sind nach Design kryptisch (`a-z0-9-`), aber: wenn die Doku-Bibliothek
sensible Inhalte hat, kann der Slug selbst ein Indikator sein:
`hr-kuendigungsprozess`, `ceo-privat-jet-nummern`, `kunden-xyz-vertragsdetails`.
Solche Slugs sind informativ, auch wenn der Inhalt geschützt ist.

### Vektor 3 — Lange Strings = Log-Bloat
`query` kann theoretisch mehrere KB lang sein (F-SE-001 zeigt den DoS-Vektor).
Auch ohne PII: das Log-File enthält dann mehrere KB reine Eingabe. Mit
`RetainedFileCountLimit = 14` Tagen sind das 14 × N KB Log-Größe nur für
eine einzige Suche.

## Aktuelle Mitigations

- `MinimumLevel: "Information"` (Default) → der Log-Eintrag wird geschrieben
- `RetainedFileCountLimit: 14` → Logs werden nach 14 Tagen rotiert
- `shared: true` (Serilog-Option) → Multi-Process-safe

Aber: **kein** PII-Filter, **keine** Längen-Triggerung, **kein** Hashing.

## Fix

### Variante A — Längen-Truncation + Hash für Korrelation

```csharp
private static string Truncate(string? value, int maxLength = 80) =>
    value is null ? "<null>" :
    value.Length <= maxLength ? value : value[..maxLength] + $"…(+{value.Length - maxLength} chars)";

private static int Hash(string? value) => value is null ? 0 : value.GetHashCode();

// In DocsMcpTools:
logger.LogInformation("list_children(parentSlugHash={Hash}, parentSlugLength={Length})",
    Hash(parentSlug), parentSlug?.Length ?? 0);
logger.LogInformation("search_docs(queryHash={Hash}, queryLength={Length})",
    Hash(query), query.Length);
logger.LogInformation("get_doc(slugHash={Hash}, slugLength={Length})",
    Hash(slug), slug?.Length ?? 0);
```

**Vorteile:**
- Korrelation möglich über `queryHash` (zwei Aufrufe mit identischem `query`
  haben gleichen Hash)
- Keine PII im Log
- Kein Log-Bloat

**Nachteile:**
- `GetHashCode` ist nicht kryptographisch (zwei Strings können denselben Hash
  haben) → für Korrelation OK, für Identifikation NICHT
- Wenn Korrelation nicht gebraucht wird, könnte auch nur die Länge geloggt werden

### Variante B — Längen-Triggerung (Hash) + opt-in Klartext

```csharp
// Neue Option in appsettings.json: KnowHowToAi.Tools.LogArgsAsPlaintext (bool, default false)

private void LogArg(string name, string? value)
{
    if (options.Tools.LogArgsAsPlaintext)
    {
        logger.LogInformation("arg {Name}={Value}", name, Truncate(value, 200));
    }
    else
    {
        logger.LogInformation("arg {Name} (hash={Hash}, length={Length})",
            name, Hash(value), value?.Length ?? 0);
    }
}
```

**Vorteile:**
- Default sicher (kein Klartext)
- User kann explizit aktivieren, wenn Debugging gebraucht wird
- Klare Audit-Spur, weil die Einstellung in `appsettings.json` sichtbar ist

**Empfehlung:** Variante A (immer Hash, kein Opt-in). Einfacher, sicherer, weniger
Konfigurations-Ballast.

## Tests

```csharp
[Fact]
public void Truncate_ShortValue_ReturnsUnchanged()
{
    Assert.Equal("foo", Truncate("foo", 80));
}

[Fact]
public void Truncate_LongValue_TruncatesWithSuffix()
{
    var result = Truncate(new string('a', 100), 80);
    Assert.Equal(80 + 5, result.Length);  // 80 chars + "...(+20 chars)"
    Assert.Contains("+20 chars", result);
}

[Fact]
public void Truncate_NullValue_ReturnsNullMarker()
{
    Assert.Equal("<null>", Truncate(null));
}
```

Da `DocsMcpTools` per `02-testing.mdc` *nicht* separat testpflichtig ist (reine
Delegation), sind diese Tests *nice-to-have*, nicht zwingend. Die Logik ist
trivial, ein Fehler hier ist nicht kritisch.

## Aufwand

- ~20 Minuten Code
- ~5 Minuten Doku (F-MC-001 ergänzen: "Query-Werte werden im Log nur als Hash
  + Länge geloggt, nicht im Klartext")
- Insgesamt: ~25 Minuten, 1 Commit

## Risiko

Niedrig. Die Änderung ist *rein additiv-defensiv*: bestehende Logs verlieren
die Klartext-Argumente, aber Korrelation funktioniert weiterhin über den Hash.
Wer explizit Klartext-Logs braucht, kann auf die nächste Stufe gehen.

## Migrations-Plan

1. `Truncate`-Helper in `DocsMcpTools` (oder einem `Logging`-Helper-Modul)
2. Drei `LogInformation`-Aufrufe umstellen auf Hash + Length
3. Tool-Description (F-MC-001) anpassen
4. Bestehende Logs manuell prüfen, ob PII bereits drin ist — wenn ja, vor dem
   Deployment manuell löschen oder `RetainedFileCountLimit` auf 1 setzen
