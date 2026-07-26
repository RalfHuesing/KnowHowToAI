# Dimension 2 — Sicherheit (MCP-Attack-Surface)

> **Vergleichsbasis:** Allgemeine Best-Practices für stdio-MCP-Server, OWASP-Top-10 (insb. A03
> Injection + A04 Insecure Design), MCP-Spezifikation (Annahmen basieren auf
> `2025-11-25`/`2026-07-28` RC), sowie `.agents/rules/01-code-style.mdc` (Defense-in-Depth).
> **Methodik:** Statische Code-Analyse aller Stellen, an denen LLM-Eingaben verarbeitet oder
> ausgegeben werden. Threat-Modell: böswilliger/kompromittierter LLM-Client, böswillige Inhalte
> in Doku-Dateien, kompromittierte `appsettings.json`.
> **Nicht im Scope:** Live-Pentest, Fuzzing, oder konkrete Exploit-Demonstration. Alle Bewertungen
> sind statisch und konservativ ("kann passieren" → dokumentiert).

## Threat-Modell

| Akteur | Annahme | Vektor |
| --- | --- | --- |
| **Böswilliger LLM-Client** | MCP-Client wird kompromittiert, schickt böswillige Args | Tool-Args (`query`, `slug`, `parentSlug`) |
| **Böswilliger Doku-Autor** | Kann Dateien in den Docs-Root legen (z.B. kompromittierter Git-PR) | Front-Matter-Werte, Markdown-Links, Content |
| **Kompromittierte Config** | `appsettings.json` wird manuell verbogen oder ersetzt | `ConnectionString`, `DocumentsTableName` |
| **Angreifer mit Filesystem-Zugriff** | Andere Apps laufen mit demselben User-Account | Lesen der Logs (`Logs/*.log`) |
| **MCP-Host-Umgebung** | MCP-Host startet den Server mit reduziertem Environment | Env-Var-Lookups, Working-Directory, `AppContext.BaseDirectory` |

## Findings-Übersicht

| ID | Schwere | Titel | Datei:Zeile |
| --- | --- | --- | --- |
| [F-SE-001](#f-se-001) | **High** | `BuildLikePattern` interpoliert `query` ohne LIKE-Wildcard-Escaping — DoS-Vektor | `Sync/SqlDocumentsStore.cs:94` |
| [F-SE-006](#f-se-006) | Low | `SlugRules.FromFilePath` ohne expliziten Path-Traversal-Schutz (Defense-in-Depth) | `Documents/SlugRules.cs:22-28` |
| [F-SE-007](#f-se-007) | Low | `%COMPUTERNAME%`-Expansion ist hartcodiert; keine generische Env-Var-Expansion | `Cli/Program.cs:163-169` |
| [F-SE-008](#f-se-008) | Low | `JsonSerializer.Deserialize<List<string>>(row.Tags)!` ohne Defensive Catch (siehe F-CQ-003) | `Sync/SqlDocumentsStore.cs:111-112` |
| [F-SE-009](#f-se-009) | Low | MCP-Tool-Beschreibungen sind sehr knapp — fehlende LLM-UX-Sicherheits-Hinweise (siehe Dim 9) | `McpTools/DocsMcpTools.cs:16, 25, 34` |
| [F-SE-010](#f-se-010) | Info | `ServerInstructions` enthält das `%COMPUTERNAME%`-Workaround-Erbe implizit — keine Secrets im Output | `McpTools/DocsMcpResources.cs:11-14` |

## Detail-Findings

### F-SE-001 — `BuildLikePattern` ohne LIKE-Wildcard-Escaping

**Schweregrad:** High (DoS-Vektor + Wildcard-Smuggling)

**Beobachtung:**
`src/KnowHowToAI.Core/Sync/SqlDocumentsStore.cs:94`:
```csharp
private static string BuildLikePattern(string query) => $"%{query}%";
```

`SearchDocsAsync` baut `LIKE @Pattern` gegen vier Spalten. Das Pattern wird in `query`
direkt interpoliert. Die LLM-Eingabe `query` ist vom Client (LLM) frei wählbar.

**Vektor 1 — Wildcard-Smuggling:**
- LLM (oder ein Angreifer, der das LLM kontrolliert) schickt `query = "%"` → Pattern wird `%%%`
  → matched jede Zeile. Unschön, aber nicht kritisch.
- LLM schickt `query = "_"` → matched jede Zeile mit mindestens 1 Zeichen. Ebenso.
- LLM schickt `query = "%%%%%%%%...%"` (viele `%`) → Pattern-Länge explodiert, SQL-Server kann
  das Pattern effizient verarbeiten, aber bei großen Tabellen kann der Plan-Compiler aussteigen.

**Vektor 2 — DoS via Pattern-Länge:**
- LLM schickt `query` mit 100 KB Zeichen. Pattern wird 100 KB + 2 Zeichen. SQL-Server nimmt das
  entgegen, scannt die Tabelle, vergleicht jede Zeile mit dem 100-KB-Pattern. Bei einer Tabelle
  mit 10.000 Zeilen × 4 Spalten × 100 KB Pattern-Vergleich → mehrere Sekunden bis Minuten.
- Bei wiederholten Aufrufen in einer Schleife: Trivialer DoS gegen den lokalen SQL-Server.

**Vektor 3 — Plan-Compiler-Bombe:**
- LLM schickt `query = "(((((([%_]*[%_]*)+)+)+)+)+..."` — alternierende Wildcard-Gruppen.
  Einige SQL-Server-Versionen haben Optimizer-Heuristiken, die mit extremen Pattern-Sequenzen
  aussteigen (Timeout, Stack-Overflow im Query-Optimizer).

**Aktuelle Mitigations:**
- `LIKE` benutzt `@Pattern` als Parameter (kein klassisches SQL-Injection).
- Aber: `BuildLikePattern` ist KEIN Schutz, weil es die Wildcard-Bedeutung von `%` und `_`
  *bewusst* ausnutzt.

**Fix-Empfehlung:**
```csharp
// 1. Max-Länge prüfen (z.B. 200 Zeichen) — wirft klar definierte Exception
// 2. Wildcard-Zeichen escapen: % -> [%], _ -> [_], [ -> [[], \ -> [\]
// 3. Pattern-Builder als interne Helper-Klasse mit Unit-Tests
private const int MaxQueryLength = 200;

private static string BuildLikePattern(string query)
{
    if (query.Length > MaxQueryLength)
    {
        throw new ArgumentException(
            $"search_docs query ist {query.Length} Zeichen lang, max {MaxQueryLength}.",
            nameof(query));
    }

    var escaped = query
        .Replace(@"\", @"\\")
        .Replace("[", "[[]")
        .Replace("%", "[%]")
        .Replace("_", "[_]");
    return $"%{escaped}%";
}
```
Plus Tests:
- `BuildLikePattern_EscapesPercent`
- `BuildLikePattern_EscapesUnderscore`
- `BuildLikePattern_AllowsNormalSubstring`
- `BuildLikePattern_ThrowsOnTooLongQuery`

**Detail-Datei:** [`_findings/F-SE-001-like-wildcard-injection.md`](_findings/F-SE-001-like-wildcard-injection.md)

**Aufwand:** ~30 Minuten + Tests.

---

### F-SE-006 — `SlugRules.FromFilePath` ohne Path-Traversal-Schutz

**Schweregrad:** Low (Defense-in-Depth-Lücke, Validator fängt es ab)

**Beobachtung:**
`src/KnowHowToAI.Core/Documents/SlugRules.cs:22-28`:
```csharp
public static string FromFilePath(string docsRootPath, string filePath)
{
    var relative = Path.GetRelativePath(docsRootPath, filePath)
        .Replace(Path.DirectorySeparatorChar, '/')
        .Replace(Path.AltDirectorySeparatorChar, '/');
    return relative[..^Path.GetExtension(relative).Length];
}
```

Wenn `filePath = "/etc/passwd"` und `docsRootPath = "C:\\Daten\\Entwicklung\\Ralf\\KnowHowToAI\\demo-docs"`,
dann gibt `Path.GetRelativePath` etwas wie `../../../../etc/passwd` zurück. Der Slug wird
`../../../../etc/passwd`. `SlugRules.IsValidSegment` lehnt jedes Segment mit `.` oder `/`
außerhalb des Slug-Patterns ab → Validator-Eintrag, kein Datenleck.

**Warum trotzdem ein Finding:**
- Defense-in-Depth: Der `import`-Pfad sollte den File-Walk auf `docsRootPath` *beschränken*,
  nicht erst durch den Validator korrigieren.
- `ImportService.ReadDocuments` (Zeile 32): `Directory.EnumerateFiles(docsRootPath, "*.md",
  SearchOption.AllDirectories)` — folgt keinen Symlinks, also beschränkt auf den Root.
  Gut.
- ABER: Wenn `docsRootPath` selbst ein Symlink ist (z.B. `C:\Daten\MyDocs -> D:\ExternalDocs`),
  kann der Enumerator Dateien außerhalb des "logischen" Roots liefern.

**Fix-Empfehlung:**
In `ImportService.ReadDocuments` (oder als Helper in `SlugRules.FromFilePath`) prüfen, dass
das Resultat von `Path.GetRelativePath` *nicht* mit `..` beginnt. Falls doch, überspringen
+ loggen. Das fängt symlink-Fälle ab.

**Aufwand:** ~10 Minuten.

---

### F-SE-007 — `%COMPUTERNAME%`-Expansion ist hartcodiert

**Schweregrad:** Low (nur Dev-Setup; dokumentiert; `Environment.ExpandEnvironmentVariables`
wäre die generische Lösung, hat aber Nachteile in MCP-Host-Kontexten)

**Beobachtung:**
`src/KnowHowToAI.Cli/Program.cs:163-169` — ersetzt nur `%COMPUTERNAME%`, nicht
`%USERPROFILE%`, `%APPDATA%`, etc.

**Sicherheits-Perspektive:** Sehr niedrig. `%COMPUTERNAME%` ist die einzige in der
Config-Datei referenzierte Variable. Es gibt keinen dokumentierten Anwendungsfall für
andere. Daher kein echter Bug.

**Empfehlung:** Dokumentations-Kommentar in `appsettings.json` oder in `docs/03`, dass nur
`%COMPUTERNAME%` expandiert wird. Sonst nichts.

---

### F-SE-008 — `JsonSerializer.Deserialize` ohne Catch (siehe F-CQ-003)

Querverweis. Severity konsistent mit Dim 1.

---

### F-SE-009 — Tool-Beschreibungen fehlen Sicherheits-Hinweise

**Schweregrad:** Low (LLM-UX, nicht Security-Hardening)

Querverweis zu Dim 9. Tool-Beschreibungen sollten Hinweise geben wie "Wenn ein Slug
nicht gefunden wird, ist das kein Fehler" oder "Die Suche nutzt `LIKE`, daher kein
Ranking". Sonst ruft das LLM die Tools suboptimal auf und ggf. in Schleifen, was
Performance-Probleme verursachen kann.

---

### F-SE-010 — `ServerInstructions` enthält keine Secrets (Info)

**Beobachtung:** `ServerInstructions` ist statisch und enthält nur Tool-Namen + Pfad-Hinweise.
Kein Leak von ConnectionString, Pfaden, oder Inhalten. Sauber.

**Kein Handlungsbedarf.**

---


## Zusammenfassung Dim 2

- **6 Findings** (nach Prio D-Extraktion), davon 1 × High (PrioA), 0 × Medium, 3 × Low, 2 × Info.
- **F-SE-001** (LIKE-Injection) ist in PrioA extrahiert. F-SE-003 (Längen-Validierung), F-SE-004 (Plattform-Inkonsistenz) und F-SE-005 (Credentials-Doku-Hinweis) sind in PrioD extrahiert.
