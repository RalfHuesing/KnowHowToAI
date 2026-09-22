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
│     ├─ main#shell-main (genau eine Routable Page)
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
| Arbeitsfläche | Jede Routable Page besitzt `main#shell-main`; ihre Page-Root- und Feature-Sections liegen in der jeweiligen `Web/Features/*`-Komponente. |
| Feedback/Dialoge | Kritische oder fachliche Zustände bleiben an der auslösenden Seite (`InlineAlert`, `StatusBanner`, Lade-/Leer-/Fehlerzustände). Bestätigungen nutzen den gemeinsamen Dialog; nichtkritische abgeschlossene Aktionen nutzen die einzige `ToastRegion`. |

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
       └─ Historie ── Snapshot/Release ── Wissensbaum als Read-Kontext
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
  Einstieg und erhalten den relevanten Kontext. Historie bleibt read-only; sie
  bietet keinen Merge- oder Reapply-Ablauf.

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
| `/audiences` | Optional genau einer von `transactionId`, `snapshotId`, `releaseId`; Zielgruppen anzeigen, im offenen Working-Kontext pflegen, sonst read-only. | [`AudiencesPage.razor`](../src/KnowHowToAI.Server/Web/Features/Audiences/AudiencesPage.razor) · [`AudiencesPageTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/Audiences/AudiencesPageTests.cs) |
| `/history` | `audienceId` sowie optional `baseSnapshotId`, `targetSnapshotId`, `nodeId`; Snapshots vergleichen, Releases anzeigen und in einen historischen Knowledge-Kontext wechseln. | [`HistoryPage.razor`](../src/KnowHowToAI.Server/Web/Features/History/HistoryPage.razor) · [`HistorySmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/ReadOnly/HistorySmokeTests.cs) |

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

- Intention: unveränderliche Snapshots vergleichen und Releases einsehen.
- Hauptbereiche: Abgrenzung zu Working Transactions, Snapshot-Auswahl,
  cursor-paginierter Diff und Release-Panel.
- Primäre Aktionen: Ausgang/Ziel wählen, optional Node filtern und Snapshot oder
  Release im Knowledge-Read-Kontext öffnen.
- Zustände: ungültige Query-IDs, noch nicht gewählte Vergleichsstände, leere
  Historie und geladener/fehlerhafter Diff.
- Code/Test: [`HistoryPage.razor`](../src/KnowHowToAI.Server/Web/Features/History/HistoryPage.razor),
  [`HistoryPageTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/History/HistoryPageTests.cs).

## 6. Gemeinsame Verträge und Detailquellen

- **Read-/Arbeitskontext:** Auflösung und gegenseitiger Ausschluss der drei
  Read-Selektoren: [`WebReadContextResolver.cs`](../src/KnowHowToAI.Server/Web/State/WebReadContextResolver.cs).
  Der flüchtige Seitenzustand für Node, Zielgruppe, Kontext, `ChangeVersion` und
  Dirty ist [`WorkspaceState.cs`](../src/KnowHowToAI.Server/Web/State/WorkspaceState.cs).
- **Dirty und Navigation:** Editor-Komponenten ändern den zentralen Zustand;
  [`NavigationProtection.razor`](../src/KnowHowToAI.Server/Web/Components/Layout/Shell/NavigationProtection.razor)
  schützt interne und externe Navigation. Persistierte Transaction-Änderungen
  sind kein Browser-Dirty-State.
- **Feedback und Dialoge:** Zustandssemantik liegt bei
  [`Shared/Feedback`](../src/KnowHowToAI.Server/Web/Components/Shared/Feedback),
  blockierende Bereiche bei [`Shared/States`](../src/KnowHowToAI.Server/Web/Components/Shared/States)
  und Bestätigungen bei [`Shared/Dialogs`](../src/KnowHowToAI.Server/Web/Components/Shared/Dialogs).
- **Normative Querschnittsregeln:** Accessibility-/Layoutnachweise und
  Ownership stehen in den [Web-UI-Guardrails](../.agents/rules/WebUiHtmlCss.mdc),
  die Browsercheckliste in der [manuellen UI-Abnahme](Manuelle-UI-Abnahme.md).
  Fachliche Invarianten bleiben in [Invarianten](Invarianten.md); technische
  Schichtung, Application-Grenzen und Testgrenzen in [Architektur](Architektur.md).
