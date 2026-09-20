# Planung M2 – Systemrahmen und Navigationsfluss

## Zweck und Freigabe

M2 ist die nächste kleine Etappe nach M1.5. Sie bearbeitet belegte Handlungs- und Layoutbrüche, nicht die gesamte Weboberfläche. Die drei Leaves sind erst nach Prüfung dieses Dokuments und der Konzeptdokumente ausführbar. Nach ihrer Abnahme stoppt die Umsetzung am manuellen Entscheidungsgate.

## Verbindliche Referenzen

- [UI/UX-Konzeptindex](../../konzept/README.md)
- [Bedienmodell und mentale Modelle](../../konzept/01-bedienmodell-und-mentale-modelle.md)
- [Nutzerreisen und Zustandsflüsse](../../konzept/02-nutzerreisen-und-zustandsfluesse.md)
- [IA-Optionen](../../konzept/03-ia-und-navigationsoptionen.md)
- [Layout- und Aktionssystem](../../konzept/04-layout-und-aktionssystem.md)
- [Findings-/Evidenzinventar](../../konzept/05-findings-und-evidenz.md)
- [Entscheidungsregister](../../konzept/06-entscheidungsregister.md)
- [M1-Roadmap](../01-ist-audit-und-erste-iteration/roadmap.md), insbesondere offenes [M1.5-T7](../01-ist-audit-und-erste-iteration/tasks/M1.5-T7.md)
- Ist-Dokumentation: [`docs/README.md`](../../../../docs/README.md), [`Invarianten`](../../../../docs/Invarianten.md), [`Retrieval`](../../../../docs/Retrieval.md), [`Transaktionen und Historie`](../../../../docs/Transaktionen-und-Historie.md)
- Webfrontend-Zielkonzept: [`Bedienkonzept und UI`](../../../webfrontend/konzept/02-bedienkonzept-und-ui.md), [`Projektstruktur`](../../../webfrontend/konzept/08-projektstruktur-und-codekonventionen.md)

## Reihenfolge

1. **M2.1-T1** erstellt die gemeinsame Layoutbasis und migriert nur benannte Features. `RolesPage` bleibt ausgeschlossen. M1.5-T7 wird weder kopiert noch in M2 als erledigt behauptet; seine offene Checkbox und die bestehende Transaction-Grid-Verantwortung bleiben in M1.
2. **M2.2-T1** beginnt mit einem isolierten roten Test für URL-/Selection-Drift und deckt Create root, Create child, Update und Delete ab. Erst danach folgt die kleinste Korrektur.
3. **M2.3-T1** führt den bestehenden `TransactionPage`-Link `Im Wissensbaum öffnen` in der sichtbaren Bearbeitungsreise; Linkziel bleibt `/knowledge?transactionId=...` mit vorhandener Rolle.
4. **M2-Gate:** manueller Entscheid zu IA, Terminologie und node-lokalem Bearbeitungseinstieg. Ohne Gate keine neuen Routen und keine M2.4-Folgearbeit.

## Design- und Prüfregeln

- **Verbindlich:** volle Monitorbreite für Page-Frame und Arbeitsflächen; `readable` nur für Prosa.
- **Verbindlich:** DRY Tokens/Base-Patterns; keine globale Action Registry; `PageActions` nur für echte seitenweite Aktionen.
- **Verbindlich:** eine primäre Aktion je Abschnitt; globale und lokale Aktionen getrennt.
- **Verbindlich:** technisch bereits vorhandene Informationen, Funktionen und Links bleiben erreichbar; technisch progressive Darstellung ist erlaubt.
- **Verbindlich:** Tastatur ist kein eigenes Produktziel dieser Etappe, native Semantik darf aber nicht absichtlich brechen.
- **Verbindlich:** Layoutmigrationen werden bei 1280, 1920, 2560 sowie bestehender Responsive-Referenz geprüft.
- **Verbindlich:** `NodeId` bleibt stabil; nach Mutation wird die sichtbare Auswahl aktualisiert und URL-/Breadcrumb-Synchronisation geprüft.
- **Nicht freigegeben:** Fallback-/erster Content bei `Availability=None`; dafür gilt `tasks/webfrontend/roadmap/05-rollen-content-und-rich-text/tasks/M5.4-T1.md`.

## Testbudget

Die Leaves verwenden nur den kleinsten passenden Component-/Browser-Test plus gezielte Layoutbelege. M2.1 benötigt keine vollständige visuelle Sanierung; M2.2 benötigt Red-Test-first und die Mutation-Regressionen; M2.3 benötigt den Transaction-Browser-/Komponentennachweis. Ein Volltest, eigene Keyboard-Abnahme und Produktänderungen außerhalb des Scopes sind nicht erforderlich. Für reine Doku-Folgen genügen `git diff --check` und Link-/Diff-Prüfung.

## Entscheidungsgate-Kriterien

- [ ] Die drei Leaves sind mit Nachweisen und Roadmap-Checkboxen abgeschlossen.
- [ ] Kein M1.5-T7-Duplikat und keine Änderung an `RolesPage` wurde eingeführt.
- [ ] IA-Option Hybrid versus stärker getrennte Task-Seiten ist mit Auswirkungen bewertet.
- [ ] Node-lokaler Einstieg versus Transaction-first ist mit echtem Workflow-/Fachfeedback entschieden oder bewusst zurückgestellt.
- [ ] Terminologie, Icon/Text-Regel, PageActions-Kandidaten und Pflichtrolle-vor-Navigation haben eine dokumentierte Entscheidung.
