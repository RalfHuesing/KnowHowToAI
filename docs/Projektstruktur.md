# KnowHowTo AI – anpassbarer Projekt- und Namespace-Rahmen

Dieses Dokument übersetzt Konzept und Roadmap in eine erste C#-Struktur. Es ist eine
Arbeitslandkarte, keine zusätzliche fachliche Quelle und kein unveränderlicher
Architekturvertrag. Fachliche Wahrheit bleibt `docs/Konzept.md`; Reihenfolge und
Fertigstellungsstatus bleiben in `docs/Roadmap.md`.

## Leitplanken

- Abhängigkeiten laufen ausschließlich `Server -> Core`, `Server -> Storage.SqlServer`
  und `Storage.SqlServer -> Core`. Core referenziert kein Infrastrukturprojekt.
- `Domain` enthält deterministische Fachmodelle und Regeln ohne SQL, Hosting oder MCP.
- `Application` orchestriert Use Cases und definiert Ports. Es enthält keine MCP-Typen
  und keine SQL-Implementierungsdetails.
- DTOs, Commands und Results werden beim jeweiligen Feature abgelegt. Es entstehen
  keine globalen Sammelordner `Models`, `Helpers`, `Utils` oder `Services`.
- Ein Namespace sollte meistens unter etwa 12 bis 15 produktiven Dateien bleiben.
  Zeichnen sich mehrere eigenständige Verantwortlichkeiten oder deutlich mehr Dateien
  ab, wird fachlich weiter unterteilt. Die Linter-Obergrenze von 30 Verzeichniseinträgen
  ist nur ein spätes Sicherheitsnetz, kein Planungsziel.
- Die angelegten leeren Typhüllen fixieren Namen und Verantwortungsrichtung, aber noch
  keine Methoden, DTO-Felder oder Persistenzverträge. Beim jeweiligen Roadmap-Slice
  werden sie implementiert, angepasst, zusammengelegt oder entfernt.

## `KnowHowToAI.Core`

| Namespace | Erwartete Größenordnung | Verantwortung |
|---|---:|---|
| `Domain.Common` | 5–8 | Result-, Fehler- und Warnverträge sowie gemeinsame primitive Regeln |
| `Domain.Hierarchy` | 6–10 | Node-Modell, Baumregeln, Sortierung und Zyklenprüfung |
| `Domain.Roles` | 5–8 | Rollen, Resolution Orders und deterministische Auflösung |
| `Domain.Content` | 8–12 | NodeContent, Normalisierung, Revisionen, Markdown- und Textoperationen |
| `Domain.Dependencies` | 6–10 | Provenienz, Dependency-Graph und transitive Freshness |
| `Domain.Versioning` | 5–8 | Snapshot, Transaction und Release |
| `Domain.Validation` | 5–8 | aggregierte harte Fehler, Warnungen und Validation Reports |
| `Application.Abstractions.Persistence` | 8–12 | schmale Ports für Migration, Versionierung, fachliche Writes und Retrieval |
| `Application.Abstractions.Runtime` | 2–4 | kontrollierbare Zeit- und ID-Erzeugung |
| `Application.Policies` | 2–4 | von der App-Konfiguration unabhängige Quality- und Retrieval-Policies |
| `Application.Transactions` | 5–8 | Begin, Get, Validate, Commit und Discard |
| `Application.Navigation` | 6–10 | Read-Kontext, Root, Node, Children und Rollen-Metadaten |
| `Application.Mutations.Nodes` | 6–10 | Create, Update, Move, Reorder und Delete Node |
| `Application.Mutations.Content` | 5–8 | Replace Content/Text und Delete Content |
| `Application.Mutations.Roles` | 5–8 | Rollenpflege und vollständige Resolution Orders |
| `Application.Retrieval.Export` | 4–7 | deterministischer Markdown-Export |
| `Application.Retrieval.Search` | 5–8 | begrenzte Suche, Snippets und Paging |
| `Application.History` | 6–10 | Snapshots, Diffs, Transaction Changes und Releases |

Domain-Typen dürfen keine Application-Typen referenzieren. Application-Features dürfen
sich gemeinsame Domain-Verträge teilen, aber nicht über einen globalen God-Service
aneinander gekoppelt werden.

## `KnowHowToAI.Storage.SqlServer`

| Namespace | Erwartete Größenordnung | Verantwortung |
|---|---:|---|
| `Connections` | 3–5 | Connection Factory und verbindungsbezogene Policies |
| `Configuration` | 2–4 | validierte SQL-/Migrations-Policies ohne `IConfiguration`-Abhängigkeit |
| `Migrations` | 6–10 | Skriptkatalog, Checksums, Journal, Locking und Migration Runner |
| `Repositories.Transactions` | 5–8 | Snapshot-Kopie und Transaction-Zustandswechsel |
| `Repositories.Knowledge` | 8–12 | versionierte Nodes, Rollen, Content und Dependencies |
| `Repositories.Snapshots` | 4–7 | Current-, Working- und historische Snapshot-Reads |
| `Repositories.History` | 5–8 | historische Snapshot-/Diff-Reads und unveränderliche Releases |
| `Repositories.Retrieval` | 6–10 | Navigation, Search, Paging und Diff-Abfragen |
| `Mapping` | 8–12 | interne SQL-Zeilenmodelle und explizites Domain-Mapping |

Repository-Namespace und -Klasse folgen dem fachlichen Zugriffsmuster. Eine Klasse pro
SQL-Tabelle ist ausdrücklich nicht das Ziel. Dapper-Zeilenmodelle bleiben intern.

## `KnowHowToAI.Server`

| Namespace | Erwartete Größenordnung | Verantwortung |
|---|---:|---|
| `Configuration` | 8–12 | bindbare Options, zentrale Validatoren und Redaction |
| `Hosting` | 3–6 | Composition Root, DI, Start und kontrollierter Shutdown |
| `Mcp.Contracts.Transactions` | 6–10 | Request-/Response-DTOs der Transaction-Tools |
| `Mcp.Contracts.Navigation` | 8–12 | Navigation, Search und Export-DTOs |
| `Mcp.Contracts.Mutations.Nodes` | 8–12 | Request-/Response-DTOs der Node-Mutationen |
| `Mcp.Contracts.Mutations.Content` | 6–10 | Request-/Response-DTOs der Content-Mutationen |
| `Mcp.Contracts.Mutations.Roles` | 6–10 | Request-/Response-DTOs der Rollen-Mutationen |
| `Mcp.Contracts.History` | 6–10 | Snapshot-, Diff- und Release-DTOs |
| `Mcp.Tools.Transactions` | 3–5 | dünne Transaction-/Validation-Handler |
| `Mcp.Tools.Navigation` | 3–5 | dünne Navigation-/Search-/Export-Handler |
| `Mcp.Tools.Mutations` | 5–8 | dünne Node-, Content- und Rollen-Handler |
| `Mcp.Tools.History` | 3–5 | dünne Historien- und Release-Handler |
| `Mcp.Mapping` | 3–6 | ausschließlich Transport-/Result-Mapping |

Die absehbar größte Contract-Gruppe `Mutations` ist bereits parallel zu den
Application-Features in `Nodes`, `Content` und `Roles` geteilt. Handler enthalten
keine Fachlogik und dürfen keine SQL-Typen kennen.

## Testprojekte

`KnowHowToAI.Core.Tests` spiegelt die Domain- und Application-Featuregrenzen. Schnelle
Tests liegen damit beispielsweise unter `Domain.Content`, `Domain.Hierarchy`,
`Application.Transactions` oder `Application.Retrieval`.

`KnowHowToAI.IntegrationTests` ist nach realer Grenze gegliedert:

- `SqlServer.Migrations`
- `SqlServer.Transactions`
- `SqlServer.Repositories`
- `Server.Hosting`
- `Server.Mcp`
- `TestSupport` für gemeinsam genutzte, echte Testinfrastruktur

Test-Support wird nur ergänzt, wenn mindestens zwei Tests ihn tatsächlich benötigen.
Testnamen beschreiben Verhalten; die Produktionsordner werden nicht mechanisch bis auf
Klassenebene gespiegelt.

Die vorläufigen `Namespace.cs`-Dateien halten noch leere Zielordner im Repository
sichtbar. Sobald der erste echte Typ oder Test in einem solchen Ordner entsteht, wird
die jeweilige Datei entfernt.
