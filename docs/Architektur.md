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
schmaler JS-Isolation in `AppDialog.razor.js` und `ConfirmationDialog`),
`Web/Components/Shared/Feedback` (Status-, Warn- und Toastdarstellungen)
und `Web/Components/Shared/States` (wiederverwendbare Lade-, Leer- und
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
Zielgruppe, Lese-Kontext, `BaseSnapshotId` und `ChangeVersion` sowie `WebReadContextResolver`
zur Validierung und Auflösung von `transactionId`, `snapshotId` und
`releaseId` über `ITransactionRepository` und `IReleaseRepository` auf Core-`ReadContext` und `KnowledgeContextViewModel`) und
die Feature-Namespaces unter `Web.Features.*` (`Knowledge`, `Audiences`,
`Search`, `History`, `Dashboard`, `Transactions`).

Die Web-Lesegrenze entkoppelt Razor-Komponenten vollständig von Domain-Typen:
Komponenten rufen Application Services direkt in-process per Dependency
Injection auf (keine REST-Schicht). Die Ergebnisse werden über statische
Mapper (`KnowledgeNavigationMapper`, `AudienceMapper`, `SearchMapper`,
`HistoryMapper`) in unveränderliche UI-ViewModels überführt. Fehlercodes,
Warnungen, opake Cursors und `ChangeVersion` bleiben dabei vollständig
erhalten; Domain-Typen erscheinen nicht im Rendering. Das transportneutrale
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

`Web.Features.History` stellt unter `/history` getrennte paginierte Listen für
committed Snapshots und Releases bereit. Snapshot-Zeilen zeigen neben Zeit und
Basis die metadata-first gelesene erzeugende Transaction einschließlich Actor,
Client, Purpose und Commit-Nachricht, soweit der Stand nicht der initiale
Snapshot ist. `HistoryPage` hält ausschließlich Query-Parameter und die Auswahl
der Vergleichssnapshots. Die featurelokalen Komponenten `SnapshotList`,
`SnapshotDiffPanel` und `ReleasePanel` besitzen jeweils ihren passenden
in-process-Servicezugriff und kommunizieren Auswahl sowie Snapshotseite über
immutable Parameter und `EventCallback`s. Sie verwenden ausschließlich History-
ViewModels und übergeben bei Navigation den jeweiligen `snapshotId`- oder
`releaseId`-Queryparameter an den bestehenden Web-Read-Context-Resolver. Working
Transactions erscheinen dort bewusst nicht. Zwei ausgewählte committed Snapshots
werden im `SnapshotDiffPanel` als strukturierter, cursor-paginierter Netto-Diff
dargestellt; die UI zeigt die Kategorien Zielgruppen, Zielgruppenauflösungen, Nodes,
Contents und Dependencies mit fachlichen Schlüsseln sowie Vorher-/Nachher-Werten.
Ein Link aus der Node-Detailansicht setzt den optionalen `nodeId`-Filter;
dieser begrenzt den Vergleich auf die fachlich zugehörigen Node-, Content- und
Dependency-Änderungen. Die Web-Grenze bietet dabei keine Merge- oder Reapply-
Operation.

`Web.Features.Transactions` stellt unter `/transactions` die offenen
Transactions und unter `/transactions/{transactionId}` ihren Arbeitsbereich
bereit. Die Detailseite ruft `TransactionService` und `HistoryService` direkt
in-process auf und führt im ersten Weiterarbeiten-Abschnitt den vorhandenen
Working-Einstieg `Im Wissensbaum öffnen`; Zielgruppenpflege und Übersicht bleiben
sekundäre Folgewege. Sie zeigt außerdem Validierung sowie cursor-paginierten Netto-Diff. Commit
und Discard werden jeweils über einen expliziten nativen Bestätigungsdialog
ausgelöst; der Commitdialog übergibt eine optionale Commit-Nachricht. Während
einer Abschlussanfrage sind beide Aktionen gesperrt. Ein abweichender lokaler
`ChangeVersion`-Stand, ein geschlossener Status oder ein fachlich abgelehnter
Commit bleibt als verständlicher Seitenfehler im Working Context sichtbar.

Bei `SnapshotConflict` zeigt dieselbe Detailseite die vom Fehlervertrag gelieferten
Base- und Current-Snapshot-IDs und bindet den schreibgeschützten Snapshot-Diff ein.
Sie kann anschließend eine leere Transaction auf dem Current Snapshot starten,
damit der Benutzer die geprüften Änderungen manuell erneut anwendet. Die
Web-Grenze kopiert, merged oder rebased dabei keine Änderungen und verwirft die
konfliktbehaftete Transaction nicht implizit.

`Web.Features.Audiences` stellt unter `/audiences` die Zielgruppenpflege bereit. `AudiencesPage` löst
ausschließlich den Read-Kontext über den gemeinsamen `WebReadContextResolver` auf,
spiegelt `PageRegionState` und `WorkspaceState` und reicht `ReadContext` sowie die
aktuelle `ChangeVersion` an den zustandsbehafteten `AudienceEditor` weiter. `AudienceEditor`
lädt die Zielgruppenliste einschließlich opaker Paging-Fortsetzung über
`NavigationService`, hält Formular- und Löschdialogzustand und ruft für Erstellen,
Umbenennen und Löschen ausschließlich `AudienceMutationService` auf. Die drei Aktionen
sind nur bei einer offenen Working Transaction sichtbar und aktiv; erfolgreiche
Antworten werden lokal aus dem Mutationsergebnis projiziert. Über ein schmales
`EventCallback<long>` meldet der Editor die neue `ChangeVersion` an die Page, die
damit `WorkspaceState` aktualisiert und den Kontext als dirty markiert.
`AudienceInUse`, `AudienceNameRequired`, `AudienceNotFound` und `ChangeVersionConflict` werden
mit ihrem stabilen Fehlercode und den strukturierten Details am Editor-Formular
angezeigt; ein Fehler lässt Eingaben und Working-Zustand unverändert. Current-,
Snapshot-, Release- und abgeschlossene Transaction-Kontexte bleiben schreibgeschützt.
Die Seite bearbeitet keine Resolution Orders; das ist ein separater Zielgruppen-Leaf.

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

Die Benutzeroberfläche der Hierarchienavigation wird durch die routable Page
`KnowledgePage` (`/knowledge`, `/knowledge/{NodeId:guid}`), den nativen
`KnowledgeTree` und `Breadcrumbs` gebildet. `KnowledgeTree` setzt die
WAI-ARIA-Treeview-1.2-Semantik um (`role="tree"`, `role="treeitem"`, `role="group"`,
`aria-level`, `aria-selected`, `aria-expanded`) und steuert den aktiven Knoten über
einen roving `tabindex` (`0` auf genau einem sichtbaren Knoten, `-1` auf allen anderen).
Die Tastaturnavigation unterstützt `ArrowUp`/`ArrowDown` (sichtbare Knoten),
`ArrowRight` (Expand bzw. erstes Kind), `ArrowLeft` (Collapse bzw. Elternknoten),
`Home`/`End` (erster/letzter sichtbarer Knoten) sowie `Enter`/`Space` (Auswahl).
Knoten-Zustände (selektiert, geladen, teilweise geladen, leer, ladend, fehlerhaft) werden
klar differenziert; Paging-Buttons („Vorherige 100 Einträge“, „Weitere 100 Einträge“)
erscheinen innerhalb des jeweiligen Teilbaums. `Breadcrumbs` bildet den hierarchischen
Pfad bis zum aktuellen Knoten ab (`aria-current="page"` auf dem letzten Element) und
erlaubt direkte Rücknavigation zu übergeordneten Ebenen. Die Auswahl eines Knotens
aktualisiert die Route `/knowledge/{NodeId}` unter Erhalt bestehender Query-Parameter.
Nach einer erfolgreichen Node-Mutation projiziert `KnowledgePage` das
Mutationsergebnis in `KnowledgeTreeState`, `WorkspaceState`, Breadcrumb/Detail
und dieselbe Route: Create child/root und Update wählen den Ergebnis-Node, Delete
den aktiven Parent oder die bestehende Root-Route als Fallback. Der vorhandene
ReadContext-Selektor und `audienceId` werden dabei unverändert weitergeführt.
Beim Neuladen oder direkten Einstieg ermittelt der `KnowledgeTreePathLoader` die
Ancestor-Kette metadata-first über `NavigationService.GetNodeAsync` und lädt danach
für jedes Segment die opaken 100er-Childseiten des Parents, bis das Segment sichtbar
ist. Nur ein vollständig erfolgreich geladener Pfad wird im Tree ausgewählt; ein
`NodeNotFound`-, `InvalidCursor`- oder `CursorExpired`-Ergebnis bleibt als erklärter
Node-/Zweigzustand sichtbar und erzeugt keine unsichtbare Auswahl. Bereits geladene
Off-Path-Teilbäume gehören ausdrücklich nicht zur Rekonstruktion und bleiben der
Zehn-Seiten-Eviction unterworfen.

Die Read-only Node-Detailansicht (`NodeDetails`) zeigt Titel, Beschreibung, Position,
Zielgruppe (inklusive Fallback-Kennzeichnung mit Pfeil und aufgelöster Zielgruppe), Verfügbarkeit,
Freshness-Status, Inhaltsmodus (`Independent` vs. `Derived`), optionale Revisions-ID
sowie bei wirksam aufgelöstem abgeleitetem Inhalt (`Derived`) dessen direkt
gespeicherte Quellrevisionen (`SourceRevisions`). `NavigationService.GetNodeAsync`
liefert dafür je direkter Dependency Source-Node, Source-Zielgruppe, gespeicherte
Source-Revision und deren aktuell ausgewertete Freshness; die Liste ist keine
transitive Provenienzauflistung. Die featurelokale `NodeDetailsPane` kapselt
Laden, Fehler- und NotFound-Zustand, Markdown-Download-URL sowie Darstellung;
`KnowledgePage` bleibt für Route, Query, Zielgruppenwahl und sichtbaren Page-Zustand
zuständig. Die Pane und MCP mappen dasselbe transportneutrale Ergebnis, auch wenn
der wirksame Derived Content aus einer Fallback-Zielgruppe stammt.
Im aktiven Transaction-Kontext ergänzt `NodeMetadataEditor` diese Ansicht um
explizite Formulare für Titel, Beschreibung und eine Child-Node unter dem
ausgewählten Parent. Die Komponente ruft ausschließlich
`NodeMutationApplicationService` auf, übergibt die gelesene `ChangeVersion` und
initialisiert nach einer bestätigten Mutation Tree, Auswahl und Details aus dem
Working Snapshot neu. Bei `ChangeVersionConflict` bleibt der Formzustand sichtbar
und der serverseitige Fehler wird am Formular angezeigt; Korrekturen erfolgen
bewusst oder durch Discard, ohne globalen Undo-Stack.
Ist der vollständig geladene Working Tree leer, zeigt `KnowledgePage` stattdessen
den `RootNodeEditor`; ohne offene Transaction oder bei Lade- beziehungsweise
Fehlerzustand bleibt diese Aktion unsichtbar. Der Editor erfasst Titel und
optionale Beschreibung, ruft `NodeMutationApplicationService.CreateAsync` mit
`ParentNodeId = null` und der gelesenen `ChangeVersion` auf und lädt nach einer
bestätigten Anlage den Tree neu; der neue Root wird unmittelbar ausgewählt und
seine Details angezeigt.
`NodeDeletionEditor` lädt vor jeder globalen Löschung den Working-Stand erneut
über `NodeDeletionPreviewService` und zeigt Ziel, Root-Auswirkung, direkten und
vollständigen Teilbaum, explizite Inhalte sowie entfernte oder als Provenienz
erhaltene Dependencies. Children verlangen die explizite Teilbaumwahl; erst die
anschließende destruktive Bestätigung ruft `NodeMutationApplicationService` mit
der Vorschau-`ChangeVersion` auf. Bei `ChangeVersionConflict` bleibt die
Löschung aus und die UI fordert eine neue Löschprüfung an. Nach Erfolg lädt
`KnowledgePage` den Tree neu und selektiert den Parent oder bei einer Root-Löschung
keine Node.
Der Inhaltsbereich rendert Markdown sicher über `SafeMarkdownRenderer` (gemäß O-020:
kein Raw-HTML-Rendering via `DisableHtml`, Neutralisierung von JavaScript-, Data- und
File-Links sowie externen Bild-URLs zur Vermeidung von Netzwerk-Requests; keine
Bearbeitungscontrols). Bei fehlendem Inhalt wird ein expliziter Hinweis angezeigt;
Lade- und Fehlerzustände nutzen `LoadingState`, `InlineAlert` bzw. `NotFoundState`.
Für den ausgewählten Knoten bietet die Ansicht außerdem einen Markdown-Teilbaumdownload.
Der schmale Browserendpunkt `GET /downloads/markdown` erhält `nodeId`, `AudienceId` und
höchstens einen Read-Context-Selektor, löst diesen über `WebReadContextResolver` auf
und delegiert an `MarkdownExportService`. Erfolgreiche Antworten sind UTF-8-Markdown
als Attachment mit `Cache-Control: no-store`; der Dateiname besteht aus bereinigtem
Node-Titel und Zielgruppe. Fehler werden als RFC-9457-`ProblemDetails` mit stabilem
Fehlercode und Correlation-ID ausgeliefert, niemals als Teil-Datei. Bekannte
fachliche Fehlercodes werden explizit auf `400`, `404` oder `409` abgebildet;
unbekannte Codes und unerwartete Ausnahmen liefern neutral `500` mit einem
endpunktspezifischen technischen Fehlercode. Ein Request-Abbruch wird nicht als
Serverfehler protokolliert.

Die routable Seite `SearchPage` (`/search`) verwendet mit der globalen Zielgruppe und dem
aus Query-Parametern aufgelösten Lesekontext direkt den transportneutralen
`SearchService`. `SearchForm` hält nur den unpersistierten Suchtext;
`SearchResults` rendert genau eine Trefferseite mit Snippet, hierarchischem Breadcrumb
und Navigation zur kanonischen `/knowledge/{NodeId}`-Route unter Erhalt der
Kontext-Query. Der featurelokale Cursor wird unverändert an den Use Case
zurückgegeben und nie dekodiert. Ein neuer Suchauftrag oder Kontextwechsel bricht den
vorherigen Request ab; verspätete Ergebnisse werden nicht gerendert. Die ergänzende
`SearchBreadcrumbLoader`-Grenze liest Pfadtitel einzeln über den bestehenden
`NavigationService`, sodass Razor weiterhin nur Search-ViewModels und keine
Domain-Typen rendert.

`KnowledgeFilter` hält die Auswahl von aufgelöster Zielgruppe, Availability,
Freshness und Findings ausschließlich featurelokal. Werte innerhalb einer
Facette werden als Oder, verschiedene Facetten als Und an `SearchService`
übergeben. Ein Filterwechsel verwirft Trefferseite und Cursor; der Tree bleibt
unverändert. Die Navigation eines Treffers lädt dessen Pfad über den bestehenden
Tree-Workspace und setzt den Fokus auf das ausgewählte Treeitem.

Die Warnungs-, Bestätigungs- und Änderungszustände teilen sich den
wiederverwendeten Vertrag `AlertKind` (`Info`, `Erfolg`, `Warnung`,
`Fehler`) und rendern Inhalt und Farbe über die zentrale
`AppStatus`-Darstellung, sodass nie nur Farbe die Bedeutung trägt:
`InlineAlert` bleibt beim auslösenden Inhalt und kündigt Fehler sofort
(`role="alert"`), alle übrigen Stufen höflich (`role="status"`) an.
`StatusBanner` gilt für die gesamte Seite statt für einen einzelnen
Inhalt, verwendet dieselbe Ankündigungsregel und markiert die Stufe
zusätzlich über eine farbige Kante. `WorkingIndicator` zeigt eine
laufende Änderung am Ort des Geschehens als Text plus Icon
(`role="status"`, Animation ruht bei `prefers-reduced-motion`) und
rendert im Ruhezustand nichts; der ungespeicherte Zustand bleibt in der
zentralen `AppStatus`-Darstellung, blockierende Läufe im `BusyOverlay`.
`ConfirmationDialog` baut auf dem `AppDialog`-Wrapper auf: Titel, kurze
Auswirkung (über `aria-describedby` mit der primären Aktion verknüpft),
primäre Aktion und „Abbrechen“. „Abbrechen“ ist das erste Element und
erhält deshalb beim Öffnen den Fokus; Fokusfalle und Fokusrückgabe
übernimmt die Dialogisolation, Escape entspricht „Abbrechen“. Eine
destruktive Aktion ist zusätzlich zur Beschriftung durch Fehlerfarbe und
Warnicon eindeutig; eine Texteingabe zur Bestätigung gibt es nicht.
Während eines Requests läuft der Bestätigungs-Callback höchstens einmal
und sind beide Aktionen deaktiviert; der programmatische Abschluss nach
bestätigter Aktion löst keinen Abbruch aus.

Die einzige globale `ToastRegion` hostet `MainLayout` genau einmal und
liest den flüchtigen Circuit-Zustand `ToastState` (unter `Web/State`,
scoped pro Circuit): Sie bestätigt ausschließlich nichtkritische
abgeschlossene Aktionen, kündigt neue Meldungen über die dauerhaft
vorhandene höfliche Live-Region (`aria-live="polite"`) an und verschiebt
den Fokus nie. Meldungen laufen nicht zeitgesteuert ab; sie bleiben mit
Icon plus Text bis zum Schließen durch den Benutzer sichtbar und werden
mit dem Ende des Circuits verworfen. Kritische Informationen erscheinen
zusätzlich oder ausschließlich im Seitenzustand über `InlineAlert` oder
`StatusBanner` und nie nur in der Toastregion.

Das
Verzeichnis
`Web/Components/Layout` gliedert seine Bausteine in `Shell` (Hauptlayout
und Reconnect-Oberfläche), `Context` (Wissenskontext und -auswahl) und
`PageRegions` (Breadcrumbs, Aktionen, Navigation und Seitenbereichs-Slot):

- `MainLayout` zeichnet den Kopf mit der Produktbezeichnung als reine
  Textwortmarke ohne Logo-Asset, das Sprungziel, genau ein
  `main`-Landmark für den Seiteninhalt, optional eingerückte
  Seitenbereiche und die einzige globale Toastregion. Landmarks:
  Sprunglink „Zum Hauptinhalt springen“ als
  erstes Element – seine Aktivierung legt den Fokus auf das `main`-Landmark,
  weil die erweiterte Blazor-Navigation den Hash-Link sonst abfängt, ohne
  den Fokus zu verschieben –, `header`, `nav` mit zugänglichem Namen
  `Hauptnavigation` mit den vier bestehenden Zielen Start (`/`), Suche
  (`/search`), Transactions (`/transactions`) und Zielgruppen (`/audiences`), `nav`
  `Breadcrumbs`, der Seitenaktionsbereich und optional `aside` `Kontext`.
  Die vier Ziele erscheinen als ruhig gruppierte Linkflächen; der aktive
  Route-Kontext wird ausschließlich visuell über den bestehenden `NavLink`-
  Status markiert. Die Navigation führt keine zusätzliche Berechtigungs- oder
  Fachauswahl ein.
- Fachseiten hängen Breadcrumbs, Aktionen und Kontext ohne eigenes
  Seitenraster über den scoped Slot `PageRegionState` ein; leere Bereiche
  belegen keinen Platz und erhalten keine Dummytexte.
- Fachseiten liefern über denselben Slot den Vertrag
  `KnowledgeContextViewModel` (immutable `record` unter
  `Web/Components/Layout/Context`) für die globale Wissenskontextleiste: Art des
  Lese-Kontexts (`Current`, `Snapshot`, `Transaction`, `Release`), optionale
  ID/Bezeichnung, optionale Zielgruppe, `IsDirty` und optionale `BaseSnapshotId`. Bei
  den M4-Strukturformularen ist `WorkspaceState.IsDirty` die zentrale flüchtige
  Quelle; `KnowledgePage` projiziert ihn in diesen Slot-Vertrag. Die Komponente
  `KnowledgeContextBar` rendert daraus genau eine globale Kontextleiste im
  Kopfbereich nahe der Wortmarke – als Text und Status ohne Selektor, Links
  oder Mutation; sie spiegelt `IsDirty` als `data-ktai-dirty`-Attribut ihres
  Wurzelelements; eine fehlende Zielgruppe erscheint neutral als „Keine Zielgruppe
  ausgewählt“, der Dirty-Zustand nur bei Bedarf als „Ungespeicherte
  Änderungen“ mit Icon plus Text und bei Transactions der Base-Snapshot als
  eigenes Meta-Item. Nicht gelieferte Angaben erscheinen nicht;
  die Dashboard-Seite mappt den tatsächlichen Seitenkontext Current ohne
  Zielgruppe und ohne `IsDirty`. Domain-Typen und der `WorkspaceState` bleiben
  bewusst nicht Teil des Markup-Vertrags.
- `NavigationProtection` schützt den zentralen ungespeicherten Formularzustand
  (`WorkspaceState.IsDirty`, im Slot als `IsDirty` gespiegelt) über
  `NavigationLock` und einen `ConfirmationDialog` bei interner Blazor-Navigation
  sowie über das native `beforeunload`-Ereignis bei externer Navigation; `MainLayout`
  bindet die abgegrenzte Shell-Komponente ein;
  bereits in einer Transaction persistierte Änderungen verbleiben in der Datenbank
  und sind per URL rekonstruierbar.
- `ContextSelectorDialog` hostet ausschließlich den nativen Dialog-Lifecycle.
  Das featurekonkrete `ContextSelectionForm` hält den unpersistierten
  Auswahlentwurf, validiert und bildet die kanonische Ziel-URL. Die beiden
  schmalen UI-Grenzen `IContextSelectionCatalog` und
  `IContextSelectionAudienceCatalog` übersetzen Application-Ergebnisse in
  darstellbare Auswahlwerte; dadurch kennt weder Dialoghost noch Formular
  Release-, Dashboard- oder `NavigationService` direkt.
- Ab 1280 CSS-Pixeln (vom schmalen Modul `MainLayout.razor.js` über
  `matchMedia` gemeldet) stehen Navigation, Arbeitsfläche und optionaler
  Kontextbereich nebeneinander; die Arbeitsfläche nutzt
  `minmax(0, 1fr)`, um nicht unter `min-width`-Defaults zu überlaufen.
- Die Navigation startet im Desktop offen. Ein dauerhaft sichtbarer
  Drei-Linien-Menübutton oben links in der App-Leiste steuert sie in allen
  Breiten über den zugänglichen Zustandsnamen sowie `aria-expanded` und
  `aria-controls`; beim Desktop-Schließen gibt die Navigation ihre Spalte an
  die Arbeitsfläche frei.
- In kompakten Breiten steuert derselbe zugänglich benannte Kopfbutton die
  Navigation als überlagerndes Panel unterhalb des Kopfs; der Kontextbereich
  besitzt bei Bedarf einen eigenen Kopfbutton. Öffnen setzt den Fokus auf den
  jeweiligen Bereich, Schließen (Kopfbutton, Escape) gibt ihn an den Auslöser
  zurück, und Escape schließt nur den zuletzt geöffneten überlagernden Bereich.
  Keine fixierten Höhen für normalen Inhalt; die Seite und die Arbeitsfläche
  per Bildlauf im Dokument. Unterhalb von 1024 besteht nur die Zoom-/Reflow-
  Anforderung, keine Smartphone-Navigation.
- Der Verbindungsverlust des Interactive-Server-Circuits wird durch die
  offizielle .NET-10-Reconnect-Oberfläche behandelt: Die Komponente
  `ReconnectModal` (unter `Web/Components/Layout/Shell`, aus `App.razor`
  eingebunden) stellt das Markup mit der ID `components-reconnect-modal`
  bereit, auf die die Blazor-Runtime die Klassen `components-reconnect-*`
  setzt und das Ereignis `components-reconnect-state-changed` sendet; das
  schmale Modul `ReconnectModal.razor.js` passt ausschließlich Darstellung,
  Fokus und Texte an. Die Verbindungslogik bleibt im Framework
  (`Blazor.reconnect`/`Blazor.resumeCircuit`) mit dessen begrenztem
  Retryplan (höchstens 30 Versuche, exponential ansteigende Abstände); es
  gibt keine eigene SignalR-Verbindung und keine unbegrenzte Retryschleife.
  Der modale Dialog blockiert sämtliche Interaktion hinter einem
  halbtransparenten Hintergrund, der die letzte Ansicht sichtbar hält. Der
  Zustand wird als Text plus Icon über eine höfliche Live-Region
  angekündigt: „Verbindung wird wiederhergestellt …“ während der
  Wiederherstellung (mit Countdown bis zum nächsten Versuch), „Verbindung
  getrennt“ mit „Erneut versuchen“ nach vorläufigem Fehlschlag sowie „Sitzung
  nicht mehr verfügbar“ mit „Seite neu laden“ bei abgelehntem/abgelaufenem
  Circuit; Escape schließt den Dialog nicht. Beim Öffnen liegt der Fokus
  deterministisch auf dem Dialog, in den Handlungsstates auf der sicheren
  nächsten Aktion. Ein erfolgreicher Reconnect schließt den Dialog ohne
  fachlichen Erfolgshinweis. Ein Reload warnt nur bei tatsächlich ungespeicherten
  Änderungen: `beforeunload` liest das Attribut `data-ktai-dirty` der
  Kontextleiste zum Ereigniszeitpunkt; fehlt das Element, gilt die Seite als
  nicht dirty; es existiert kein `window`-Flag mehr. `NodeMetadataEditor` (für
  Edit und Child-Create), `RootNodeEditor` (für Root-Create) und der
  `ContentEditor` setzen den wertbasierten Zustand nur bei tatsächlich
  abweichenden Eingaben, räumen ihn bei erfolgreichem Save, Cancel und Dispose
  und erhalten ihn bei fehlgeschlagenem Save; beim Content-Save wird der
  kanonische Markdown erst über `readMarkdown` gelesen und die Mutation mit
  `expectedChangeVersion` ausgeführt. Eine serverseitige Ablehnung überschreibt
  den Editorwert nicht, und eine persistierte Transaction gilt nie als
  ungespeichert.
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
Ein Dialog-Smoke belegt die Tastaturfolge
`Enter`, `Tab`, `Shift+Tab`, `Escape` einschließlich Fokusfalle und
Fokusrückgabe gegen dieselben Serverressourcen und dass beim Laden keine
Drittanbieter-Origin angefordert wird. Ein Layout-Smoke belegt für 1280 × 720
und 1024 × 720 die Landmark-Struktur, das Nebeneinander der Spalten beziehungsweise
das Ein-/Ausklappen über beschriftete Buttons, die Fokusübergabe an die
Bereichsüberschrift mit Fokusrückgabe an den Auslöser, Escape als Schließen
des zuletzt geöffneten Bereichs sowie fehlenden Horizontalüberlauf und
Seiten-Scrollbarkeit mit langem Testinhalt. Ein visueller Smoke nimmt je
Viewport erst nach den Verhaltensassertionen einen maskierten Light-Theme-
Screenshot der Shell auf und vergleicht ihn mit der versionierten Baseline
unter `tests/KnowHowToAI.BrowserTests/TestSupport/Baselines/`; Abweichungen
erfordern eine manuelle Diff-Prüfung, eine automatische Baselineaktualisierung
im regulären Lauf findet nicht statt. Da sein Host ausschließlich den minimalen
Read-only-Bestand nutzt, können Historien-, Release- und Transaction-Workflows
die Pixelbaseline nicht verändern.

Die Testablagen sind nach Prüfgegenstand benannt: Komponententests liegen in
`KnowHowToAI.Web.Tests` unter `Components/{Layout,Shared}` (Layout- und
Shared-Komponenten), `Features/<Feature>` (echte Feature-Seiten wie Dashboard)
und `TestSupport/` (Showcase- und Token-Fixture-Tests: `UiBasisShowcase`,
`DesignTokens`); vollständige Benutzerabläufe entstehen in
`KnowHowToAI.BrowserTests` unter `ReadOnly/` (Shell, Navigation, Reconnect)
und den featurebezogenen Ordnern `Transactions/`, `Content/`, `PdfExport/`
und `Assets/`, sobald die zuständigen Milestones sie befüllen.

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
