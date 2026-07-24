# Nice-to-Haves & Backlog-Items

> **Was hier landet:** Findings mit Schweregrad `Low` oder `Info`, sowie Erkenntnisse
> aus dem Audit, die in keinen der priorisierten Fixes passen, aber wertvoll sind.

## Nice-to-haves (nächste 3-6 Monate)

Diese sind *nicht dringend*, aber würden die Code-Qualität weiter verbessern.

### Test-Lücken (F-TS-003 bis F-TS-011)

**Aufwand:** ~45 Min für alle zusammen
**Impact:** Edge-Case-Coverage verbessern

| Test | Was geprüft wird | Datei |
| --- | --- | --- |
| `content.Length = 0` | Leere Inhalte in `DocsValidator` | `DocsValidatorTests.cs` |
| `replaceAllAsync`-Throws | Was passiert bei SQL-Exception? | `ImportServiceTests.cs` |
| `getAllAsync`-Throws/Empty | Edge-Cases im Export | `ExportServiceTests.cs` |
| Nur-whitespace `title` | `title: "   "` Front-Matter | `FrontMatterParserTests.cs` |
| BOM am Dateianfang | `EF BB BF` vor `---` | `FrontMatterParserTests.cs` |
| `SlugRules.FromFilePath` | Edge-Cases (Path-Traversal, mehrere Extensions) | `SlugRulesTests.cs` |
| Nicht-`.md`-Dateien | Werden vom Validator ignoriert? | `DocsValidatorTests.cs` |

**Empfehlung:** In einem einzigen `tests(audit-fixes): Ergänze Edge-Case-Tests`-
Commit, der diese ~7 Tests in einem Rutsch hinzufügt.

---

### `FrontMatterParser` zu `static class` (F-CQ-005)

**Aufwand:** ~15 Min
**Impact:** Spart 3 `new`-Allokationen, klarer Vertrag

**Vorbedingung:** Verifizieren, dass YamlDotNet `IDeserializer`/`ISerializer`
thread-safe sind (laut Doku: ja). Wenn das geklärt ist, kann der Refactor in
einem Commit durchgeführt werden.

---

### `Constants.cs` einführen (F-AR-005)

**Aufwand:** ~20 Min
**Impact:** Vorbereitung für zukünftige Refactorings; Konsolidierung von
`---`-Delimiter, `.md`/`.markdown`-Extensions, `file://`-Präfix

**Vorbedingung:** Erst sinnvoll, wenn ein 2. Anwendungsfall für eine dieser
Konstanten entsteht.

---

### Linter-Coverage in `AiNetLinterTests` reporten (F-CQ-001-Disput)

**Aufwand:** ~10 Min
**Impact:** Macht den Linter-Output glaubwürdiger. Wenn der Linter "OK" sagt,
aber Audit-Disput besteht, sollte das im Test-Output stehen.

**Konkrete Schritte:**
1. `AiNetLinterTests` parst den `--list-rules`-Output (oder einen anderen
   Coverage-Report)
2. Test bestätigt: "EnforceSealedClasses ist für `*.Core`-Projekte aktiv"
3. Wenn nicht: Skip mit Hinweis

---

### Service-Lifetimes in `docs/03` dokumentieren (F-AR-007)

**Aufwand:** ~5 Min
**Impact:** Klärt, warum `SqlDocumentsStore` Singleton ist

**Konkrete Schritte:** Ein Absatz in `docs/03` Abschnitt 1 (Solution-Layout)
mit Erklärung der Lifetime-Wahl.

---

### `import`-Pfad: Cancellation-Sicherheit verbessern (F-TS-005)

**Aufwand:** ~20 Min
**Impact:** Bei abgebrochenem Import bleibt die DB konsistent

**Konkrete Schritte:** `ImportService.ReadDocuments` defensiver machen, sodass
`IOException` (nicht-lesbare Datei) als Validation-Error gesammelt wird statt
zu propagieren.

---

### `appsettings.example.json` einführen (F-CD-002 / F-SE-005)

**Aufwand:** ~30 Min
**Impact:** Saubere Trennung Dev-Config von Production-Config

**Vorbedingung:** Migration erfordert `git rm --cached appsettings.json`,
was die History nicht antastet, aber Working-Tree-Status verändert.

---

### `appsettings.Production.json`-Unterstützung (F-CD-003)

**Aufwand:** ~15 Min
**Impact:** Standard Microsoft.Extensions.Configuration-Pattern, fehlt

---

### `dotnet user-secrets`-Support (F-CD-004)

**Aufwand:** ~20 Min
**Impact:** Sichere Secret-Verwaltung in Dev-Setups

---

### `publish.ps1` Pre-Test (F-CD-005)

**Aufwand:** ~5 Min
**Impact:** Verhindert Publish bei roten Tests

**Konkrete Schritte:**
```powershell
dotnet test -c $Configuration --no-build
if ($LASTEXITCODE -ne 0) { throw "Tests rot — Publish abgebrochen." }
```

---

### `publish.ps1` Output-Cleanup (F-CD-006)

**Aufwand:** ~2 Min
**Impact:** Verhindert stale Files in `publish/`

---

### Pfad-Traversal-Schutz in `SlugRules.FromFilePath` (F-SE-006)

**Aufwand:** ~10 Min
**Impact:** Defense-in-Depth für `import`

---

### `ConnectionString` Env-Var-Expansion generalisieren (F-SE-007)

**Aufwand:** ~10 Min
**Impact:** Konsistenter mit Standard-Patterns

**Achtung:** Das ursprüngliche Argument für *nur* `%COMPUTERNAME%` war
"MCP-Prozess erbt möglicherweise kein Environment" — `Environment.ExpandEnvironmentVariables`
wäre riskant. Besser: explizite Allow-List von Variablen, oder Doku-Hinweis.

---

### `LogResponseSize` in falscher Schicht (F-AR-003)

**Aufwand:** obsolet nach Fix #1 in priorisierten Plan
**Impact:** Architektur-Smell, nicht kritisch

**Hinweis:** Mit Fix #1 (`MeasureResponseSize`-Variante) wird `LogResponseSize`
gelöscht. Damit ist F-AR-003 obsolet.

---

### `JsonSerializerContext` (Source-Generator) (F-PE-007)

**Aufwand:** ~1 Stunde
**Impact:** Reflection-Overhead bei JSON-Serialisierung eliminieren

**Vorbedingung:** Performance-Messung vorher/nachher. Wenn der Unterschied
< 5% ist, nicht lohnend.

---

## Backlog-Items (langfristig)

Diese sind *keine direkten Audit-Findings*, sondern Verbesserungen, die sich
aus dem Audit-Kontext ergeben.

### `Backlog-Item: SQLite-Test-Infrastruktur`

Aus F-TS-001. Substantieller Aufwand (4+ Stunden), eigenes Projekt wert.
Dokumentation in `docs/05-Roadmap.md` ist bereits vorhanden (Zeile 94).

### `Backlog-Item: SqlBulkCopy für Bulk-Imports`

Aus F-PE-004. Sinnvoll bei Bibliotheken >5.000 Dokumente. Nicht für v1.

### `Backlog-Item: Full-Text-Search-Migration`

Aus F-PE-005. Bewusste v1-Entscheidung *gegen* FTS. Wenn die Token-Budget-
Probleme aus F-PE-002 real werden, ist das der Ausweg.

### `Backlog-Item: Content-Chunking für große Docs`

Aus `docs/05-Roadmap.md`, Zeile 95 (Backlog). Relevant für v2, wenn
`get_doc` mit 100-KB-Inhalten das LLM-Kontext-Budget sprengt.

### `Backlog-Item: Connection-Pool-Tuning`

Aus F-PE-008 (Low). Nicht dringend. Connection-Pooling läuft via
`Microsoft.Data.SqlClient`-Defaults. Wenn man mal Performance-Probleme misst,
ist das eine Stellschraube.

### `Backlog-Item: Watch-Modus (FileSystemWatcher)`

Aus `docs/05-Roadmap.md`, Zeile 89. Explizit v2.

### `Backlog-Item: CI-Pipeline (GitHub Actions)`

Aus `docs/05-Roadmap.md`, Zeile 93. Würde viele der Build-Test-Lint-Probleme
frühzeitig erkennen. Auch F-DP-003 (NuGet-Audit) gehört dazu.

### `Backlog-Item: Schreib-Tools via MCP`

Aus `docs/05-Roadmap.md`, Zeile 91. Erfordert Design-Entscheidung über
Race-Conditions zwischen Claude-Editing und `import`-Läufen. F-AR-004
(`ReplaceAllAsync` Thread-Safety) wird relevant, sobald das umgesetzt wird.

### `Backlog-Item: Multi-Library-Support in einem Prozess`

Aus `docs/05-Roadmap.md`, Zeile 90. Aktueller Stand: mehrere Configs =
mehrere Prozesse. Multi-Library = ein Prozess, mehrere Tabellen.

## Was *nicht* in den Nice-to-haves steht

Diese sind bewusst *nicht* im Nice-to-have-Plan, obwohl sie als Findings
dokumentiert sind:

- **F-CQ-006/007/008** (Info-Findings): Kein Handlungsbedarf, positiver Befund.
- **F-DK-009/010/011/012** (Info-Findings): Doku-Code-Konsistenz bestätigt.
- **F-SE-008** (JsonSerializer-Catch): In F-CQ-003 (Quick-Win) enthalten.
- **F-SE-010** (Info): Kein Handlungsbedarf.
- **F-AR-008/009/010** (Info): Positivbefunde.
- **F-CD-009/010/011** (Info): Positivbefunde.
- **F-DP-007/008** (Info): Positivbefunde.
- **F-MC-008/009/010** (Info): Positivbefunde.
- **F-PE-007/008/009** (außer 002, 003, 005): Bewusste Design-Entscheidungen.

## Review-Zyklus

Empfehlung: Diesen Audit in 3-6 Monaten wiederholen, mit Fokus auf:

1. Welche der hier dokumentierten Findings umgesetzt wurden
2. Welche neuen Findings durch die Umsetzungen entstanden sind (jeder
   Refactor öffnet neue Lücken)
3. Welche Performance-Charakteristika jetzt sichtbar sind, die statisch
   nicht erkennbar waren (z.B. nach erstem Echt-Import einer 10.000-Doc-
   Bibliothek)
4. Dependency-Updates: Sind `ModelContextProtocol 2.0.0` Stable und
   `System.CommandLine 3.0.0` Stable mittlerweile verfügbar?
5. `docs/`-Drift: Haben sich neue Lücken aufgetan, weil Doku nicht
   mit-code-nachgezogen wurde?
