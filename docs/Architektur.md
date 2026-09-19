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
| `Application.Dashboard` | transportneutrale Dashboardaggregation (Current Snapshot, Release, offene Transactions, Qualität, geänderte Nodes) |

**`KnowHowToAI.Storage.SqlServer`**: `Connections` (Connection Factory),
`Configuration` (validierte SQL-/Migrations-Policies),
`Migrations` (Katalog, Checksums, Journal, Locking, Runner),
`Repositories.Transactions` (Snapshot-Kopie, Transaction-Zustandswechsel),
`Repositories.Knowledge` (versionierte Nodes, Rollen, Content, Dependencies),
`Repositories.Snapshots` (Current-/Working-/historische Snapshot-Reads),
`Repositories.History` (historische Snapshot-/Diff-Reads),
`Repositories.Releases` (unveränderliche Releases),
`Repositories.Retrieval` (Navigation, Search, Paging, Diff-Abfragen, Dashboard-Reads via `SqlDashboardRepository`),
`Mapping` (interne Dapper-Zeilenmodelle und explizites Domain-Mapping).

**`KnowHowToAI.Server`**: `Configuration` (bindbare Options, zentrale
Validatoren, Redaction), `Hosting` (Composition Root, DI, Kestrel-Start,
kontrollierter Shutdown), `Mcp.Contracts.*` (Request-/Response-DTOs je Toolgruppe),
`Mcp.Tools.*` (dünne Handler), `Mcp.Mapping` (ausschließlich
Transport-/Result-Mapping), `Web.Components` (Shell, Router, Layout,
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
Rolle, Lese-Kontext und `ChangeVersion` sowie `WebReadContextResolver`
zur Validierung und Auflösung von `transactionId`, `snapshotId` und
`releaseId` auf Core-`ReadContext` und `KnowledgeContextViewModel`) und
die Feature-Namespaces unter `Web.Features.*` (`Knowledge`, `Roles`,
`Search`, `History`, `Dashboard`).

Die Web-Lesegrenze entkoppelt Razor-Komponenten vollständig von Domain-Typen:
Komponenten rufen Application Services direkt in-process per Dependency
Injection auf (keine REST-Schicht). Die Ergebnisse werden über statische
Mapper (`KnowledgeNavigationMapper`, `RoleMapper`, `SearchMapper`,
`HistoryMapper`) in unveränderliche UI-ViewModels überführt. Fehlercodes,
Warnungen, opake Cursors und `ChangeVersion` bleiben dabei vollständig
erhalten; Domain-Typen erscheinen nicht im Rendering.

`Web.Features.History` stellt unter `/history` getrennte paginierte Listen für
committed Snapshots und Releases bereit. Die Seite ruft `HistoryService` und
`ReleaseService` direkt in-process auf, verwendet ausschließlich History-
ViewModels und übergibt bei einer Auswahl den jeweiligen `snapshotId`- oder
`releaseId`-Queryparameter an den bestehenden Web-Read-Context-Resolver. Working
Transactions erscheinen dort bewusst nicht.

Der native Wissensbaum (`Web.Features.Knowledge`) nutzt den flüchtigen Circuit-State
`KnowledgeTreeState` als Lazy-Loading-Datenadapter. Er lädt den Root-Knoten über
`NavigationService.GetRootAsync` und Kindknoten ausschließlich bei Expand über
`NavigationService.ListChildrenAsync` (mit `Limit = 100` und unverändert
weitergereichten opaken Cursors). Höchstens zehn 100er-Seiten liegen gleichzeitig im
Circuit-Cache (`KnowledgeTreePageCache`). Bei der elften Seite greift eine LRU-Eviction:
Ein unselektierter Teilbaum wird geschlossen; liegen alle zehn Seiten auf dem Auswahlpfad,
wird die rootnächste Seite entfernt und ihr Kind auf dem Auswahlpfad zum `VisualRoot`
des Tree-Ausschnitts (der globale Pfad bleibt in den Breadcrumbs). Paging („Zurück“ /
„Weitere“) ersetzt die sichtbare 100er-Seite vollständig über eine rein opaque
Cursor-Historie, ohne Seiten zu einer wachsenden Liste zusammenzufügen.

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
aktualisiert die Route `/knowledge/{NodeId}` unter Erhalt bestehender Query-Parameter;
beim Neuladen oder direktem Einstieg lädt der `KnowledgeTreePathLoader` den Pfad bis zum
Zielknoten nach, sodass Zustand und Breadcrumbs aus der URL vollständig rekonstruierbar
bleiben.

Die Read-only Node-Detailansicht (`NodeDetails`) zeigt Titel, Beschreibung, Position,
Rolle (inklusive Fallback-Kennzeichnung mit Pfeil und aufgelöster Rolle), Verfügbarkeit,
Freshness-Status, Inhaltsmodus (`Independent` vs. `Derived`), optionale Revisions-ID
sowie bei abgeleitetem Inhalt (`Derived`) die Quellrevisionen (`SourceRevisions`).
Der Inhaltsbereich rendert Markdown sicher über `SafeMarkdownRenderer` (gemäß O-020:
kein Raw-HTML-Rendering via `DisableHtml`, Neutralisierung von JavaScript-, Data- und
File-Links sowie externen Bild-URLs zur Vermeidung von Netzwerk-Requests; keine
Bearbeitungscontrols). Bei fehlendem Inhalt wird ein expliziter Hinweis angezeigt;
Lade- und Fehlerzustände nutzen `LoadingState`, `InlineAlert` bzw. `NotFoundState`.

Die routable Seite `SearchPage` (`/search`) verwendet mit der globalen Rolle und dem
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

`KnowledgeFilter` hält die Auswahl von aufgelöster Rolle, Availability,
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
  `Hauptnavigation` (genau der vorhandene Start-Link auf `/`; noch nicht
  implementierte Routen erscheinen bewusst nicht), `nav` `Breadcrumbs`,
  der Seitenaktionsbereich und optional `aside` `Kontext`.
- Fachseiten hängen Breadcrumbs, Aktionen und Kontext ohne eigenes
  Seitenraster über den scoped Slot `PageRegionState` ein; leere Bereiche
  belegen keinen Platz und erhalten keine Dummytexte.
- Fachseiten liefern über denselben Slot den Vertrag
  `KnowledgeContextViewModel` (immutable `record` unter
  `Web/Components/Layout/Context`) für die globale Wissenskontextleiste: Art des
  Lese-Kontexts (`Current`, `Snapshot`, `Transaction`, `Release`), optionale
  ID/Bezeichnung, optionale Rolle und `IsDirty`. Die Komponente
  `KnowledgeContextBar` rendert daraus genau eine globale Kontextleiste im
  Kopfbereich nahe der Wortmarke – als Text und Status ohne Selektor, Links
  oder Mutation; sie spiegelt `IsDirty` als `data-ktai-dirty`-Attribut ihres
  Wurzelelements; eine fehlende Rolle erscheint neutral als „Keine Rolle
  ausgewählt“, der Dirty-Zustand nur bei Bedarf als „Ungespeicherte
  Änderungen“ mit Icon plus Text. Nicht gelieferte Angaben erscheinen nicht;
  die Dashboard-Seite mappt den tatsächlichen Seitenkontext Current ohne
  Rolle und ohne `IsDirty`. Domain-Typen und der M3-`WorkspaceState` sind
  bewusst nicht Teil dieses Vertrags.
- `ContextSelectorDialog` hostet ausschließlich den nativen Dialog-Lifecycle.
  Das featurekonkrete `ContextSelectionForm` hält den unpersistierten
  Auswahlentwurf, validiert und bildet die kanonische Ziel-URL. Die beiden
  schmalen UI-Grenzen `IContextSelectionCatalog` und
  `IContextSelectionRoleCatalog` übersetzen Application-Ergebnisse in
  darstellbare Auswahlwerte; dadurch kennt weder Dialoghost noch Formular
  Release-, Dashboard- oder `NavigationService` direkt.
- Ab 1280 CSS-Pixeln (vom schmalen Modul `MainLayout.razor.js` über
  `matchMedia` gemeldet) stehen Navigation, Arbeitsfläche und optionaler
  Kontextbereich nebeneinander; die Arbeitsfläche nutzt
  `minmax(0, 1fr)`, um nicht unter `min-width`-Defaults zu überlaufen.
- In kompakten Breiten klappen klar beschriftete Kopfbuttons die
  Seitenbereiche ein und aus; sie erscheinen als überlagernde Panels
  unterhalb des Kopfs. Öffnen setzt den Fokus auf die
  Bereichsüberschrift, Schließen (Schalter, Schließen-Button, Escape)
  gibt ihn an den Auslöser zurück, und Escape schließt nur den zuletzt
  geöffneten überlagernden Bereich. Keine fixierten Höhen für normalen
  Inhalt; die Seite und die Arbeitsfläche scrollen im Dokument. Unterhalb
  von 1024 besteht nur die Zoom-/Reflow-Anforderung, keine
  Smartphone-Navigation.
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
  nicht dirty; es existiert kein `window`-Flag mehr; M2 besitzt dafür noch
  keinen Produzenten, eine persistierte Transaction gilt nie als ungespeichert.
`wwwroot/css/app.css` enthält den neutralen Reset, die zentralen
Design-Tokens des Business-Themes als CSS Custom Properties (Farben mit
Primary `#2563EB`, Text `#111827`, Page `#F8FAFC`, Surface `#FFFFFF` sowie
Erfolgs-, Warnungs- und Fehlerpaaren; Abstandsskala `4/8/12/16/24/32px`;
Radien `4/8px`; Surface-Schatten; Basistext `16px` mit Zeilenhöhe `1.5` und
der Systemschriftkette `Segoe UI, Arial, sans-serif`; Fokusring mit `3px`
Stärke und `2px` Abstand) und frameworkweite Basisklassen für Fokusring und
deaktivierten Zustand. Komponenten-CSS ist scoped und referenziert diese
Tokens ausschließlich über `var(...)`; es gibt kein zweites Theme und kein
Dark Theme. Die Shell bindet ausschließlich lokale eigene Ressourcen ein,
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
Die Suite arbeitet dabei gegen die von der manuellen SQL-Integrationssuite
getrennte Browser-Testdatenbank gemäß
[Konfiguration und Betrieb](Konfiguration-und-Betrieb.md#konfigurationstrennung).
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
im regulären Lauf findet nicht statt.

Die Testablagen sind nach Prüfgegenstand benannt: Komponententests liegen in
`KnowHowToAI.Web.Tests` unter `Components/{Layout,Shared}` (Layout- und
Shared-Komponenten), `Features/<Feature>` (echte Feature-Seiten wie Dashboard)
und `TestSupport/` (Showcase- und Token-Fixture-Tests: `UiBasisShowcase`,
`DesignTokens`); vollständige Benutzerabläufe entstehen in
`KnowHowToAI.BrowserTests` unter `ReadOnly/` (Shell, Navigation, Reconnect)
und den featurebezogenen Ordnern `Transactions/`, `Content/`, `PdfExport/`
und `Assets/`, sobald die zuständigen Milestones sie befüllen.

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
