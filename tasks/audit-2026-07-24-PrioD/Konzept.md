# Audit Prio D — KnowHowToAI

> **Quelle:** `tasks/audit-2026-07-24/` (Code-Audit v1.0.2, HEAD `e5e0008`)
> **Stand:** 2026-07-26
> **Zweck:** Verdichteter Input für die Planung via `.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md`
> **Vorgänger:** `tasks/audit-2026-07-24-PrioA/Konzept.md` (umgesetzt), `tasks/audit-2026-07-24-PrioB/Konzept.md` (in Umsetzung), `tasks/audit-2026-07-24-PrioC/Konzept.md` (Planungs-Input)
> **Methodik:** Aus dem Gesamt-Audit (70 Findings nach Prio A + B + C) wurden die 3 Findings extrahiert, die unter „Sicherheits-Hardening (Rest Dim 2)" zusammengefasst sind. Alle übrigen Findings (67) wurden bewusst weggelassen — Begründung am Ende des Dokuments.

## Inhalt

| ID | Titel | Schweregrad | Aufwand | Status |
|---|---|---|---|---|
| [F-SE-003](#f-se-003--keine-längen-validierung-der-mcp-tool-argumente) | Keine Längen-Validierung der MCP-Tool-Argumente | Medium | ~20 Min + Tests | offen |
| [F-SE-004](#f-se-004--sqlidentifiervalidator-plattform-inkonsistenz) | `SqlIdentifierValidator` Plattform-Inkonsistenz | Medium | ~15 Min + Tests | **erledigt** (Commit 3d549aa) |
| [F-SE-005](#f-se-005--connectionstring-mit-credentials-in-appsettingsjson) | `ConnectionString` mit Credentials in `appsettings.json` | Medium | ~5 Min Doku | **erledigt** |

**Gesamt-Aufwand:** ~40 Min (20 Min Code + 15 Min Tests + 5 Min Doku). Aufteilbar in 2 Commits.

**Leitidee:** DoS-Vektoren schließen, Plattform-Inkonsistenzen fixen, bevor das Repo produktiv geht. Plus einen Doku-Hinweis zur bewussten Credential-Entscheidung. Alle Längen-Limits in `appsettings.json` (per `.agents/rules/06-configuration.mdc`), keine magic values im Code.

---

## F-SE-003 — Keine Längen-Validierung der MCP-Tool-Argumente

> **Schweregrad:** Medium · **Dimension:** Sicherheit
> **Datei:** `src/KnowHowToAI.Cli/McpTools/DocsMcpTools.cs:17, 26, 35` + `src/KnowHowToAI.Cli/appsettings.json` (neu) + `src/KnowHowToAI.Cli/KnowHowToAiOptions.cs` (neu)

### Problem

`DocsMcpTools.ListChildrenAsync(string? parentSlug, ...)`, `SearchDocsAsync(string query, ...)`, `GetDocAsync(string slug, ...)` — keine Längen-Validierung. Das MCP-SDK selbst hat keine Schutzmaßnahmen.

**Vektor 1 — 10MB-Slug:**
- LLM schickt `slug = "a".repeat(10_000_000)`.
- `GetDocAsync` macht `SELECT ... WHERE slug = @Slug` — SQL-Server parameterisiert, harmlos. ABER: das MCP-Framework muss den 10-MB-String erst durch JSON-Serialisierung/-Deserialisierung jagen, was Speicher kostet.
- Schlimmer: `LogResponseSize` mit Document-Allokation. Wenn Document 100 KB ist, weitere 100 KB Allokation.

**Vektor 2 — `query` mit 100 KB:** Siehe F-SE-001 (in Prio A).

### Fix-Empfehlung

**Per `.agents/rules/06-configuration.mdc`: Alles veränderliche nach `appsettings.json`, Konstanten-Datei erst ab 2. Fall.**

Daher: KEINE `private const int` im Code. Stattdessen Options in `KnowHowToAiOptions`:

```csharp
// In KnowHowToAiOptions.Validation:
public sealed class ValidationOptions
{
    public int MaxSlugLength { get; init; } = 450;      // SQL Server-Index-Limit (Default)
    public int MaxParentSlugLength { get; init; } = 450;
}

// In KnowHowToAiOptions.Search:
public sealed class SearchOptions
{
    public int MaxQueryLength { get; init; } = 200;     // siehe F-SE-001 (Default)
}
```

```json
// appsettings.json (ergänzen):
"Validation": {
  "MaxContentLengthWarning": 8000,   // bestehend
  "MaxSlugLength": 450,
  "MaxParentSlugLength": 450
},
"Search": {
  "MaxResults": 50,                  // bestehend (F-PE-002)
  "MaxQueryLength": 200              // neu
}
```

**Validierung** in `DocsMcpTools` (oder in `SqlDocumentsStore`):
```csharp
public sealed class DocsMcpTools(KnowHowToAiOptions options, ...)
{
    public async Task<...> GetDocAsync(string slug, CancellationToken ct)
    {
        if (slug.Length > options.Validation.MaxSlugLength)
        {
            throw new ArgumentException(
                $"Slug ist {slug.Length} Zeichen lang, max {options.Validation.MaxSlugLength}.",
                nameof(slug));
        }
        // ... existing implementation
    }
}
```

Auch `parentSlug` validieren. Fehler als Tool-Error zurückgeben (MCP-SDK-Standard), nicht als unbehandelte Exception.

### Aufwand

- ~20 Min (5 Min Options, 5 Min appsettings.json, 5 Min Code, 5 Min Tests)
- 1 Commit (kann mit F-SE-001 aus Prio A kombiniert werden, falls dort `MaxQueryLength` nicht bereits in `KnowHowToAiOptions.Search` ist)

### Risiko

Niedrig. Additiv-defensiv. Bestehende Calls mit normal-langen Strings funktionieren identisch. Defaults sind konservativ (450 für Slug, 200 für Query).

---

## F-SE-004 — `SqlIdentifierValidator` Plattform-Inkonsistenz

> **Schweregrad:** Medium · **Dimension:** Sicherheit
> **Datei:** `src/KnowHowToAI.Core/Sync/SqlIdentifierValidator.cs:10`

### Problem

```csharp
private static readonly Regex Pattern = new("^[A-Za-z_][A-Za-z0-9_]{0,99}$", ...);
```

Erlaubt: Großbuchstaben, Kleinbuchstaben, Ziffern, Unterstrich. Max 100 Zeichen.

**Plattform-Verhalten:**
- **Windows-Default-Collation** (z.B. `SQL_Latin1_General_CP1_CI_AS`): case-**insensitive**. `MyTable` und `mytable` sind *derselbe* Identifier. Funktioniert.
- **Linux-Default-Collation** (z.B. mit `UTF8`-Collation, oder wenn explizit `Latin1_General_100_BIN2`): case-**sensitive**. `MyTable` und `mytable` sind *verschiedene* Identifier.

**Konsequenz:** Eine `appsettings.json` mit `"DocumentsTableName": "MyTable"` funktioniert auf dem Dev-Rechner (Windows) und bricht auf einer Linux-DB-Instanz.

**SQL Server Reserved Words:**
Die Regex erlaubt auch Identifier wie `Table`, `Select`, `From`, `User` etc. SQL Server wirft dann "Incorrect syntax near the reserved word". `SchemaMigrator` würde beim `CREATE TABLE dbo.User` scheitern.

### Fix-Empfehlung

1. Lowercase-only erzwingen: `^[a-z_][a-z0-9_]{0,99}$` — passt zu den Slug-Regeln (lowercase-only) und ist plattform-konsistent.
2. Optional: Liste verbotener Reserved Words prüfen (z.B. via eine `HashSet<string>` mit den ~50 häufigsten).
3. Konsistenz mit `SlugRules`: beide nutzen `a-z0-9-` als "sichere" Identifiers.

### Aufwand

- ~15 Min + Tests für Lowercase-only + Reserved-Word-Liste
- 1 Commit

### Risiko

Niedrig. Falls jemand Uppercase-Identifier in `appsettings.json` hat, schlägt der Validator nun früher. Das ist *gewollt* — Konfigurationen werden auf das sichere Pattern gezwungen.

---

## F-SE-005 — `ConnectionString` mit Credentials in `appsettings.json`

> **Schweregrad:** Medium · **Dimension:** Sicherheit + Doku
> **Datei:** `docs/03-Projektstruktur-und-Konfiguration.md` (Doku-Update) + ggf. `appsettings.json`-Kommentar

### Problem

`src/KnowHowToAI.Cli/appsettings.json:4`:
```json
"ConnectionString": "Server=%COMPUTERNAME%\\MSSQLSERVER2022;Database=DemoDB;User Id=Agent;Password=Agent!;TrustServerCertificate=True;",
```

In Git committed. `docs/03-Projektstruktur-und-Konfiguration.md`, Zeile 81 dokumentiert:
> "Für dieses konkrete lokale Dev-/Demo-Setup (SQL-Login `Agent` auf einer lokalen Instanz, keine echten Geheimnisse) hat der Projektverantwortliche das Committen explizit freigegeben — `appsettings.json` ist daher **nicht** mehr in `.gitignore`."

**Risiko-Pattern (für die Zukunft, nicht heute):**
- Sobald jemand die Config-Datei für einen nicht-Dev-Einsatz kopiert (z.B. Test-Server, Kunden-Demo), sind die echten Credentials in Git-History.
- `.gitignore` enthält `appsettings.json` *nicht*, daher sind alle jemals committeten Versionen in der History.

**Bewusste User-Entscheidung:** Per Audit und User-Profile explizit akzeptiert. Daher KEIN Code-Fix. Nur Doku-Hinweis, dass die Entscheidung *bewusst* ist und einen Migrations-Pfad für die Zukunft aufzeigt.

### Fix-Empfehlung (Doku-Variante)

Kurzer Abschnitt in `docs/03` Abschnitt 2 (appsettings.json-Beispiel):
> "**Credential-Strategie für v1 (bewusst gewählt):** `appsettings.json` enthält Demo-Credentials (`Agent`/`Agent!`) und ist in Git committed. Das ist OK, weil:
> 1. Es sind Demo-Credentials, keine echten Geheimnisse.
> 2. Der lokale Dev-SQL-Server akzeptiert sie ohne Auth.
> 3. Production-Migration: bei erstem produktiven Einsatz umstellen auf `appsettings.example.json` (Template ohne Credentials) + `appsettings.json` in `.gitignore`, oder `dotnet user-secrets`."

### Aufwand

- ~5 Min Doku
- 1 Doku-Commit

### Risiko

Keine. Reine Doku, klarstellt die bewusste Entscheidung.

---

## Warum diese 3 und nicht andere?

### Aufgenommen

1. **F-SE-003** — Echte DoS-Lücke (10MB-Slug), billig zu fixen
2. **F-SE-004** — "Funktioniert, bis es nicht mehr funktioniert" auf Linux-DB; Lowercase-only ist saubere Lösung
3. **F-SE-005** — Bewusste User-Entscheidung; Doku-Hinweis verhindert, dass jemand die Config versehentlich "aufräumt" und in Git-History echte Secrets landen

### Bewusst weggelassen (Kurzbegründung)

- **F-SE-006 (Path-Traversal):** Defense-in-Depth, niedrig. Validator fängt es ab.
- **F-SE-007 (COMPUTERNAME hartcodiert):** Dev-only, bewusst. Vollständige `Environment.ExpandEnvironmentVariables`-Lösung hat Nachteile in MCP-Host-Kontexten.
- **F-SE-008 (JsonSerializer ohne Catch):** Querverweis zu F-CQ-003. Wird in Prio G (Code-Quality-Rest) mit F-CQ-003 zusammen behandelt.
- **F-SE-009 (Tool-Beschreibungen Sicherheits-Hinweise):** Niedrig, LLM-UX, gehört zu Prio B (F-MC-001).

Alle übrigen Findings (67) gehören thematisch in andere Brocken (E: Test-Coverage, F: Performance-Polish, G: Config-Deploy, H: Code-Quality-Rest, plus die Prio-A-Findings die umgesetzt sind und aus dem Original-Audit entfernt werden müssen).

## Empfohlene Umsetzungs-Reihenfolge

1. **F-SE-003** (~20 Min + Tests) — DoS-Lücke mit Options-Pattern (keine magic values)
2. **F-SE-004** (~15 Min + Tests) — Plattform-Konsistenz
3. **F-SE-005** (~5 Min Doku) — kann mit F-SE-004 in einem Commit kombiniert werden

**Gesamt-Aufwand in dieser Reihenfolge:** ~40 Min, 2 Commits.

**Commit-Clustering-Vorschlag:**
- Commit 1: F-SE-003 + F-SE-004 (Defense-in-Depth-Bündel)
- Commit 2: F-SE-005 (Doku-Hinweis)

## Querverweise zu anderen Brocken

- **F-SE-001 in PrioA** — DoS-Vektor `query`-Länge. F-SE-003 ergänzt das mit Slug-Validierung.
- **F-SE-005 ist Doku-Pendant zu F-DK-005 in Prio B-Bewusst-rausgelassen** — thematisch verwandt (Preview-Dependencies / Credentials).
- **F-SE-008 in Prio G (Code-Quality-Rest)** — Querverweis zu F-CQ-003.

## Nächster Schritt

Dieses Konzept wird in einem separaten Chat via
`.agents/Agent-Scaffolding/dev-loop/planning/orchestrator.md` durchgegangen.
Dort entsteht der konkrete Umsetzungs-Plan (Schritt-Liste, Commit-Strategie,
Test-Strategie, Doku-Updates).
