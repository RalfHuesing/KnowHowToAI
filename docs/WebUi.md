# Web-UI-Gesamtbild (Ist-Zustand)

## Zweck und Grenze

Die Weboberfläche stellt den Wissensarbeitsplatz und den Entwurfsablauf bereit.
Core-Anwendungsfälle, SQL-Persistenz sowie MCP-Tools für Suche, Historie,
Zielgruppenverwaltung und Markdown-Export bleiben transportneutral erhalten.
Die Weboberfläche besitzt keine Legacy-Seiten oder Weiterleitungen für diese
Use-Cases.

## Shell und Ownership

`App.razor` stellt die interaktive Server-Circuit-Grenze bereit. `MainLayout`
besitzt Skip-Link, Header, Navigation, aktiven Entwurfslink, `main#shell-main`,
Dirty-Schutz, Toasts und Reconnect-Zustand. `PageFrame` stellt den gemeinsamen
Seitenrahmen bereit. Die Wissensseite besitzt Tree, Knotenauswahl, Dokument,
Zielgruppenauswahl und deren Query-Kontext. `DraftPage` besitzt Review,
Validierung, Diff, Übernahme, Verwerfen und den lesenden Abschlusszustand.
Gemeinsam verwendete Draft-Komponenten liegen unter
[`Web/Features/Drafts`](../src/KnowHowToAI.Server/Web/Features/Drafts).

## Produktive UI-Routen

| Route | Verhalten | Nachweis |
|---|---|---|
| `/` | Ersetzt die URL durch `/knowledge`. | [`KnowledgeRedirect.razor`](../src/KnowHowToAI.Server/Web/Components/Navigation/KnowledgeRedirect.razor) |
| `/knowledge` | Liest den Current Snapshot oder bei `transactionId` den offenen Entwurf. `audienceId` wählt die Perspektive; fehlt eine gültige Auswahl bei vorhandenen Zielgruppen, fordert ein Dialog zur Auswahl auf. | [`KnowledgePage.razor`](../src/KnowHowToAI.Server/Web/Features/Knowledge/KnowledgePage.razor), [`KnowledgeTreeSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/ReadOnly/KnowledgeTreeSmokeTests.cs) |
| `/knowledge/{NodeId:guid}` | Wie `/knowledge`, zusätzlich direkte Knotenauswahl und Wiederherstellung des Pfades. | [`KnowledgeTreeMoveSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/Transactions/KnowledgeTreeMoveSmokeTests.cs) |
| `/drafts` | Listet offene Entwürfe und ermöglicht den Einstieg in den Arbeitskontext. | [`DraftPage.razor`](../src/KnowHowToAI.Server/Web/Features/Drafts/DraftPage.razor), [`DraftWorkflowSmokeTests.cs`](../tests/KnowHowToAI.BrowserTests/Drafts/DraftWorkflowSmokeTests.cs) |
| `/drafts/{TransactionId:guid}` | Prüft einen Entwurf; offene Entwürfe bieten Diff, Validierung, Commit und Discard. Abgeschlossene Entwürfe sind schreibgeschützt. | [`DraftLifecycleTests.cs`](../tests/KnowHowToAI.Web.Tests/Features/Drafts/DraftLifecycleTests.cs) |

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

`WorkspaceState` hält nur den Circuit-Zustand für ausgewählten Node,
Zielgruppe, Current-/Draft-Kontext, geladenen Snapshot und `ChangeVersion`.
`KnowledgePageContextResolver` löst ausschließlich Current oder eine offene
`transactionId` für die Wissensroute auf. Snapshot-/Release-Auswahl gehört nicht
zur Weboberfläche; die gleichnamigen historischen MCP/Core-Funktionen bleiben
bestehen.

## Export- und Featuregrenze

Die Weboberfläche besitzt keinen Markdown-Download-Endpunkt. Markdown-Export
bleibt über den MCP-Exportvertrag verfügbar. Das lesende Knotendokument zeigt
Inhalt und Fallback-/Freshness-Informationen; Änderungen erfolgen ausdrücklich
im Draft-Kontext.

## Verbindliche UI-Nachweise

Die Routengrenze und MCP-/Asset-Erreichbarkeit stehen in den Hosttests. Die
verbleibenden Web-Komponenten werden durch FastTests und Browser-Smokes geprüft;
HTML-Semantik, responsive Reflow und Layoutgrenzen folgen den
[Web-UI-Guardrails](../.agents/rules/WebUiHtmlCss.mdc) und der
[manuellen UI-Abnahme](Manuelle-UI-Abnahme.md).
