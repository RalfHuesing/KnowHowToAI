# Bedienmodell und mentale Modelle

**Version:** 1.0 · **Status:** verbindlich

## Zielgruppe und Erlebnis

Support-Mitarbeitende und Consultants suchen, lesen, prüfen und pflegen Wissen. Sie sollen die nächste sinnvolle Aufgabe ohne technische Vorbildung erkennen. Die UI ist ein System: gleiche Zustände, gleiche räumliche Regeln und gleiche Aktionshierarchie gelten featureübergreifend. Modern-clean bedeutet ruhige Flächen, harmonische Abstände, klare deutsche Aufgabenbegriffe und hohe Dichte ohne Diagnose-Übergewicht.

**Verbindlich:** Keine bestehende Funktion, Route, Fachbedeutung oder technische Information wird durch die visuelle Vereinfachung entfernt. Technische Details erscheinen progressiv, sobald sie für Diagnose, Nachweis oder Expertenarbeit relevant sind. UI-Führung ersetzt lange Erklärtexte, nicht die fachliche Wahrheit.

## Mentale Modelle

### Baum: „Wo liegt etwas?“

Der Wissensbaum ist die räumliche Orientierung. Ein Node hat eine stabile `NodeId`, einen Titel in der Hierarchie und optionalen Content je Zielgruppe. Auswahl, Breadcrumb und Detailansicht müssen denselben Node meinen. Expandieren, Paging und Drag-and-drop verändern die Ansicht; sie verändern den fachlichen Zustand nur, wenn eine bestehende Strukturmutation tatsächlich ausgeführt wird.

### Zielgruppe: „Für wen sehe ich es?“

Eine Zielgruppe ist eine Inhalts- und Zielgruppenperspektive, keine Authentifizierung und keine ACL. Sie entscheidet, welcher Content aufgelöst wird; sie gewährt keine Berechtigung. Die UI muss `requestedAudience`, `resolvedAudience` und Fallback-Provenienz verständlich unterscheiden.

### ReadContext: „Welche Linse nutze ich?“

`ReadContext` ist die Leselinse: Current Snapshot, historischer Snapshot, Release oder Working Transaction. Er ist nicht bloß ein Filter und nicht austauschbar mit Zielgruppe oder Node. Genau ein Query-Selektor `transactionId`, `snapshotId` oder `releaseId` darf gelten; ohne Selektor wird Current gelesen. Die Kontextleiste hält diese Linse global sichtbar.

### Transaction: „Welche Arbeitskopie ändere ich?“

Eine Transaction ist eine sichtbare Arbeitskopie eines Snapshots. Alle Node-, Zielgruppen- und Content-Mutationen gehören zu ihr. Mehrere Clients dürfen parallel schreiben; stale `ChangeVersion` wird fachlich abgelehnt. Der Working-Zustand bleibt bis Commit oder Discard offen.

### Save ist nicht Commit

`Speichern` schreibt eine einzelne Node-, Zielgruppen- oder Contentänderung in die Working Transaction und aktualisiert die sichtbare `ChangeVersion`. `Commit` prüft und veröffentlicht die gesamte Arbeitskopie als Current Snapshot; `Discard` verwirft sie vollständig. Ein Editor-Dirty-State ist lokale unpersistierte Eingabe vor `Speichern`; er darf nicht mit uncommitted Working-Änderungen oder globalem Discard vermischt werden.

### Bearbeiten beginnt am Wissenseintrag

Ein gelesener Wissenseintrag führt seine lokale Hauptaktion sichtbar als
`Bearbeiten`, sofern eigener expliziter Independent-Content vorhanden ist. Die
Aktion öffnet einen kleinen Auswahl-/Startdialog für eine kompatible offene
Arbeitskopie oder den ausdrücklichen Start einer neuen Arbeitskopie. Danach
öffnet derselbe `NodeId` mit derselben Zielgruppe direkt im vorhandenen
Working-Editor. Nutzer müssen weder `Node`, `Transaction` noch eine
Transaktionsübersicht kennen, um den Einstieg zu finden; diese technischen
Begriffe bleiben progressive Details.

Fallback, `None` und `Derived` werden fachlich erklärt und bleiben in M2 an
ihren bestehenden Read-only-/Working-Grenzen. Neue Contentaktionen gehören zu
M5.4 und werden nicht durch eine irreführende `Bearbeiten`-Affordance
vorweggenommen.

## Gestaltungsfolgen

- **Eine primäre Aktion je Abschnitt:** Ein sichtbarer Arbeitsabschnitt führt genau eine bevorzugte nächste Handlung. Sekundäre Wege bleiben sichtbar, aber leiser.
- **Global versus lokal:** Globale Kontext-/Navigationsaktionen stehen in der Shell; lokale Node-, Editor- und Transaktionsaktionen stehen beim betroffenen Inhalt. Keine globale Aktion wird als lokale Mutation missverstanden.
- **Führung vor Erklärung:** Überschrift, Status, Ergebnis und nächste Aktion stehen vor technischen IDs, Revisionen und Prozessdiagnose. Erklärtext ist kurz; die UI zeigt die Reihenfolge durch Position, Gruppierung und Zustandswechsel.
- **Seltene Wege kompakt:** History und Download sind seltene, sekundäre Folgeaktionen. Vorläufig sind Textlinks oder Icon-plus-Text zulässig; Icon-only ist nicht die Standardausprägung.
- **Tastatur:** Die bestehende native Tastatur-/Fokussemantik bleibt erhalten. Tastatur ist für dieses Refactoring kein eigenes Produktziel oder separates Abnahmekriterium; ein absichtlicher Bruch ist dennoch unzulässig.

## Progressive technische Tiefe

Ebene 1 benennt Aufgabe und fachliches Ergebnis; Ebene 2 zeigt Modus und relevante Auswirkungen; Ebene 3 legt IDs, Revisionen, `ChangeVersion`, Source und Debug-Details in nativen Details-/Expertenbereichen offen. Kein technischer Wert wird unlesbar oder unzugänglich, aber kein leerer Wert darf die Arbeitsaufgabe verdrängen.
