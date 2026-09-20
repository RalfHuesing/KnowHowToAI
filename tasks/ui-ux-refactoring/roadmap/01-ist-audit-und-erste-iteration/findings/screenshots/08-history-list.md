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
