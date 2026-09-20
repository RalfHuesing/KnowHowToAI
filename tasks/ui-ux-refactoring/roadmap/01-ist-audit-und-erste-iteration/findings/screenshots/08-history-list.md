# 08 – History-Liste

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/08_history_list_desktop_1280x800.png` · Route `/history` · Snapshot-/History-Liste · Desktop 1280×800.

## Neutrale Beobachtung

- Die Ansicht stellt historische Einträge/Snapshots in einer Liste bzw. einem technischen Zustandsbereich dar.
- Zeit-, Revisions- oder Identifikatorinformationen erhalten viel Aufmerksamkeit.
- Der fachliche Grund, warum ein Eintrag für Support relevant ist, wird nicht vorangestellt.
- Die Auswahl eines Eintrags ist als technische Listenhandlung erkennbar.
- Der vorhandene Contentvergleich ist nicht als klarer primärer Arbeitsabschnitt geöffnet.
- Shell und Navigationskontext bleiben sichtbar.

## Probleme und Schwere

- **P1 – Sprache:** History-Begriffe und Zustandswerte sind für die Zielgruppe technisch.
- **P1 – Aufgabe:** Bedeutung und nächster Schritt eines Eintrags sind nicht vorrangig.
- **P2 – Vergleich:** Der Übergang von Liste zu Diff ist nicht als eindeutiger Arbeitsablauf lesbar.
- **P2 – Dichte:** Technische Werte konkurrieren mit fachlicher Zusammenfassung.

## Gelungene Aspekte

- Historischer Kontext ist separat erreichbar und nicht mit dem aktuellen Content vermischt.
- Einträge sind als reproduzierbarer Zustand vorhanden.
- Die technische Detailtiefe kann grundsätzlich als Experteninformation erhalten bleiben.

## Folgerungen ohne Featureausweitung

- Fachliche Auswahlhandlung vor Revisions- und ID-Werten gruppieren.
- Technische Werte progressiv darstellen, ohne History-Semantik zu ändern.
- Eine bestehende Auswahlaktion je gleichzeitig sichtbarem History-Arbeitsabschnitt führen.
- Diff-Zustände erst nach belastbarer Capture-Vorbedingung bewerten.

## Abhängigkeiten

Snapshot-/Diff-Vertrag, Node-History-Navigation und M1.2-T1 für Zustand 09.

## Audit-Lücken

Keine für die sichtbare Grundliste.

## M1.5-Re-Audit

`temp/ui-audit/2026-09-20_21-35-15/08_history_list_desktop_1280x800.png`
bestätigt die bestehende History-Liste mit den vorhandenen Aktionen „Als
Ausgang wählen“ und „Als Ziel wählen“. Die technische Snapshot-/Transaction-
Einordnung ist sichtbar, konkurriert aber mit der fachlichen Auswahlhandlung.
M1.5-T5 führt diese Auswahl und den anschließenden Vergleichskontext stärker;
History-, Snapshot- und Routenverträge bleiben unverändert.

## M1.5-T5-Ergebnis

Die History führt die vorhandene Auswahl jetzt in einem gemeinsamen
Vergleichs-Arbeitsabschnitt. Ausgang und Ziel werden mit ihrem aktuellen
Auswahlstatus sichtbar zusammengefasst; die bestehenden Aktionen „Als Ausgang
wählen“ und „Als Ziel wählen“ bleiben je Snapshot unverändert erreichbar.
Primäre Snapshot-/Zeitinformationen stehen vor den technischen Werten, die
unter „Technische Details“ vollständig zugänglich bleiben. Die bestehende
Snapshot-Navigation, Pagination und Release-Funktion wurden nicht geändert.

Der gezielte UiAudit-Lauf
`temp/ui-audit/m1-5-t5/2026-09-20_23-20-36/08_history_list_desktop_1280x800.png`
belegt den gemeinsamen Auswahlrahmen im Desktop-Viewport 1280×800.
