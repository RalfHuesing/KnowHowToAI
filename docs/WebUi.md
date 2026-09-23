# Web-UI-Gesamtbild (Ist-Zustand)

## Zweck und Grenze

Die Weboberfläche stellt den Wissensarbeitsplatz und den Entwurfsablauf bereit.
Core-Anwendungsfälle, SQL-Persistenz sowie MCP-Tools für Suche, Historie,
Zielgruppenverwaltung und Markdown-Export bleiben transportneutral erhalten.
Die Weboberfläche besitzt keine Legacy-Seiten oder Weiterleitungen für diese
Use-Cases.

## Shell und Ownership

`App.razor` stellt die interaktive Server-Circuit-Grenze bereit. `MainLayout`
besitzt Header, Navigation, aktiven Entwurfslink, `main#shell-main`,
Dirty-Schutz, Toasts und Reconnect-Zustand. `PageFrame` stellt den gemeinsamen
Seitenrahmen bereit. Die Wissensseite besitzt Tree, Knotenauswahl, Dokument,
Zielgruppenauswahl und deren Query-Kontext. `DraftPage` besitzt Review,
Validierung, Diff, Übernahme, Verwerfen und den lesenden Abschlusszustand.
Gemeinsam verwendete Draft-Komponenten liegen unter
[`Web/Features/Drafts`](../src/KnowHowToAI.Server/Web/Features/Drafts).

## Produktive UI-Routen

| Route | Verhalten | Nachweis |
|---|---|---|
| `/` | Ersetzt die URL durch `/knowledge`. | [`KnowledgeRedirect.razor`](../src/KnowHowToAI.Server/Web/Components/Navigation/KnowledgeRedirect.razor), [`KnowledgeRedirectSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/ReadOnly/KnowledgeRedirectSmokeTests.cs) |
| `/knowledge` | Liest den Current Snapshot oder bei `transactionId` den offenen Entwurf. `audienceId` wählt die Perspektive; fehlt eine gültige Auswahl bei vorhandenen Zielgruppen, fordert ein Dialog zur Auswahl auf. | [`KnowledgePage.razor`](../src/KnowHowToAI.Server/Web/Features/Knowledge/KnowledgePage.razor), [`KnowledgeTreeSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/ReadOnly/KnowledgeTreeSmokeTests.cs), [`RootNodeInitializationSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/Transactions/RootNodeInitializationSmokeTests.cs) |
| `/knowledge/{NodeId:guid}` | Wie `/knowledge`, zusätzlich direkte Knotenauswahl und Wiederherstellung des Pfades nach Direktaufruf und Reload. | [`KnowledgeTreeSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/ReadOnly/KnowledgeTreeSmokeTests.cs), [`KnowledgeDirectEditingSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/Transactions/KnowledgeDirectEditingSmokeTests.cs), [`KnowledgeTreeMoveSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/Transactions/KnowledgeTreeMoveSmokeTests.cs) |
| `/drafts` | Listet offene Entwürfe und ermöglicht den Einstieg in den Arbeitskontext. | [`DraftPage.razor`](../src/KnowHowToAI.Server/Web/Features/Drafts/DraftPage.razor), [`DraftWorkflowSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/Drafts/DraftWorkflowSmokeTests.cs) |
| `/drafts/{TransactionId:guid}` | Prüft einen Entwurf; offene Entwürfe bieten Diff, Validierung, Commit und Discard. Abgeschlossene Entwürfe sind schreibgeschützt. | [`DraftWorkflowSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/Drafts/DraftWorkflowSmokeTests.cs), [`SnapshotConflictSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/Transactions/SnapshotConflictSmokeTests.cs), [`DraftLifecycleTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/Drafts/DraftLifecycleTests.cs) |

Alte URLs wie `/dashboard`, `/search`, `/history`, `/audiences` und
`/transactions[/...]` werden nicht mehr als UI-Seiten angeboten und liefern
NotFound. Es gibt keine Redirects. Hosttests prüfen diese Grenze sowie MCP und
statische Assets in [`WebHostTests.cs`](../tests/KnowHowToAI.IntegrationTests/Server/Hosting/WebHostTests.cs).

## Arbeitsabläufe

Der erste persistente Web-Write beginnt über `WebWriteCoordinator` einen
Entwurf und trägt dessen `transactionId` in die Wissens-URL ein. Weitere
Mutationen verwenden dieselbe offene Transaction und deren `ChangeVersion`.
Nach Commit oder Discard wechselt der Arbeitsplatz zurück zum Current Snapshot.
Ungespeicherte Editor-Eingaben werden durch `NavigationProtection` gesichert;
ein gespeicherter, noch offener Entwurf ist für sich genommen kein Dirty-State.

Bei einer konkurrierenden Änderung zeigt der Draft-Review Base-/Current-Stand
und strukturierten Diff. Ein manuelles Reapply beginnt einen neuen Entwurf; es
kopiert keine Änderungen automatisch.

Die Wissensseite zeigt bei ausgewähltem Knoten zuerst die native Reiterleiste
„Lesen“, „Bearbeiten“, „Titel“ und „Technische Details“. Der Lese- oder
Arbeitskontext steht rechts neben den Reitern. Eine neue Node- oder
Zielgruppenauswahl beginnt bei „Lesen“; das einzige Seiten-`h1` gehört dem
gemeinsamen `PageFrame` und bleibt in allen Ansichten dasselbe. Im Lesepanel
stehen gerenderter Inhalt und Fallback-/Derived-Hinweise vor der getrennten
Zielgruppenangabe. Metadaten und Content besitzen getrennte Formulare und
Speichern-Aktionen. Technische Details enthalten Verfügbarkeit, Aktualität,
Inhaltsmodus, Position, Revision und – bei Derived Content – Herkunft; die
Angaben haben keine zusätzliche aufklappbare Überschrift.
Im Titelpanel stehen „Abbrechen“ und „Speichern“ in der unteren rechten
Aktionsgruppe, mit „Speichern“ am rechten Rand. Im Nur-Lese-Kontext zeigt das
Panel Titel und Beschreibung ohne Mutationsaktionen.

Metadatenformular und Inhaltseditor werden erst beim ersten Öffnen ihrer
Ansicht montiert. Danach bleiben sie für dieselbe Node und Zielgruppe beim
Ansichtswechsel montiert und verborgen; dadurch bleiben ungespeicherte Werte,
Validierungsfehler und Warnungen erhalten. Der Editor erhält beim ersten
Öffnen und beim Wiedereintritt den Fokus. Im Editor öffnet Fallback oder
fehlender eigener Content eine leere, ausdrücklich zu speichernde Fassung;
Derived Content bleibt dort schreibgeschützt. Metadaten sind in einem
schreibbaren Current- oder Draft-Kontext weiterhin unabhängig davon änderbar.
Beim Wechsel von Node oder Zielgruppe greift der vorhandene Dirty-Schutz.
Der `ContentEditor` hält Eingabe, Dirty- und Rückmeldungszustand; der explizite
Speichervorgang übergibt Markdown und gelesene Versionsstände an
`IContentWriteWorkflow`. `ContentWriteWorkflow` erstellt daraus die
`ReplaceContentRequest` und führt die Mutation über den gemeinsamen
Web-Write-Coordinator aus.

`WorkspaceState` hält nur den Circuit-Zustand für ausgewählten Node,
Zielgruppe, Current-/Draft-Kontext, geladenen Snapshot und `ChangeVersion`.
`KnowledgePageContextResolver` löst ausschließlich Current oder eine offene
`transactionId` für die Wissensroute auf. Snapshot-/Release-Auswahl gehört nicht
zur Weboberfläche; die gleichnamigen historischen MCP/Core-Funktionen bleiben
bestehen.

Der Wissensbaum stellt Hierarchien als verschachtelte Listen dar. Eine native
Schaltfläche wählt jeden Knoten aus; separate Schaltflächen klappen Zweige auf,
legen Unterknoten an und blättern durch Seiten mit bis zu 100 Einträgen. Im
schreibbaren Kontext wird ausschließlich per Maus-Drag-and-drop vor, nach oder
unter einen Zielknoten verschoben. Der Drop-Indikator erscheint ab einer
5-Pixel-Bewegung; Auswahl- und Aktionsschaltflächen bleiben unterscheidbar.
Serverseitige Validierung und Bestätigung bestimmen die sichtbare Reihenfolge;
ein ungültiger oder abgelehnter Drop sortiert den Baum nicht lokal um. Der Baum
verspricht kein besonderes Tastaturmuster und verwendet deshalb keine
Treeview-Rollen oder Roving-Tabindex-Steuerung. Native Schaltflächen behalten
ihre Browser-Fokus- und Aktivierungsfunktionen. Nach einem bestätigten Move
bleibt die `transactionId` beim Routenwechsel erhalten, damit der aktualisierte
Baum weiter aus demselben Entwurf gelesen wird.

Die flüchtige Aufklappabsicht ist von den geladenen Kindseiten getrennt. Der
LRU-Cache hält höchstens zehn Elternseiten; bei Verdrängung bleiben Zweige
geöffnet und zeigen „Unterknoten laden“. Erst diese Aktion lädt genau den
betroffenen Zweig erneut. Explizites Zuklappen entfernt seine Aufklappabsicht.
Ein echter Kontextwechsel, Zielgruppenwechsel oder Reload startet frisch;
Current-zu-Draft, Metadaten-Refresh und Move-Recovery behalten sie bei. Nach
Refresh werden offene Seiten nicht gesammelt vorgeladen.

## Export- und Featuregrenze

Die Weboberfläche besitzt keinen Markdown-Download-Endpunkt. Markdown-Export
bleibt über den MCP-Exportvertrag verfügbar. Die Leseansicht des Knotendokuments
zeigt gerenderten Inhalt und dessen Fallback-/Derived-Einordnung; Freshness und
Revision liegen unter „Technische Details“. Änderungen erfolgen ausdrücklich
im Draft-Kontext. Node-Kennungen erscheinen nicht als lesbarer Dokumenttext;
die bestehende Node-URL und interne Kennungen für Routing und Zuordnung bleiben
erhalten. Bei Derived Content bezeichnet die Herkunft den Quellknoten mit seinem
Titel aus demselben ReadContext. Ist dieser dort nicht verfügbar oder schlägt die
optionale Titelabfrage fehl, zeigt die Oberfläche „Quellknoten nicht verfügbar“;
das Hauptdokument bleibt lesbar. Die Zielgruppe bleibt in der Dokumentansicht
sichtbar.

## Verbindliche UI-Nachweise

Die Routengrenze und MCP-/Asset-Erreichbarkeit stehen in den Hosttests. Die
verbleibenden Web-Komponenten werden durch FastTests und Browser-Smokes geprüft.
`LayoutShellSmokeTests` und `PageFrameSmokeTests` belegen die gemeinsamen
Landmarken, Seiten-Innenkanten und Layoutzustände bei den dokumentierten
Desktopbreiten 1280 × 720 und 1024 × 720. `LayoutShellSmokeTests` prüft die
Navigation per Mausklick und langen Seiteninhalt per Mausrad. Die manuelle
Abnahme prüft die relevanten Desktopansichten mit Maus; sie ersetzt keine
automatisierten Verhaltensassertions. HTML-Semantik, Darstellung bei den
unterstützten Desktopbreiten und Layoutgrenzen folgen den
[Web-UI-Guardrails](../.agents/rules/WebUiHtmlCss.mdc) und der
[manuellen UI-Abnahme](Manuelle-UI-Abnahme.md).
