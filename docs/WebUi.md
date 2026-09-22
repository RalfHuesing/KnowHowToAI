# Web-UI-Gesamtbild (Ist-Zustand)

## 1. Lesezweck und Grenzen

Diese Seite ist die kompakte Landkarte der implementierten Blazor-Oberfläche.
Sie beantwortet vor seitenübergreifenden Änderungen: Wo liegt der Nutzerkontext,
welche Seiten gibt es, welche Übergänge sind fachlich relevant und wer besitzt
welchen UI-Bereich? Das maßgebliche Istbild kommt aus den verlinkten Razor-
Komponenten und Tests; Markup-, CSS-, ViewModel- und Einzelfall-Details bleiben
dort. Diese Seite beschreibt kein Redesign und keinen vollständigen Button- oder
Komponentenkatalog.

## 2. Globales UI-Modell

```text
App.razor
├─ Routes → Router → MainLayout
│  ├─ Skip-Link, Header/Wortmarke, Navigationstoggle
│  ├─ KnowledgeContextBar (fachlicher Lese-/Arbeitskontext)
│  ├─ PrimaryNavigation
│  ├─ optional: ContextPanel
│  └─ Workspace
│     ├─ optional: BreadcrumbRegion + PageActions
│     ├─ main#shell-main (eine Routable Page wird darin gerendert)
│     └─ ToastRegion (global, nichtkritische Abschlussmeldungen)
│  + ContextSelectorDialog + NavigationProtection
└─ ReconnectModal (Framework-Circuitzustand)
```

| Bereich | Ownership und Grenze |
|---|---|
| Shell | [`MainLayout.razor`](../src/KnowHowToAI.Server/Web/Components/Layout/Shell/MainLayout.razor) besitzt Landmarks, Header, Workspace, globalen Toast-Host, Kontextauswahl und Navigation Protection. |
| Hauptnavigation | [`PrimaryNavigation.razor`](../src/KnowHowToAI.Server/Web/Components/Layout/PageRegions/PrimaryNavigation.razor) zeigt Start, Suche, Transactions und Zielgruppen. Historie ist routbar, aber kein Hauptnavigationsziel. |
| Wissenskontext | [`KnowledgeContextBar.razor`](../src/KnowHowToAI.Server/Web/Components/Layout/Context/KnowledgeContextBar.razor) zeigt `Current`, `Snapshot`, `Transaction` oder `Release`, optionale Bezeichnung/Zielgruppe, Working-Metadaten und bei Bedarf Dirty. Sie mutiert den Kontext nicht. |
| Seitenregionen | [`PageRegionState.cs`](../src/KnowHowToAI.Server/Web/Components/Layout/PageRegions/PageRegionState.cs) nimmt Breadcrumbs, Aktionen, optionalen Kontextbereich und den Kontextvertrag einer Fachseite auf. Leere Slots belegen keinen Platz. |
| Arbeitsfläche | [`MainLayout.razor`](../src/KnowHowToAI.Server/Web/Components/Layout/Shell/MainLayout.razor) besitzt genau ein `main#shell-main`; Routable Pages rendern darin ihren Page-Root und ihre Feature-Sections. |
| Gemeinsame Seitenbasis | [`PageFrame.razor`](../src/KnowHowToAI.Server/Web/Components/Shared/PageFrame.razor) rendert für jede routable Page genau einen `section.page-frame--shared` mit `header`, `h1` und Inhaltsbereich. `Title` ist erforderlich; `Description`, `Badges`, `Actions`, `ChildContent`, `Class` und zusätzliche Attribute sind optionale Parameter. |
| Feedback/Dialoge | Kritische oder fachliche Zustände bleiben an der auslösenden Seite (`InlineAlert`, `StatusBanner`, Lade-/Leer-/Fehlerzustände). Bestätigungen nutzen den gemeinsamen Dialog; nichtkritische abgeschlossene Aktionen nutzen die einzige `ToastRegion`. |

### Gemeinsamer Seitenbasis-/Headervertrag

Alle zehn routbaren Page-Varianten verwenden [`PageFrame`](../src/KnowHowToAI.Server/Web/Components/Shared/PageFrame.razor)
als äußeren Page-Root: `/`, `/knowledge`, `/knowledge/{NodeId:guid}`, `/search`,
`/transactions`, `/transactions/{TransactionId:guid}`, `/drafts`,
`/drafts/{TransactionId:guid}`, `/audiences` und `/history`. Auch Lade-, Leer-,
Fehler-, Auswahl- und Working-Zustände bleiben
innerhalb dieses Roots. Die `Title`-Parameter werden dadurch immer als genau
ein semantisches `h1` ausgegeben. Auf beiden Knowledge-Routen bleibt
`Wissensbasis` das stabile `h1`; der ausgewählte Node wird im Detailbereich als
`h2` dargestellt. Die Transaction-Detailseite setzt ihren konkreten Zweck
dynamisch über `TransactionPageTitle`.

Die Shell besitzt weiterhin den verfügbaren Arbeitsraum und ihren Inset.
`PageFrame` nutzt diesen Raum mit `width: 100%`, `min-width: 0` und ohne eigenes
Shell-Padding, Zentrierung oder seitenweites `max-width`. Die einzige zulässige
gemeinsame Prosa-Begrenzung ist die Beschreibung im Header (`70ch`). Die
komponentennahe Header-/Abstands-/Umbruchgestaltung gehört ausschließlich
[`PageFrame.razor.css`](../src/KnowHowToAI.Server/Web/Components/Shared/PageFrame.razor.css);
das zentrale [`layout.css`](../src/KnowHowToAI.Server/wwwroot/css/base/layout.css)
liefert nur das neutrale `.page-frame`-Primitive sowie `.readable` und
`.action-group`. Feature-scoped CSS besitzt ausschließlich fachliche
Innenlayouts und überschreibt den gemeinsamen Root-/Header-/`h1`-Vertrag nicht.

Die strukturellen Grenzen sind bewusst eng: `main#shell-main` bleibt das einzige
`main`-Landmark, der Page-Root erzeugt kein weiteres `main`, und der Inhalt
bleibt im normalen Dokument scrollbar. Computed Styles, Innenkanten,
Heading-Anzahl und horizontaler Overflow werden routeübergreifend im
[`PageFrameSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/ReadOnly/PageFrameSmokeTests.cs)
geprüft; der isolierte Parameter-/Semantikvertrag steht in
[`PageFrameTests.cs`](../tests/KnowHowToAI.Web.Tests/Components/Shared/PageFrameTests.cs).

Responsive Anordnung, Semantik, Fokus, Reflow und Layout-Ownership sind in den
[Web-UI-Guardrails](../.agents/rules/WebUiHtmlCss.mdc) und der
[manuellen UI-Abnahme](Manuelle-UI-Abnahme.md) geregelt; die Shell-Belegung wird
nicht in diesen Regeln dupliziert. Die technische Schichtung und die
renderer-/Circuit-Grenzen stehen in [Architektur](Architektur.md).

## 3. Seitennetz und Kernabläufe

```text
Start/Dashboard ── Wissensbaum ── Node-Details ── Historie/Export
       │                 └─ Working-Transaction: Nodes/Content/Zielgruppen
       │                                              └─ Validieren → Commit/Verwerfen
       ├─ Suche ── Treffer ── Wissensbaum (Kontext-Query bleibt erhalten)
       ├─ Transactions ── Transaction-Arbeitsbereich ── Wissensbaum/Zielgruppen
       ├─ Entwürfe ── Entwurf prüfen ── übernehmen/verwerfen
       └─ Historie ── Snapshot/Diff, Release-Metadaten ── Wissensbaum als Read-Kontext
```

- Ein Read-Kontext ist entweder Current oder genau einer der URL-Selektoren
  `transactionId`, `snapshotId`, `releaseId`. [`WebReadContextResolver`](../src/KnowHowToAI.Server/Web/State/WebReadContextResolver.cs)
  validiert und rekonstruiert ihn; die Seiten geben ihn über die globale
  Kontextleiste sichtbar weiter.
- Knowledge und Search wählen eine Zielgruppe explizit über `audienceId` oder
  den gespeicherten letzten Wert. Fehlt sie trotz vorhandener Zielgruppen,
  öffnet der Kontextselektor; bei keinen Zielgruppen bleibt ein erklärter
  Leerzustand.
- Mutationen laufen im offenen Working-Kontext. Ein erfolgreicher Save hält den
  Kontext und `ChangeVersion` aktuell; Commit/Verwerfen führt zurück zum Current-
  Kontext. Ungespeicherte Formularänderungen werden über
  [`NavigationProtection.razor`](../src/KnowHowToAI.Server/Web/Components/Layout/Shell/NavigationProtection.razor)
  geschützt.
- Suchtreffer und Dashboard-/Historienlinks öffnen den kanonischen Knowledge-
  Einstieg und erhalten den relevanten Kontext. Snapshot-/Diff-Ansichten der
  Historie bleiben read-only; der Release-Bereich kann Release-Metadaten für
  committed Snapshots anlegen. Einen Merge- oder Reapply-Ablauf bietet die
  Historie nicht.

## 4. Routenindex

Jede implementierte `@page`-Direktive erscheint hier genau einmal. Query-
Selektoren sind nur dort aufgeführt, wo die routbare Page sie tatsächlich liest.

| Route | Kontext/Parameter und Hauptaufgabe | Routable Page / repräsentativer Test |
|---|---|---|
| `/` | Current; Dashboard für Current Snapshot/Release, offene Transactions, Qualität und letzte Node-Änderungen. | [`DashboardPage.razor`](../src/KnowHowToAI.Server/Web/Features/Dashboard/DashboardPage.razor) · [`DashboardPageTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/Dashboard/DashboardPageTests.cs) |
| `/knowledge` | Optional genau einer von `transactionId`, `snapshotId`, `releaseId` plus `audienceId`; Baum ohne feste Node-Auswahl, Details/Editor je Zustand. | [`KnowledgePage.razor`](../src/KnowHowToAI.Server/Web/Features/Knowledge/KnowledgePage.razor) · [`KnowledgePageContextSelectorTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/Knowledge/KnowledgePageContextSelectorTests.cs) |
| `/knowledge/{NodeId:guid}` | Wie `/knowledge`, zusätzlich GUID-Auswahl des Knotens; Baum, Breadcrumbs und Knotendetails fokussieren diesen Node. | [`KnowledgePage.razor`](../src/KnowHowToAI.Server/Web/Features/Knowledge/KnowledgePage.razor) · [`KnowledgeTreeSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/ReadOnly/KnowledgeTreeSmokeTests.cs) |
| `/search` | Optional genau einer von `transactionId`, `snapshotId`, `releaseId` plus `audienceId`; Suche, Filter, paginierte Treffer und Übergang in Knowledge. | [`SearchPage.razor`](../src/KnowHowToAI.Server/Web/Features/Search/SearchPage.razor) · [`SearchSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/ReadOnly/SearchSmokeTests.cs) |
| `/transactions` | Kein Page-Query-Kontext; Working Transaction beginnen oder offene Transactions aufnehmen und in Details/Arbeitskontext weitergehen. | [`TransactionsPage.razor`](../src/KnowHowToAI.Server/Web/Features/Transactions/TransactionsPage.razor) · [`TransactionsPageTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/Transactions/TransactionsPageTests.cs) |
| `/transactions/{TransactionId:guid}` | GUID identifiziert die Working Transaction; weiter in Knowledge/Zielgruppen, validieren, committen oder verwerfen. | [`TransactionPage.razor`](../src/KnowHowToAI.Server/Web/Features/Transactions/TransactionPage.razor) · [`TransactionPageTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/Transactions/TransactionPageTests.cs) |
| `/drafts` | Übersicht aller offenen Entwürfe ohne Besitzbehauptung; bewusste Auswahl zum Fortsetzen oder Prüfen. | [`DraftsPage.razor`](../src/KnowHowToAI.Server/Web/Features/Drafts/DraftsPage.razor) · [`DraftPagesTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/Drafts/DraftPagesTests.cs) |
| `/drafts/{TransactionId:guid}` | Direktaufruf eines Entwurfs mit Netto-Diff, Validierung und Abschlussaktionen. Konflikte erhalten den Entwurf und zeigen Snapshotvergleich und sicheren nächsten Schritt. Abgeschlossene Direktaufrufe zeigen Current. | [`DraftPage.razor`](../src/KnowHowToAI.Server/Web/Features/Drafts/DraftPage.razor) · [`DraftWorkflowSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/Drafts/DraftWorkflowSmokeTests.cs) |
| `/audiences` | Optional genau einer von `transactionId`, `snapshotId`, `releaseId`; Zielgruppen anzeigen, im offenen Working-Kontext pflegen, sonst read-only. | [`AudiencesPage.razor`](../src/KnowHowToAI.Server/Web/Features/Audiences/AudiencesPage.razor) · [`AudiencesPageTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/Audiences/AudiencesPageTests.cs) |
| `/history` | `audienceId` sowie optional `baseSnapshotId`, `targetSnapshotId`, `nodeId`; Snapshots vergleichen, Release-Metadaten für einen committed Snapshot anlegen, Releases anzeigen und in einen historischen Knowledge-Kontext wechseln. | [`HistoryPage.razor`](../src/KnowHowToAI.Server/Web/Features/History/HistoryPage.razor) · [`HistoryPageTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/History/HistoryPageTests.cs) |

## 5. Seitensteckbriefe

### Dashboard

- Intention: schneller Einstieg in den Current-Wissensstand und seine offenen
  Arbeits-/Qualitätssignale.
- Hauptbereiche: Snapshot/Release, offene Transactions, Qualitätsübersicht,
  letzte Node-Änderungen; jeder Bereich kann seinen eigenen technischen Fehler
  anzeigen.
- Primäre Aktionen: Wissensbaum, Historie, Transaction-Details und betroffene
  Nodes öffnen; die Interaktivitätsprüfung ist ein Shell-Smoke-Anker.
- Zustände: initiales Laden, bereichsweise Fehler mit Retry, geladene Übersicht.
- Code/Test: [`DashboardPage.razor`](../src/KnowHowToAI.Server/Web/Features/Dashboard/DashboardPage.razor),
  [`DashboardSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/ReadOnly/DashboardSmokeTests.cs).

### Wissenscockpit (beide Knowledge-Routen)

- Intention: hierarchisches Lesen und – nur im offenen Working-Kontext –
  Bearbeiten eines Nodes.
- Hauptbereiche: Breadcrumbs, lazy geladener Wissensbaum, Knotendetails und
  Markdown-Teilbaumexport; leerer Working Tree bietet Root-Anlage.
- Primäre Aktionen: Node auswählen/navigieren, Kontext/Zielgruppe wählen,
  Node-/Content-Mutation ausführen oder eine Arbeitskopie beginnen.
- Zustände: fehlender/ungültiger Kontext, keine Zielgruppen, Pflichtauswahl,
  Laden/Fehler/NotFound, Current-/Snapshot-/Release-read-only und Working-
  Dirty-State. Die Node-Route setzt zusätzlich die initiale Auswahl.
- Code/Test: [`KnowledgePage.razor`](../src/KnowHowToAI.Server/Web/Features/Knowledge/KnowledgePage.razor),
  [`KnowledgePageTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/Knowledge/KnowledgePageTests.cs),
  [`MarkdownDownloadSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/ReadOnly/MarkdownDownloadSmokeTests.cs).

### Suche

- Intention: Titel, Beschreibungen und aufgelösten Zielgruppeninhalt im
  gewählten Read-Kontext finden.
- Hauptbereiche: Suchformular, Zielgruppen-/Qualitätsfilter und eine paginierte
  Trefferliste mit Breadcrumb.
- Primäre Aktionen: suchen, Filter anwenden, weitere Treffer laden und einen
  Treffer im Knowledge-Kontext öffnen.
- Zustände: Kontext-/Audience-Fehler, keine Zielgruppen, initial bereit,
  laufende Suche, leer/fehlerhaft und paginierte Ergebnisse; veraltete Requests
  überschreiben kein aktuelles Ergebnis.
- Code/Test: [`SearchPage.razor`](../src/KnowHowToAI.Server/Web/Features/Search/SearchPage.razor),
  [`SearchPageTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/Search/SearchPageTests.cs).

### Transactions-Übersicht

- Intention: Working Transactions beginnen und wiederaufnehmen.
- Hauptbereiche: optionales Startformular und Liste offener Transactions mit
  fachlichen Metadaten sowie progressiven technischen Details.
- Primäre Aktionen: Transaction beginnen, Arbeitskontext fortsetzen, Details
  öffnen.
- Zustände: Laden, leer, Startfehler, laufender Start und überalterte offene
  Transaction.
- Code/Test: [`TransactionsPage.razor`](../src/KnowHowToAI.Server/Web/Features/Transactions/TransactionsPage.razor),
  [`TransactionsPageTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/Transactions/TransactionsPageTests.cs).

### Transaction-Arbeitsbereich

- Intention: eine Working Transaction prüfen und abschließen.
- Hauptbereiche: Transaction-Kopf, Weiterarbeiten-Einstieg, Validierung,
  Netto-Diff, Commit-/Discard-Abschluss und bei Konflikt Snapshotvergleich.
- Primäre Aktionen: Knowledge/Zielgruppen öffnen, validieren, committen oder
  verwerfen; bei SnapshotConflict eine neue Transaction für manuelles Reapply
  beginnen.
- Zustände: Laden, nicht gefunden/Fehler, offene oder bereits abgeschlossene
  Transaction, Validierungs-/ChangeVersion-Fehler und SnapshotConflict.
- Code/Test: [`TransactionPage.razor`](../src/KnowHowToAI.Server/Web/Features/Transactions/TransactionPage.razor),
  [`TransactionStateAndNavigationSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/Transactions/TransactionStateAndNavigationSmokeTests.cs).

### Entwürfe

- Intention: offene Arbeitsstände nach Reload auffindbar und gezielt prüfbar machen.
- Hauptbereiche: offene Entwürfe mit Fortsetzen-/Prüfen-Aktionen; Detail mit
  Netto-Diff, serverseitiger Validierung, Übernahme/Verwerfen und
  Konfliktvergleich.
- Zustände: Laden, leere Übersicht, ungültige oder nicht vorhandene ID, offener
  und abgeschlossener Entwurf. Nur die ID des geöffneten Entwurfs wird an seine
  Abschlussaktion weitergegeben.
- Code/Test: [`DraftsPage.razor`](../src/KnowHowToAI.Server/Web/Features/Drafts/DraftsPage.razor), [`DraftPage.razor`](../src/KnowHowToAI.Server/Web/Features/Drafts/DraftPage.razor), [`DraftPagesTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/Drafts/DraftPagesTests.cs), [`DraftWorkflowSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/Drafts/DraftWorkflowSmokeTests.cs).

### Zielgruppen

- Intention: Zielgruppen des gewählten Wissenskontexts lesen und im Working-
  Kontext pflegen.
- Hauptbereiche: Kontextabhängiger `AudienceEditor` mit Liste, Formular- und
  Löschdialogen.
- Primäre Aktionen: anzeigen; in einer offenen Transaction erstellen,
  umbenennen und löschen.
- Zustände: Laden, ungültiger Kontext, read-only Current/Snapshot/Release oder
  geschlossene Transaction, sowie Dirty-/ChangeVersion-Fehler.
- Code/Test: [`AudiencesPage.razor`](../src/KnowHowToAI.Server/Web/Features/Audiences/AudiencesPage.razor),
  [`AudiencesPageTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/Audiences/AudiencesPageTests.cs).

### Historie und Releases

- Intention: unveränderliche Snapshots vergleichen sowie Release-Metadaten für
  committed Snapshots einsehen und anlegen.
- Hauptbereiche: Abgrenzung zu Working Transactions, Snapshot-Auswahl,
  cursor-paginierter Diff, Release-Panel und `CreateReleaseDialog`.
- Primäre Aktionen: Ausgang/Ziel wählen, optional Node filtern, Snapshot- oder
  Diff-Ansicht read-only öffnen, Release-Metadaten für einen committed Snapshot
  anlegen und Snapshot/Release im Knowledge-Read-Kontext öffnen.
- Zustände: ungültige Query-IDs, noch nicht gewählte Vergleichsstände, leere
  Historie, geladener/fehlerhafter Diff sowie Release-Anlage mit Validierungs-,
  Namenskonflikt- oder Qualitätsbefund-Ergebnis.
- Code/Test: [`HistoryPage.razor`](../src/KnowHowToAI.Server/Web/Features/History/HistoryPage.razor),
  [`ReleasePanel.razor`](../src/KnowHowToAI.Server/Web/Features/History/ReleasePanel.razor),
  [`CreateReleaseDialog.razor`](../src/KnowHowToAI.Server/Web/Features/History/CreateReleaseDialog.razor),
  [`HistoryPageTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/History/HistoryPageTests.cs).

## 6. Gemeinsame Verträge und Detailquellen

- **Read-/Arbeitskontext:** Auflösung und gegenseitiger Ausschluss der drei
  Read-Selektoren: [`WebReadContextResolver.cs`](../src/KnowHowToAI.Server/Web/State/WebReadContextResolver.cs).
  Der flüchtige Seitenzustand für Node, Zielgruppe, Kontext, `ChangeVersion` und
  Dirty ist [`WorkspaceState.cs`](../src/KnowHowToAI.Server/Web/State/WorkspaceState.cs).
- **Dirty und Navigation:** Editor-Komponenten ändern den zentralen Zustand;
  [`NavigationProtection.razor`](../src/KnowHowToAI.Server/Web/Components/Layout/Shell/NavigationProtection.razor)
  schützt interne und externe Navigation. Eine erfolgreich abgeschlossene
  Audience-Mutation im Working Snapshot setzt aktuell weiterhin
  `WorkspaceState.IsDirty`; dadurch greift die NavigationProtection auch nach
  dieser fachlichen Persistierung. Der Dirty-State wird beim Kontextwechsel
  zurückgesetzt und bezeichnet den noch nicht committeten Working-Kontext.
- **Feedback und Dialoge:** Zustandssemantik liegt bei
  [`Shared/Feedback`](../src/KnowHowToAI.Server/Web/Components/Shared/Feedback),
  blockierende Bereiche bei [`Shared/States`](../src/KnowHowToAI.Server/Web/Components/Shared/States)
  und Bestätigungen bei [`Shared/Dialogs`](../src/KnowHowToAI.Server/Web/Components/Shared/Dialogs).
- **Normative Querschnittsregeln:** Accessibility-/Layoutnachweise und
  Ownership stehen in den [Web-UI-Guardrails](../.agents/rules/WebUiHtmlCss.mdc),
  die Browsercheckliste in der [manuellen UI-Abnahme](Manuelle-UI-Abnahme.md).
  Fachliche Invarianten bleiben in [Invarianten](Invarianten.md); technische
  Schichtung, Application-Grenzen und Testgrenzen in [Architektur](Architektur.md).
