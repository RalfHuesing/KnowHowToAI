# Architektur

## Stack

- C# / .NET (aktuell `net10.0`)
- MS SQL Server 2019 oder neuer (inkl. Azure SQL)
- ASP.NET-Core-Webhost mit Kestrel; MCP ist über stateless Streamable HTTP unter `/mcp` erreichbar (ModelContextProtocol-SDK); Legacy-SSE ist deaktiviert
- aktuelle, gepflegte NuGet-Standardpakete; für Markdown-Verarbeitung eine
  etablierte Bibliothek, kein eigener Parser
- Datenzugriff über Dapper (Zeilenmodelle bleiben intern im Storage-Projekt)

## Schichten

Strikte Schichtung mit einseitigen Abhängigkeiten:

```text
             MCP Streamable HTTP (/mcp)
                          │
                          ▼
                ┌─────────────────┐
                │ MCP Adapter     │  (KnowHowToAI.Server)
                └────────┬────────┘
                         ▼
                ┌─────────────────┐
                │ Application      │  (KnowHowToAI.Core)
                │ Services + Ports │
                └────────┬────────┘
                         ▼
                ┌─────────────────┐
                │ Domain           │  (KnowHowToAI.Core)
                │ Model, Regeln   │
                └────────┬────────┘
                         ▼
                ┌─────────────────┐
                │ Repository       │  (KnowHowToAI.Storage.SqlServer)
                └────────┬────────┘
                         ▼
                ┌─────────────────┐
                │ MS SQL Server   │
                └─────────────────┘
```

MCP-Handler enthalten keine Geschäftslogik – sie mappen und delegieren
(`return await knowledgeService.UpdateContent(…)`). Die Fachlogik liegt in
Application- und Domain-Services. Domain und Application kennen weder MCP- noch
SQL-Typen; Domain referenziert kein Infrastrukturprojekt.

## Transportgrenzen

`KnowHowToAI.Server` läuft als einziger ASP.NET-Core-Webhost mit Kestrel. Der
Webhost reserviert `/api` und dessen Unterpfade mit einer leeren `404`-Antwort
für die üblichen HTTP-Methoden; eine REST- oder OpenAPI-Infrastruktur gibt es
noch nicht. Die Root-Route `/` liefert eine minimale Blazor Interactive-Server-
Shell. Sie ruft ihren Read-only-Status direkt über einen Application Service ab;
es gibt weder einen HTTP-Loopback noch eine allgemeine Browser-API. `/mcp`
ist der einzige MCP-Endpunkt und als stateless Streamable HTTP erreichbar. Der
Agent greift ausschließlich über die
[MCP-API](McpApi.md) zu; es gibt keinen Workflow über lokale temporäre
Markdown-Dateien. Das System funktioniert damit mit jedem MCP-fähigen Client,
unabhängig von lokalem Dateizugriff, Git oder Unified-Diff-Fähigkeit.

Die Geschäftslogik ist nicht an einen MCP-Transport gekoppelt. Weitere
Browseradapter werden ohne Änderung der Application-/Domain-Schicht auf diesem
Webhost ergänzt.

## Projekte und Namespaces

Abhängigkeiten laufen ausschließlich `Server -> Core`, `Server ->
Storage.SqlServer` und `Storage.SqlServer -> Core`.

**`KnowHowToAI.Core`**

| Namespace | Verantwortung |
|---|---|
| `Domain.Common` | Result-, Fehler- und Warnverträge, gemeinsame primitive Regeln |
| `Domain.Hierarchy` | Node-Modell, Baumregeln, Sortierung, Zyklenprüfung |
| `Domain.Roles` | Rollen, Resolution Orders, deterministische Auflösung |
| `Domain.Content` | NodeContent, Normalisierung, Revisionen, Markdown-/Textoperationen |
| `Domain.Dependencies` | Provenienz, Dependency-Graph, transitive Freshness |
| `Domain.Versioning` | Snapshot, Transaction, Release |
| `Domain.Validation` | aggregierte harte Fehler, Warnungen, Validation-Reports |
| `Application.Abstractions.Persistence` | schmale Ports für Migration, Versionierung, fachliche Writes, Retrieval |
| `Application.Abstractions.Runtime` | kontrollierbare Zeit- und ID-Erzeugung |
| `Application.Runtime` | Laufzeit-Dienste (durchgetaktete Zeit, IDs) |
| `Application.Policies` | von der App-Konfiguration unabhängige Quality-/Retrieval-Policies |
| `Application.Transactions` | Begin, Get, Validate, Commit, Discard |
| `Application.Navigation` | Read-Kontext, Root, Node, Children, Rollen-Metadaten |
| `Application.Mutations.Nodes` | Create, Update, Move, Reorder, Delete Node |
| `Application.Mutations.Content` | Replace Content/Text, Delete Content |
| `Application.Mutations.Roles` | Rollenpflege, vollständige Resolution Orders |
| `Application.Retrieval.Export` | deterministischer Markdown-Export |
| `Application.Retrieval.Search` | begrenzte Suche, Snippets, Paging |
| `Application.History` | Snapshots, Diffs, Transaction Changes, Releases |

**`KnowHowToAI.Storage.SqlServer`**: `Connections` (Connection Factory),
`Configuration` (validierte SQL-/Migrations-Policies),
`Migrations` (Katalog, Checksums, Journal, Locking, Runner),
`Repositories.Transactions` (Snapshot-Kopie, Transaction-Zustandswechsel),
`Repositories.Knowledge` (versionierte Nodes, Rollen, Content, Dependencies),
`Repositories.Snapshots` (Current-/Working-/historische Snapshot-Reads),
`Repositories.History` (historische Snapshot-/Diff-Reads),
`Repositories.Releases` (unveränderliche Releases),
`Repositories.Retrieval` (Navigation, Search, Paging, Diff-Abfragen),
`Mapping` (interne Dapper-Zeilenmodelle und explizites Domain-Mapping).

**`KnowHowToAI.Server`**: `Configuration` (bindbare Options, zentrale
Validatoren, Redaction), `Hosting` (Composition Root, DI, Kestrel-Start,
kontrollierter Shutdown), `Mcp.Contracts.*` (Request-/Response-DTOs je Toolgruppe),
`Mcp.Tools.*` (dünne Handler), `Mcp.Mapping` (ausschließlich
Transport-/Result-Mapping), `Web.Components` (Shell, Router, Layout und zentrale
Fehlergrenze) sowie `Web.Features.Dashboard` (die derzeit einzige Root-Seite).

Leitplanken:

- DTOs, Commands und Results liegen beim jeweiligen Feature; es gibt keine
  globalen Sammelordner `Models`, `Helpers`, `Utils` oder `Services`.
- Ein Namespace bleibt meistens unter etwa 12–15 produktiven Dateien; bei
  mehreren eigenständigen Verantwortlichkeiten wird fachlich weiter unterteilt.
  Die Linter-Obergrenze von 30 Verzeichniseinträgen ist ein spätes
  Sicherheitsnetz, kein Planungsziel.
- Domain-Typen referenzieren keine Application-Typen. Application-Features teilen
  sich Domain-Verträge, werden aber nicht über einen globalen God-Service
  gekoppelt.
- Repository-Namespace und -Klasse folgen dem fachlichen Zugriffsmuster; eine
  Klasse pro SQL-Tabelle ist ausdrücklich nicht das Ziel.
- Die MCP-Handler-Signaturen sind flach (Selektor-, Rollen- und Paging-Parameter
  je Tool), weil der MCP-SDK-Schema-Generator Parameterlisten 1:1 in das
  Tool-Input-Schema übersetzt; gebündelte Parameter-Records würden zu
  verschachtelten JSON-Feldern führen. Für `Mcp/Tools` ist deshalb in
  `ainetlinter-rules.json` per `PathOverrides` die
  `MaxMethodParameterCount`-Grenze gelockt.

## Testprojekte

`KnowHowToAI.Core.Tests` spiegelt die Domain- und Application-Featuregrenzen
(`Domain.*`, `Application.*`, `Smoke`).

`KnowHowToAI.IntegrationTests` ist nach realer Grenze gegliedert:
`SqlServer.Migrations`, `SqlServer.Transactions`, `SqlServer.Repositories`,
`SqlServer.Abnahme` (messgestützte Abnahmetests), `Server.Hosting`, `Server.Mcp`,
`Smoke`, `TestSupport` (gemeinsam genutzte, echte Testinfrastruktur).
Test-Support wird nur ergänzt, wenn mindestens zwei Tests ihn tatsächlich
benötigen; Testnamen beschreiben Verhalten.

`KnowHowToAI.Web.Tests` prüft die Razor-Shell mit bUnit und isolierten
Application-Persistence-Ports. `KnowHowToAI.BrowserTests` startet die
veröffentlichte Server-EXE als Black Box mit Google Chrome Stable im headless
Interactive-Server-Smoke; es referenziert kein Produktionsprojekt.

## Deployment

Ein Serverprozess (eine EXE) wird über eine SQL-Verbindung mit genau einer
KnowHowTo-AI-Datenbank verbunden und bedient Blazor-Shell und MCP auf einem
konfigurierten Origin. Für verschiedene Wissensbestände werden mehrere
Serverprozesse mit unterschiedlichen Connection Strings und jeweils eigenem
Origin gestartet:

```text
Server A (Origin A) → SQL DB Produkt A
Server B (Origin B) → SQL DB Produkt B
Server C (Origin C) → SQL DB Internes Wissen
```

V1 benötigt dadurch keine Mandantenverwaltung innerhalb einer Instanz. Ein
zentral betriebener SQL Server mit mehreren Serverprozessen mehrerer Nutzer ist
der vorgesehene Betriebsmodus. Da eine Serverinstanz genau eine Datenbank bedient,
erhält jede Wissensbasis ihre eigenen Policies über die App-Konfiguration.
