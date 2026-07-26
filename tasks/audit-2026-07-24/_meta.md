# Audit-Methodik & Reproduzierbarkeit

> **Zweck:** Dieses Dokument macht den Audit reproduzierbar. Wer das gleiche Ergebnis (oder ein
> anderes) bekommen will, soll exakt nachvollziehen können, was wann gegen welchen Stand geprüft
> wurde — und was bewusst *nicht*.

## Stichtag

| Feld | Wert |
| --- | --- |
| Datum | 2026-07-24 |
| HEAD-Commit | `e5e0008` (`chore(release): Version 1.0.2`) |
| Branch | `main` |
| Working-Tree-Status bei Start | clean (1 lokal-modifizierte `.agents/rules/AiNetLinter.mdc` wurde vor Audit-Start per `git checkout -- .agents/rules/AiNetLinter.mdc` reverten — siehe README, "Working-Tree-Entscheidung") |

## Geprüft vs. nicht geprüft

### Geprüft
- Alle 17 C#-Source-Dateien in `src/KnowHowToAI.Core/` und `src/KnowHowToAI.Cli/`
- Alle 7 C#-Test-Dateien in `tests/KnowHowToAI.Core.Tests/` (ohne AiNetLinter/rules)
- Beide `.csproj`-Dateien
- `global.json` (Versions-Pin)
- `sql-scripts/0001_create_documents_table.sql`
- `scripts/publish.ps1`
- 8 Agent-Regel-Dateien unter `.agents/rules/*.mdc`
- 5 Doku-Dateien unter `docs/00-05` (gegen Code abgeglichen)
- `src/KnowHowToAI.Cli/appsettings.json` (committed Dev-Default-Config)
- 4 NuGet-Pakete mit externer Recherche auf aktuelle Stable-Version

### Bewusst nicht geprüft
- **Live-SQL-Server-Tests:** Auf dem Entwicklungsrechner ist kein SQL-Server-Setup verfügbar
  (bekannter Stolperstein, dokumentiert in `docs/03-Projektstruktur-und-Konfiguration.md`,
  Abschnitt 2). `search_docs`/`get_doc`/`list_children` wurden nicht gegen eine echte DB
  gespielt. Performance-Findings basieren auf statischer Code-Analyse + SQL-Server-Grundlagenwissen.
- **`demo-docs/` als Code:** Separat behandelt in `_demo-docs/findings.md` (Front-Matter-Korrektheit,
  Slug-Konformität, Feature-Coverage). Nicht durch den vollen Code-Quality/Security-Filter gejagt.
- **GitHub Actions Workflow:** `.github/workflows/release.yml` wurde nicht im Detail auditiert —
  ist nicht der Hot-Path, sondern 1:1 `scripts/publish.ps1` + Tag-Trigger. Bei Bedarf nachziehbar.
- **`.agents/rules/`-Inhalt:** Enthält die Agenten-Regeln und AiNetLinter.mdc.
- **AiNetLinter-Tool selbst:** Externes Repo, nicht Teil dieses Audits. Die `.rules.json` wurde
  nur gegen die `.mdc` abgeglichen, nicht inhaltlich bewertet.
- **Datenbank-Migrations-Skripte jenseits 0001:** Aktuell existiert nur `0001`, also nicht anwendbar.
  `SchemaMigrator` ist aber generisch für beliebig viele Skripte — der Audit bewertet das
  Framework, nicht die hypothetischen Folge-Skripte.

## Baseline-Build & Tests

| Schritt | Ergebnis | Log-Datei |
| --- | --- | --- |
| `dotnet build -c Release` | OK, 0 Warnungen, 0 Fehler | [`_meta-build.log`](_meta-build.log) |
| `dotnet run --project tests/KnowHowToAI.Core.Tests -c Release --no-build` | siehe Test-Log | [`_meta-test.log`](_meta-test.log) |
| `AiNetLinter --config ... --path KnowHowToAI.slnx` | OK, keine Verstöße | [`_meta-lint.log`](_meta-lint.log) |

> **Hinweis zum Linter:** Der Test im Projekt (`AiNetLinterTests.LintRun_ReportsNoViolations`)
> ruft AiNetLinter mit zwei getrennten Prozessen auf (ein bekannter Bug-Workaround im Linter-CLI,
> dokumentiert in `tests/KnowHowToAI.Core.Tests/AiNetLinterTests.cs`, Zeilen 25–28). Der
> "OK"-Output ist das Resultat des ersten Aufrufs (reiner Lint-Lauf). Die AiNetLinter-Regel
> `EnforceSealedClasses` ist in den Projekten aktiv — und wird in der statischen Analyse *eines*
> Findings in Dim 1 als nicht erfüllt markiert (siehe dort). Das ist ein Audit-Disput: der
> Linter meldet keinen Verstoß, der Audit schon. Begründung im Dim-1-Finding.

## Vergleichsbasen

| Basis | Datei | Was geprüft wurde |
| --- | --- | --- |
| 00-clarification | `.agents/rules/00-clarification.mdc` | Rückfragen statt Halluzination: bei welchen Entscheidungen hätte der Audit selbst rückfragen sollen? (Selbst-Audit-Punkt) |
| 01-code-style | `.agents/rules/01-code-style.mdc` | "Keine Interface-Wüsten", "kein Feature-Creep", "kein Kommentar-Ballast" |
| 02-testing | `.agents/rules/02-testing.mdc` | Testpflicht Core, Delegate-Pattern für DB-Isolation, xUnit-v3-Stil |
| 03-git-workflow | `.agents/rules/03-git-workflow.mdc` | Commits baubar + grüne Tests, Conventional-Commits-Stil, kein `--no-verify` |
| 04-docs-reference | `.agents/rules/04-docs-reference.mdc` | `docs/` als Source of Truth — Doku-Drift-Erkennung |
| 05-documentation | `.agents/rules/05-documentation.mdc` | Doku-Kürze, Verweis-Statt-Duplikation, gleicher Commit |
| 06-configuration | `.agents/rules/06-configuration.mdc` | Alles veränderliche nach `appsettings.json`, Konstanten-Datei erst ab 2. Fall |
| AiNetLinter | `.agents/rules/AiNetLinter.mdc` | sealed-Klassen, Methodenlänge, Cognitive/Komplexität, Phantom-Dependencies, `sealed`, `static`, `#nullable enable` |

## Schweregrad-Skala

| Grad | Bedeutung | Beispiel |
| --- | --- | --- |
| **Critical** | Datenverlust, Sicherheitslücke mit Exploit-Pfad, oder Funktionsverlust bei Standard-Use-Case | SQL-Injection via Konfig-Eingabe; fehlende Transaktion in `import` |
| **High** | DoS-Vektor, nicht-triviale Performance-Auswirkung, oder Regel-Verstoß, der das Tool in Produktion gefährdet | LIKE-Pattern ohne Escaping; doppelte JSON-Serialisierung in jedem Tool-Call |
| **Medium** | Architecture-Drift, fehlende Defense-in-Depth, fehlender Edge-Case, inkonsistente Konvention | DI-Inkonsistenz; fehlender ORDER BY in `ListChildren` |
| **Low** | Nice-to-have, Stilbruch ohne Funktionsverlust, Doku-Lücken, Refactoring-Optionen | Magic-String-Konstante; fehlendes Beispiel-File in `appsettings.example.json` |
| **Info** | Beobachtung, kein Handlungsbedarf, aber relevant für Kontext | Preview-Dependency ist bewusst gewählt (mit Begründung in Dim 7) |

## Audit-Durchführung

- **Dauer:** ~eine Sitzung (Audit-Vorbereitung, Quellen-Lesung, 9 Dimensions-Pässe, Synthese, Schreiben der Markdown-Dateien, Commit).
- **Methode:** Sequenzielle Dimensions-Pässe in *einem* Kontext (keine Sub-Agent-Delegation), um
  Cross-Cutting-Concerns (Logging, Error-Handling, DI-Konsistenz) konsistent über alle Pässe
  hinweg beurteilen zu können.
- **Externe Quellen:** Eine `web_search`-Recherche zu den Preview-Dependencies
  (`ModelContextProtocol`, `System.CommandLine`) und zur `Microsoft.Data.SqlClient 7.0`-Major-Version.
- **Was *nicht* behauptet wird:** Dass der Audit vollständig ist. Code-Audit ist eine Stichprobe
  mit Bias des Auditors. Wenn der Audit-Findings-Output "0 Medium-Findings" produziert, ist das
  verdächtig — bei diesem Audit: 0 wäre unrealistisch.
