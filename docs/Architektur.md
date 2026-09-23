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

Laufzeitabhängigkeiten laufen ausschließlich `Server -> Core`, `Server ->
Storage.SqlServer` und `Storage.SqlServer -> Core`. Zusätzlich wird
`KnowHowToAI.Analyzers` ausschließlich beim Build als Analyzer an `Server`
angebunden; die Assembly ist keine Laufzeitabhängigkeit.

**`KnowHowToAI.Core`**

| Namespace | Verantwortung |
|---|---|
| `Domain.Common` | Result-, Fehler- und Warnverträge, gemeinsame primitive Regeln |
| `Domain.Hierarchy` | Node-Modell, Baumregeln, Sortierung, Zyklenprüfung |
| `Domain.Audiences` | Zielgruppen, Zielgruppen-Auflösungsreihenfolgen, deterministische Auflösung |
| `Domain.Content` | NodeContent, Normalisierung, Revisionen, Markdown-/Textoperationen |
| `Domain.Dependencies` | Provenienz, Dependency-Graph, transitive Freshness |
| `Domain.Versioning` | Snapshot, Transaction, Release |
| `Domain.Validation` | aggregierte harte Fehler, Warnungen, Validation-Reports |
| `Application.Abstractions.Persistence` | schmale Ports für Migration, Versionierung, fachliche Writes, Retrieval |
| `Application.Abstractions.Runtime` | kontrollierbare Zeit- und ID-Erzeugung, `ICurrentUserService` |
| `Application.Runtime` | Laufzeit-Dienste (durchgetaktete Zeit, IDs) |
| `Application.Policies` | von der App-Konfiguration unabhängige Quality-/Retrieval-Policies |
| `Application.Transactions` | Begin, Get, Validate, Commit, Discard, ListOpen |
| `Application.Navigation` | Read-Kontext, Root, Node, Children, Zielgruppen-Metadaten |
| `Application.Mutations.Nodes` | Create, Update, Move, Reorder, Delete Node |
| `Application.Mutations.Content` | Replace Content/Text, Delete Content |
| `Application.Mutations.Audiences` | Zielgruppenpflege, vollständige Zielgruppen-Auflösungsreihenfolgen |
| `Application.Retrieval.Export` | deterministischer Markdown-Export |
| `Application.Retrieval.Search` | begrenzte Suche, Snippets, Paging |
| `Application.History` | Snapshots, Diffs, Transaction Changes, Releases |
| `Application.Dashboard` | transportneutrale, bereichsweise ladbare Dashboard-Reads (Current Snapshot/Release, offene Transactions, Qualität und geänderte Nodes) |

**`KnowHowToAI.Storage.SqlServer`**: `Connections` (Connection Factory),
`Configuration` (validierte SQL-/Migrations-Policies),
`Migrations` (Katalog, Checksums, Journal, Locking, Runner),
`Repositories.Transactions` (Snapshot-Kopie, Transaction-Zustandswechsel),
`Repositories.Knowledge` (versionierte Nodes, Zielgruppen, Content, Dependencies),
`Repositories.Snapshots` (Current-/Working-/historische Snapshot-Reads),
`Repositories.History` (historische Snapshot-/Diff-Reads),
`Repositories.Releases` (unveränderliche Releases),
`Repositories.Retrieval` (Navigation, Search, Paging, Diff-Abfragen, Dashboard-Reads via `SqlDashboardRepository`),
`Mapping` (interne Dapper-Zeilenmodelle und explizites Domain-Mapping).

**`KnowHowToAI.Analyzers`**: kleine, frameworkunabhängige Roslyn-Buildregeln.
`KHTAI001` verhindert in direkt oder indirekt von `ComponentBase` abgeleiteten
Typen `ConfigureAwait(false)`, `Task.Run`, `TaskFactory.StartNew` und
`ContinueWith`. Die Regel läuft beim Build von `KnowHowToAI.Server` als
`Analyzer`-Ausgabe ohne Assemblyreferenz und wird durch echte Roslyn-
Kompilationen in `KnowHowToAI.Analyzers.Tests` geprüft.

**`KnowHowToAI.Server`**: `Configuration` (bindbare Options, zentrale
Validatoren, Redaction), `Hosting` (Composition Root, DI, Kestrel-Start,
kontrollierter Shutdown, `DummyCurrentUserService`), `Mcp.Contracts.*` (Request-/Response-DTOs je Toolgruppe),
`Mcp.Tools.*` (dünne Handler), `Mcp.Mapping` (ausschließlich
Transport-/Result-Mapping; die MCP-Audience-Verträge liegen in den
Audience-DTOs und den ausschließlich `list_audiences`, `create_audience`,
`update_audience`, `delete_audience` und `set_audience_resolution` benannten
Handlern), `Web.Components` (Shell, Router, Layout,
zentrale Fehlergrenze und gemeinsam genutzte Bausteine unter
`Web/Components/Shared/Dialogs` (nativer Dialog-Wrapper `AppDialog` mit
schmaler JS-Isolation in `AppDialog.razor.js` für `showModal()`, `close()` und
das native `close`-Ereignis sowie `ConfirmationDialog`),
`Web/Components/Shared/Feedback` (Status-, Warn- und Toastdarstellungen)
und `Web/Components/Shared` enthält außerdem `PageFrame`, die ausführbare
Seitenbasis für alle zehn routbaren Feature-Varianten. Die Komponente rendert
einen Page-Root mit Header, genau einem `h1`, optionaler Beschreibung, Badges
und Aktionen sowie dem Feature-Inhalt; sie enthält keine Application-Aufrufe
und keine Featurelogik. `PageFrame.razor.css` ist der einzige Owner der
komponentennahen Root-/Header-Gestaltung. Das zentrale `wwwroot/css/base/layout.css`
stellt nur das neutrale `.page-frame`-Breitenprimitive und frameworkweite
Arbeitsflächenhilfen bereit, Feature-CSS bleibt auf fachliche Innenlayouts
begrenzt. `Web/Components/Shared/States` (wiederverwendbare Lade-, Leer- und
Fehlerzustände). `AppStatus` zeigt jeden Zustand als Icon plus Text;
`LoadingState`, `BusyOverlay`, `EmptyState`, `NotFoundState` und
`TechnicalErrorState`: rein darstellende Komponenten nur mit
Anzeigeparametern und optionalem Retry-Callback, ohne Application-Aufrufe
und ohne globale Zustandsmaschine. `LoadingState` ersetzt den Initial
Load durch wahrnehmbaren Status (`role="status"`, `aria-busy`) statt
endlos leerer Fläche; `BusyOverlay` umschließt als Slot den betroffenen
Bereich, bleibt halbdurchsichtig, sperrt den Kindinhalt während der
Laufzeit über `inert` und kündigt den Busy-Text höflich an;
`EmptyState` (berechtigtes Leerergebnis) und `NotFoundState`
(fehlender gesuchter Kontext) bleiben fachlich getrennt;
`TechnicalErrorState` zeigt ausschließlich neutralen deutschen Text,
eine optionale opaque Correlation-ID und Retry – niemals Exception,
Pfade, SQL- oder Toolausgabe – und ist über `tabindex="-1"`
fokussierbar und per `aria-labelledby` mit seiner Überschrift
verknüpft. Alle Animationen respektieren `prefers-reduced-motion`.)
sowie `Web.State` (flüchtiger Circuit-Zustand: `ToastState` der globalen
Toastregion, `WorkspaceState` als Circuit-Cache für ausgewählten Node,
Zielgruppe, Lese-Kontext, den geladenen Snapshot, `BaseSnapshotId` und
`ChangeVersion` sowie `KnowledgePageContextResolver` zur Auflösung von Current oder einer offenen `transactionId` über `ISnapshotRepository` und `ITransactionRepository`; er liefert Core-`ReadContext`, `KnowledgeContextViewModel` und die Kennung des geladenen Snapshots. Feature-Namespaces unter `Web.Features.*` sind `Knowledge`, `Drafts` und `Content`; Zielgruppenauswahl gehört zur Knowledge-Seite unter `Knowledge/Audiences`.

`Web.Workflow.WebWriteCoordinator` ist scoped pro Circuit und koordiniert den
ersten Web-Write sowie weitere Writes über `TransactionService` und vorhandene
Mutations-Use-Cases. Er schreibt die begonnene `transactionId` in die
Wissens-URL; `WorkspaceState` spiegelt den dort ausgewählten Kontext und besitzt
keine fachliche Autorität.

Die Web-Lesegrenze entkoppelt Razor-Komponenten vollständig von Domain-Typen:
Komponenten rufen Application Services direkt in-process per Dependency
Injection auf (keine REST-Schicht). Die Ergebnisse werden in den Knowledge- und Draft-Flächen über featurelokale ViewModels in UI-Zustände überführt. Fehlercodes, Warnungen, opake Cursors und `ChangeVersion` bleiben erhalten; Domain-Typen erscheinen nicht im Rendering. Das transportneutrale
Validierungsergebnis trägt die beim atomaren Working-Snapshot-Read gelesene
`ChangeVersion`; die Transaction-Ansicht verwendet diese Version für Findings
und markiert den Bericht bei einer bekannten neueren Workspace-Version als
stale.

Zustandsbehaftete Razor-Komponenten besitzen zugleich die Circuit- und
Renderergrenze: Lifecycle-, UI- und Interop-Methoden behalten nach eigenen
asynchronen Aufrufen den Dispatcher durch normales `await`. Fremdausgelöste
State- oder Host-Benachrichtigungen reihen Änderungen und Rendering über
`InvokeAsync` beim Circuit-Dispatcher ein. Der Build erzwingt diese Grenze mit
`KHTAI001`; Circuit-State- und Feature-Services bleiben dabei rendererfrei.

Der native `KnowledgeTree` hält hochfrequentes Drag-Feedback in seinem
komponentenlokalen JavaScript-Modul: Eine einzige Pointer-Event-Interaktion
mit Pointer-Capture berechnet die drei sichtbaren Drop-Zonen clientseitig,
blockiert konkurrierende Mutationen bis zum Abschluss und übergibt beim
tatsächlichen Drop nur Source, Target und Position über einen
`DotNetObjectReference` an die Razor-Komponente. Die Bindung ist pro DOM-
Element idempotent und wird beim Lifecycle-Dispose freigegeben; der Modulpfad
wird über `@Assets` mit der Static-Web-Assets-Fingerabdruckroute aufgelöst.
Diese delegiert die einzelne Mutation unverändert an den featurelokalen
`TreeMoveCoordinator`; sie enthält keine Geschäftslogik.

History-, Such- und Zielgruppen-Use-Cases bleiben in Core/Application und MCP verfügbar, werden aber nicht als parallele Web-Seiten angeboten. Die Web-Grenze stellt Knowledge- und Draft-Arbeitsabläufe bereit: `DraftPage` bindet `TransactionService`/`HistoryService` direkt in-process; Zielgruppenauswahl wählt nur die Knowledge-Perspektive. Web-Routen und alte URL-Grenzen sind im [Web-UI-Gesamtbild](WebUi.md) dokumentiert.

Für den Content-Editor liegt die lokale Buildgrenze unter
`src/KnowHowToAI.Server/Frontend`. `package.json` und das ausschließlich daraus
verwendete `package-lock.json` verwalten `@milkdown/crepe`, den Build-only-
Compiler `esbuild` sowie den Dev-Testläufer `vitest`;
`Web/Features/Content/content-editor.js` stellt
den Crepe-Konstruktor und die begrenzte Toolbar-Konfiguration bereit. `build.mjs`
löscht den vorherigen Stand und erzeugt deterministisch
`wwwroot/generated/content-editor/content-editor.js`.
Der Server bindet diesen Schritt vor jedem MSBuild `Build` und damit auch vor
`dotnet publish` ein. Der `ContentEditor` lädt sein featurelokales,
dynamisch isoliertes `ContentEditor.razor.js`; dessen einzige Blazor-Aufrufe
sind `mount`, `readMarkdown`, `focus` und `dispose`. `mount` registriert nur
Änderungs-/Fokus-Callbacks, aktiviert die erlaubten Crepe-Formate und deaktiviert
Top-Bar, Headings, Latex, Upload/ImageBlock und AI. Der Editor wird vor
Nodewechsel oder erneutem Mount disposed; persistiert wird ausschließlich der
beim expliziten Speichern gelesene kanonische Markdown eines expliziten
`Independent`-Contents in einer Working-Transaction; `Derived`-Content bleibt
bis M5.4 sichtbar read-only. Der Output wird als
Static Web Asset veröffentlicht, während `Frontend/node_modules` und
`wwwroot/generated` nicht versioniert werden. Node/npm werden ausschließlich
beim Build/Publish benötigt; der Server lädt weder zur Laufzeit noch über CDN
weitere Assets. `scripts/test-fast.ps1` führt die Vitest-Suite vor den
.NET-FastTests aus; sie prüft die eigene WeakMap-Instanzverwaltung, die
Callbackweitergabe und die Reihenfolge von Dispose/Remount. Vitest ist hier
erforderlich, weil der Adapter über reine Aufrufe hinaus Lifecyclezustand und
idempotente Fehlpfade besitzt. Der Editor reduziert Browser- und Office-Paste
lokal auf erlaubte Absätze, Formatierungen, Links, Listen, Tabellen, Code und
Blockquotes. Unsichere Links, Headings, unbekannte Elemente sowie Bilder werden
verworfen; jede Reduktion bleibt als `role="status"` sichtbar und wird als
ungespeicherte Änderung markiert. Die Clipboard-Verarbeitung arbeitet
ausschließlich auf den gelieferten Strings, lädt keine Bildquelle und ersetzt
den vollständigen Editorwert bei einer serverseitigen Ablehnung nicht.
Der Editor bietet zusätzlich einen expliziten Markdown-Quellmodus mit semantisch
beschrifteter Textarea. WYSIWYG und Quellmodus teilen denselben flüchtigen Wert,
Dirty-State, Save-/ChangeVersion-Pfad und die serverseitige Contentpolicy;
Quellmoduswechsel remounten den Crepe-Editor nur mit dem unveränderten aktuellen
Markdown und fokussieren die jeweils aktivierte Ansicht. Eine serverseitige
Ablehnung lässt den vollständigen Quellwert und den Dirty-State bestehen.
Der versionierte Golden Master wird zusätzlich im Browser mit dem gebündelten
Crepe über fünf aufeinanderfolgende Save-/Remount-Roundtrips geführt; jeder
Server-Readback wird gegen die Markdig-Semantik der Fixture geprüft.
Nach erfolgreichem Commit oder Discard setzt die Seite `WorkspaceState` und
den Kontextbereich auf den Current-Read-Context, navigiert zum Wissensbaum
unter Erhalt der Zielgruppe und bestätigt den Abschluss über die globale
Toastregion.

Der native Wissensbaum (`Web.Features.Knowledge`) nutzt den flüchtigen Circuit-State
`KnowledgeTreeState` als Lazy-Loading-Datenadapter. Er lädt den Root-Knoten über
`NavigationService.GetRootAsync` und Kindknoten ausschließlich bei Expand über
`NavigationService.ListChildrenAsync` (mit `Limit = 100` und unverändert
weitergereichten opaken Cursors). Höchstens zehn 100er-Seiten liegen gleichzeitig im
Circuit-Cache (`KnowledgeTreePageCache`). Bei der elften Seite greift eine LRU-Eviction:
Ein unselektierter Teilbaum wird geschlossen; liegen alle zehn Seiten auf dem Auswahlpfad,
wird die rootnächste Seite entfernt und ihr Kind auf dem Auswahlpfad zum `VisualRoot`
des Tree-Ausschnitts (der globale Pfad bleibt in den Breadcrumbs). Die Eviction wird
erst für eine erfolgreich übernommene neue Seite wirksam. Beim Seitenersatz sowie bei
Eviction werden alle nicht mehr erreichbaren Off-Path-Knoten samt Nachfahren aus Index,
Auswahl und Cursorhistorie bereinigt; im Circuit verbleiben ausschließlich der Root-Knoten,
die Knoten der höchstens zehn geladenen Seiten und die minimale Breadcrumb-Kette des
selektierten Knotens. Pro Parent sichert eine monotone Requestgeneration, dass konkurrierende
oder überholte Antworten (auch bei ignoriertem CancellationToken) weder Zustand, Ladeanzeige
noch Registrierung verändern können. Paging („Zurück“ / „Weitere“) ersetzt die sichtbare
100er-Seite vollständig über eine rein opaque Cursor-Historie, ohne Seiten zu einer
wachsenden Liste zusammenzufügen.

`KnowledgePage` und `KnowledgeTree` erhalten diesen Adapter ausschließlich über
den featurelokalen Vertrag `IKnowledgeTreeWorkspace`. Der Vertrag enthält nur
die für Rendering und Interaktion gemeinsame Baumansicht sowie die zugehörigen
Baumoperationen; Cache, Navigation und Request-Cancellation bleiben im
`KnowledgeTreeState`. Damit teilen die beiden Komponenten eine konkrete
Präsentationsgrenze und können sie mit einem schmalen Test Double prüfen.

Die sichtbare Verantwortungs- und Zustandslandkarte des Wissenscockpits steht im
[Web-UI-Gesamtbild](WebUi.md). Die technische Implementierungsgrenze bleibt
aufgeteilt: `KnowledgeTreeState` kapselt Lazy-Loading, opaque Cursor und Cache;
`KnowledgePage` orchestriert Current-/Transaktionsroute, Zielgruppe,
`WorkspaceState` und Page-Regionen; `NodeDocument` kapselt den lesenden
Dokumentfluss. Der wiederverwendte `NodeDocumentLoader` lädt ViewModel,
Fehler/NotFound und Export-URL für die Dokumentansicht und die weiterhin
eigenständig verfügbaren Bearbeitungs-Pane.

Die Baum- und Mutationsgrenzen sind am Code und in den
[repräsentativen Web-Tests](../tests/KnowHowToAI.Web.Tests/Features/Knowledge/)
belegt und werden nicht als zweite Seitenbeschreibung in dieser Architekturdatei
wiederholt.
Beim Neuladen oder direkten Einstieg ermittelt der `KnowledgeTreePathLoader` die
Ancestor-Kette metadata-first über `NavigationService.GetNodeAsync` und lädt danach
für jedes Segment die opaken 100er-Childseiten des Parents, bis das Segment sichtbar
ist. Nur ein vollständig erfolgreich geladener Pfad wird im Tree ausgewählt; ein
`NodeNotFound`-, `InvalidCursor`- oder `CursorExpired`-Ergebnis bleibt als erklärter
Node-/Zweigzustand sichtbar und erzeugt keine unsichtbare Auswahl. Bereits geladene
Off-Path-Teilbäume gehören ausdrücklich nicht zur Rekonstruktion und bleiben der
Zehn-Seiten-Eviction unterworfen.

`NodeDocument` und `NodeDetailsPane` nutzen denselben `NodeDocumentLoader` für
Navigationsergebnisse, UI-ViewModel, NotFound und Fehler. Die neue
Knowledge-Route rendert nur `NodeDocument`; `NodeDetailsPane` und die Editor-
Komponenten bleiben für ihre geplanten Mutationsslices erhalten. Im
Working-Kontext delegieren die Editor-Komponenten Node-/Content-Mutationen mit
`ChangeVersion` an die Application-Grenzen und laden Tree, Auswahl und Details
nach Erfolg neu. Read-only- und Dirty-State-Verantwortung sowie die sichtbaren
Übergänge sind im [Web-UI-Gesamtbild](WebUi.md) und am Code belegt.

Die Weboberfläche besitzt keinen Markdown-Download-Endpunkt. Core-/MCP-Markdown-Export und MCP-Suche/Historie bleiben eigenständige transportneutrale Application-Verträge. Die Weboberfläche bildet ausschließlich Knowledge- und Draft-Arbeitsabläufe ab; die genaue Routengrenze steht im [Web-UI-Gesamtbild](WebUi.md).

Gemeinsame Feedback-, Dialog- und Toast-Komponenten bleiben rein darstellende
UI-Grenzen; ihre routeübergreifende Ownership und Zustandsverwendung stehen im
[Web-UI-Gesamtbild](WebUi.md). Application-Aufrufe und fachliche Entscheidungen
liegen in den Feature-Seiten bzw. Services, nicht in diesen Shared-Komponenten.

Die sichtbare Shell-, Kontext- und Seitenregionen-Verantwortung steht im
[Web-UI-Gesamtbild](WebUi.md). `Web/Components/Layout` bleibt technisch in
`Shell`, `Navigation` und `PageRegions` getrennt; `PageRegionState` ist die
rendererfreie Slot-Grenze. `ContextSelectorDialog`, `ContextSelectorDialog`, `ContextSelectionForm`, `WorkspaceState` und
`NavigationProtection` bleiben featureübergreifende Adapter für URL-Kontext und
flüchtigen Circuit-State. Reconnect-Interop bleibt auf `App.razor` und das
frameworkseitige Circuit-Verhalten begrenzt. Layout- und Umbruchnachweise bei
den unterstützten Desktopbreiten stehen in [Manuelle UI-Abnahme](Manuelle-UI-Abnahme.md)
und der [Web-UI-Regel](../.agents/rules/WebUiHtmlCss.mdc); diese Datei
wiederholt keine sichtbare Shellbeschreibung.
`wwwroot/css/app.css` enthält den neutralen Reset, die zentralen
Design-Tokens des Business-Themes als CSS Custom Properties (Farben mit
Primary `#2563EB`, Text `#111827`, Page `#F8FAFC`, Surface `#FFFFFF` sowie
Erfolgs-, Warnungs- und Fehlerpaaren; Abstandsskala `4/8/12/16/24/32px`;
Radien `4/8px`; Surface-Schatten; Basistext `16px` mit Zeilenhöhe `1.5` und
der Systemschriftkette `Segoe UI, Arial, sans-serif`; Fokusring mit `3px`
Stärke und `2px` Abstand) und frameworkweite Basisklassen für Fokusring und
deaktivierten Zustand. Komponenten-CSS ist scoped und referenziert diese
Tokens ausschließlich über `var(...)`; außerhalb von `app.css` sind keine
Hex-Farbliterale zulässig. Es gibt kein zweites Theme und kein Dark Theme. Die
Shell bindet ausschließlich lokale eigene Ressourcen ein,
keine CDN-, Cloud- oder Telemetrie-Ressourcen.

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
- Die MCP-Handler-Signaturen sind flach (Selektor-, Zielgruppen- und Paging-Parameter
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
Application-Persistence-Ports. Das Test-Fixture `TestSupport/UiBasisShowcase`
rendert ausschließlich in diesem Projekt die native UI-Basis (beschriftetes
Formular mit Validierung, Button, nativer Dialog über JS-Isolation, kleine
Tabelle, Inlinehinweis, Toast) und besitzt keine Route im Produkt. Das Fixture
`TestSupport/DesignTokensShowcase` belegt per Headless Chrome berechnete Stile und
den Light-Theme-Screenshot; der Test führt vor dem Chrome-Start den Chrome-Stable-Preflight
aus `KnowHowToAI.TestSupport` aus und schlägt deshalb ohne installiertes Chrome mit
einer klaren deutschen Fehlermeldung fehl.
Der isolierte Seitenbasisvertrag liegt in
`Components/Shared/PageFrameTests.cs`; der routeübergreifende Browservertrag in
`KnowHowToAI.BrowserTests/ReadOnly/PageFrameSmokeTests.cs` prüft für die zehn
Routenvarianten genau ein `h1`, das sole-`main`-Landmark, Shell-Innenkanten,
Computed Styles, die Desktop-Viewports 1280 × 720 und 1024 × 720, Navigation
offen/geschlossen und Overflow.
`KnowHowToAI.BrowserTests` startet die veröffentlichte Server-EXE als Black Box
mit Google Chrome Stable im headless Interactive-Server-Smoke; es referenziert
kein Produktionsprojekt. Der Serverstart erfolgt einmal pro Testkollektion über
eine gemeinsame Kollektions-Fixture; Reconnect-Smokes behalten bewusst eigene Hosts.
Die funktionalen Smokes arbeiten dabei gegen die von der manuellen
SQL-Integrationssuite getrennte Browser-Workflowdatenbank; die visuellen
Shell-Smokes starten ihren eigenen Host gegen einen minimalen, stabilen
Browserbestand gemäß [Konfiguration und Betrieb](Konfiguration-und-Betrieb.md#konfigurationstrennung).
Der Testhost wartet nach dem Serverstart auf eine
erste HTTP-Antwort unter der Zieladresse, bevor die Browsernavigation beginnt;
die Prozessausgabe wird dabei begrenzt und redigiert im Speicher gesammelt und
ausschließlich für Diagnosen fehlgeschlagener Läufe herangezogen.
Layout-Smokes belegen für 1280 × 720 und 1024 × 720 die Landmark-Struktur,
das Nebeneinander der Spalten beziehungsweise das Ein-/Ausklappen über
beschriftete Buttons per Mausklick sowie fehlenden Horizontalüberlauf und
Seiten-Scrollbarkeit mit langem Testinhalt per Mausrad. Ein visueller Smoke nimmt je
Viewport erst nach den Verhaltensassertionen einen maskierten Light-Theme-
Screenshot der Shell auf und vergleicht ihn mit der versionierten Baseline
unter `tests/KnowHowToAI.BrowserTests/TestSupport/Baselines/`; Abweichungen
erfordern eine manuelle Diff-Prüfung, eine automatische Baselineaktualisierung
im regulären Lauf findet nicht statt. Da sein Host ausschließlich den minimalen
Read-only-Bestand nutzt, können Historien-, Release- und Transaction-Workflows
die Pixelbaseline nicht verändern.

Die Testablagen sind nach Prüfgegenstand benannt: Komponententests liegen in `KnowHowToAI.Web.Tests` unter `Components/{Layout,Shared}`, `Features/{Knowledge,Drafts}` und `TestSupport/`. Vollständige Benutzerabläufe entstehen in `KnowHowToAI.BrowserTests` unter `ReadOnly/`, `Transactions/` und `Drafts/`; der geänderte Routenumfang und die Browser-Nachweise stehen im [Web-UI-Gesamtbild](WebUi.md).

Der Browser-Testlauf `Category=UiAudit` ist ein ausdrücklich aktivierter,
separater Diagnose-Runner. `scripts/capture-ui-audit.ps1` startet ihn gegen
die dedizierte minimale Visual-Shell-Browserdatenbank und erzeugt für den
festen Desktop-Viewport (1280 × 800) semantisch benannte PNGs unter
`temp/ui-audit/<yyyy-MM-dd_HH-mm-ss>/` einschließlich eines
`manifest.json`. Der Test wird vom normalen Discovery-Lauf gefunden, ohne
Aktivierung aber übersprungen und startet dabei keinen Host oder Browser;
volatile Werte werden vor der Aufnahme maskiert und jede Aufnahme folgt auf
Web-first-Verhaltensassertionen. Die temporären Artefakte sind keine
visuellen Baselines.
Der gemeinsame Lauf umfasst siebzehn gezielte, semantisch benannte Aufnahmen:
Knowledge-Einstieg, Zielgruppenauswahl, Root, Lesen, Metadaten, Editor und
Editorfläche, technische Details sowie Lesen und Editor für Fallback- und
Derived-Content. Die Editorflächen und der Abschlussbereich des Entwurfs werden
als gezielte Viewport-Aufnahmen nach Scrollen separat gezeigt. Der Working-Editor
mit fehlendem eigenen Fallback-Content, die Entwurfsübersicht und das
Entwurfsdetail sind ebenfalls enthalten. Jede PNG zeigt den Viewport 1280 × 800;
es gibt keine schmale Breitenmatrix und keine FullPage-Aufnahme. Das Manifest
dokumentiert Route, Zustand, Viewport und Browser; die PNGs werden nach dem
Lauf gemeinsam manuell auf unerklärten Drift geprüft und bleiben unter `temp/`
außerhalb des Commits.

`KnowHowToAI.TestSupport` bündelt projektübergreifende Testinfrastruktur: die
Repository-Root-Ermittlung (`TestRepositoryRoot`), Wegwerf-Verzeichnisse unter
`temp/<prefix>_<random>` mit Selbstaufräumung beim Verlassen des `using`
(`TestTempDirectory`, Dispose wartet den Image-Section-Nachlauf frisch
beendeter Prozesse ab), das Schreiben persistenter Messberichte nach `temp/`
(`TestMeasurementReports`) und das sprachabhängige Parsen von
SET STATISTICS-Ausgaben (`SqlStatisticsMessages`). Gemeinsame Infrastruktur
wird nur ergänzt, wenn mindestens zwei Testprojekte sie tatsächlich benötigen.

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
