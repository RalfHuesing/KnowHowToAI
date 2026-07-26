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
| [F-SE-002](#f-se-002) | **High** | LLM-Args werden ungekürzt und ungefiltert ins Serilog-File geschrieben — PII-Leak | `McpTools/DocsMcpTools.cs:19, 28, 37` |
| [F-SE-003](#f-se-003) | Medium | Keine Längen-Validierung der MCP-Tool-Argumente → DoS via 10MB-Slug | `McpTools/DocsMcpTools.cs:17, 26, 35` |
| [F-SE-004](#f-se-004) | Medium | `SqlIdentifierValidator` erlaubt case-mixed + `_` → Plattform-Inkonsistenz auf Linux-DB | `Sync/SqlIdentifierValidator.cs:10` |
| [F-SE-005](#f-se-005) | Medium | `ConnectionString` mit hartcodierten Credentials in committed `appsettings.json` | `Cli/appsettings.json:4` |
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

### F-SE-002 — LLM-Args ungekürzt im Log-File

**Schweregrad:** High (PII-Leak in Logs, die ggf. von anderen Apps gelesen werden)

**Beobachtung:**
`src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs:19, 28, 37`:
```csharp
logger.LogInformation("list_children(parentSlug={ParentSlug})", parentSlug);
logger.LogInformation("search_docs(query={Query})", query);
logger.LogInformation("get_doc(slug={Slug})", slug);
```

**Problem 1 — PII via `query`:**
Das LLM schickt u.U. `query = "Müller Personalnummer 4711"` (vom User gefragt: "Wer hat
Personalnummer 4711?"). Diese Suchanfrage enthält PII und landet ungekürzt im
`Logs/knowhowtoai-<datum>.log`. Wenn andere Apps auf demselben Rechner dieselben Logs
lesen (z.B. Log-Aggregation, Support-Diagnose), wird die PII gestreut.

**Problem 2 — Klartextgeheimnisse via `slug`:**
`get_doc(slug="...")` — Slugs sind nach Design kryptisch (`a-z0-9-`), aber wenn die
Doku-Bibliothek sensible Inhalte hat, kann der Slug selbst ein Indikator sein
("hr-kuendigungsprozess" als gültiger Slug wäre informativ).

**Problem 3 — Lange Strings:**
`query` kann theoretisch mehrere KB lang sein (F-SE-001 zeigt den DoS-Vektor). Das Log
enthält dann mehrere KB reine Eingabe. Log-File-Rotation macht das nicht rückgängig.

**Fix-Empfehlung:**
1. Längen-Truncation in der Log-Zeile selbst:
   ```csharp
   private static string Truncate(string? value, int maxLength = 80) =>
       value is null ? "<null>" :
       value.Length <= maxLength ? value : value[..maxLength] + $"…(+{value.Length - maxLength} chars)";
   ```
2. PII-Markierung optional (z.B. `parentSlug=…` statt Klartext, wenn `Validation.MaxContentLengthWarning > 0`
   ist das sowieso nicht möglich — also lieber: Pattern-Replacement für typische PII-Formate).
3. Besser: `Hash` loggen, nicht Klartext, wenn nur Korrelation wichtig ist:
   ```csharp
   logger.LogInformation("search_docs(queryHash={Hash}, queryLength={Length})",
       query?.GetHashCode(), query?.Length ?? 0);
   ```

**Detail-Datei:** [`_findings/F-SE-002-pii-in-logs.md`](_findings/F-SE-002-pii-in-logs.md)

**Aufwand:** ~20 Minuten.

---

### F-SE-003 — Keine Längen-Validierung der MCP-Tool-Argumente

**Schweregrad:** Medium (DoS-Vektor, niedriger als F-SE-001, weil Wirkung pro Aufruf)

**Beobachtung:**
`DocsMcpTools.ListChildrenAsync(string? parentSlug, ...)`, `SearchDocsAsync(string query, ...)`,
`GetDocAsync(string slug, ...)` — keine Längen-Validierung. Das MCP-SDK selbst hat keine
Schutzmaßnahmen.

**Vektor 1 — 10MB-Slug:**
- LLM schickt `slug = "a".repeat(10_000_000)`.
- `GetDocAsync` macht `SELECT ... WHERE slug = @Slug` — der SQL-Server parameterisiert, also
  harmlos. ABER: das MCP-Framework muss den 10-MB-String erst durch JSON-Serialisierung/-Deserialisierung
  jagen, was Speicher kostet.
- Schlimmer: In `GetDocAsync` wird `LogResponseSize` mit dem Document aufgerufen. Wenn das Document
  100 KB ist, wird es durch JSON-Serialisierung ALLER Felder gejagt → weitere 100 KB Allokation.

**Vektor 2 — `query` mit 100 KB:**
Siehe F-SE-001.

**Fix-Empfehlung:**
In `DocsMcpTools` (oder in `SqlDocumentsStore`) eine Validierungsschicht:
```csharp
private const int MaxSlugLength = 450; // SQL Server-Index-Limit
private const int MaxQueryLength = 200; // siehe F-SE-001

private static void ValidateSlug(string? slug)
{
    if (slug is not null && slug.Length > MaxSlugLength)
    {
        throw new ArgumentException(
            $"Slug ist {slug.Length} Zeichen lang, max {MaxSlugLength}.", nameof(slug));
    }
}
```
Auch `parentSlug` validieren. Fehler als Tool-Error zurückgeben (MCP-SDK-Standard), nicht als
unbehandelte Exception.

**Aufwand:** ~15 Minuten + Tests.

---

### F-SE-004 — `SqlIdentifierValidator` Plattform-Inkonsistenz

**Schweregrad:** Medium (funktioniert auf Windows-DB, kann auf Linux-DB brechen)

**Beobachtung:**
`src/KnowHowToAI.Core/Sync/SqlIdentifierValidator.cs:10`:
```csharp
private static readonly Regex Pattern = new("^[A-Za-z_][A-Za-z0-9_]{0,99}$", ...);
```

Erlaubt: Großbuchstaben, Kleinbuchstaben, Ziffern, Unterstrich. Max 100 Zeichen.

**Plattform-Verhalten:**
- **Windows-Default-Collation** (z.B. `SQL_Latin1_General_CP1_CI_AS`): case-**insensitive**.
  `MyTable` und `mytable` sind *derselbe* Identifier. Funktioniert.
- **Linux-Default-Collation** (z.B. mit `UTF8`-Collation, oder wenn explizit
  `Latin1_General_100_BIN2`): case-**sensitive**. `MyTable` und `mytable` sind
  *verschiedene* Identifier.

**Konsequenz:** Eine `appsettings.json` mit `"DocumentsTableName": "MyTable"` funktioniert
auf dem Dev-Rechner (Windows) und bricht auf einer Linux-DB-Instanz, weil:
- Die Migration erstellt `dbo.MyTable` (Windows erlaubt es, Linux erlaubt es auch).
- Aber wenn `MyTable` UND `mytable` in derselben DB existieren würden, wäre das Verhalten
  undefiniert.
- Schlimmer: Wenn eine CI-Instanz auf Linux läuft, kann eine config mit Uppercase zu
  kryptischen Fehlern führen.

**SQL Server Reserved Words:**
Die Regex erlaubt auch Identifier wie `Table`, `Select`, `From`, `User` etc. SQL Server
wirft dann "Incorrect syntax near the reserved word". `SchemaMigrator` würde beim `CREATE
TABLE dbo.User` scheitern.

**Fix-Empfehlung:**
1. Lowercase-only erzwingen: `^[a-z_][a-z0-9_]{0,99}$` — passt zu den Slug-Regeln
   (lowercase-only) und ist plattform-konsistent.
2. Optional: Liste verbotener Reserved Words prüfen (z.B. via eine `HashSet<string>` mit
   den ~50 häufigsten).
3. Konsistenz mit `SlugRules`: beide nutzen `a-z0-9-` als "sichere" Identifiers.

**Aufwand:** ~15 Minuten + Tests für Lowercase-only + Reserved-Word-Liste.

---

### F-SE-005 — `ConnectionString` mit Credentials in `appsettings.json`

**Schweregrad:** Medium (bewusst vom Projektverantwortlichen freigegeben, daher kein Critical;
aber Pattern-Risiko für den ersten produktiven Einsatz)

**Beobachtung:**
`src/KnowHowToAI.Cli/appsettings.json:4`:
```json
"ConnectionString": "Server=%COMPUTERNAME%\\MSSQLSERVER2022;Database=DemoDB;User Id=Agent;Password=Agent!;TrustServerCertificate=True;",
```

In Git committed. `docs/03-Projektstruktur-und-Konfiguration.md`, Zeile 81 dokumentiert:
> "Für dieses konkrete lokale Dev-/Demo-Setup (SQL-Login `Agent` auf einer lokalen Instanz,
> keine echten Geheimnisse) hat der Projektverantwortliche das Committen explizit
> freigegeben — `appsettings.json` ist daher **nicht** mehr in `.gitignore`."

**Risiko-Pattern (für die Zukunft, nicht heute):**
- Sobald jemand die Config-Datei für einen nicht-Dev-Einsatz kopiert (z.B. Test-Server,
  Kunden-Demo), sind die echten Credentials in Git-History.
- `.gitignore` enthält `appsettings.json` *nicht*, daher sind alle jemals committeten
  Versionen in der History.

**Mitigation, die heute schon geht:**
- `appsettings.example.json` als Template ohne Credentials, mit `appsettings.json` in
  `.gitignore`. User kopiert example → real, füllt Credentials.
- Oder: User-Secrets-Pattern (`dotnet user-secrets`), das Git-safe ist.
- Oder: explizit dokumentieren, dass `appsettings.json` für *nur* Dev ist, und der
  Production-Pfad eigene `appsettings.Production.json` + User-Secrets erwartet.

**Aufwand:** ~30 Minuten für saubere Trennung.

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

- **10 Findings**, davon 2 × High, 4 × Medium, 3 × Low, 1 × Info.
- **Hot Path:** F-SE-001 (LIKE-Injection) und F-SE-002 (PII in Logs) sind die zwei
  dringendsten Fixes. Beide sind klein (~30 Min Aufwand) und hochgradig sicherheitsrelevant.
- **Pattern-Risiko:** F-SE-005 (Credentials in committed Config) ist bewusst, aber das
  Pattern sollte für den ersten Produktiveinsatz auf `appsettings.example.json` +
  User-Secrets umgestellt werden.
- **Defense-in-Depth:** Die SqlIdentifierValidator-Plattform-Inkonsistenz (F-SE-004) ist
  ein "funktioniert, bis es nicht mehr funktioniert"-Risiko. Lowercase-only ist die saubere
  Lösung und kostet ~15 Minuten.
