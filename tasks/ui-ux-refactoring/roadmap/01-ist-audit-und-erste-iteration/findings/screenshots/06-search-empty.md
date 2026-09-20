# 06 – Suche leer

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/06_search_empty_desktop_1280x800.png` · Route `/search` · Suchgrundzustand ohne Treffer · Desktop 1280×800.

## Neutrale Beobachtung

- Die Suche ist als eigener Shell-Bereich mit Eingabe-/Filterfläche erkennbar.
- Filter und Suchparameter nehmen im sichtbaren Bereich viel Raum ein.
- Der Ergebnisbereich ist leer und erhält weniger visuelle Führung als die Filter.
- Es gibt keinen ausgewählten Treffer und keinen Node-Content.
- Der leere Zustand benennt den nächsten Suchschritt nur schwach.
- Die globale Navigation bleibt gleichzeitig sichtbar.

## Probleme und Schwere

- **P1 – Verhältnis:** Filter verdrängen die Fläche, die Ergebnisse bzw. die nächste Aufgabe erklären würde.
- **P1 – Leerzustand:** Es fehlt eine klare, handlungsorientierte Lesart von „noch keine Treffer“.
- **P2 – Priorität:** Mehrere Filter wirken gleich wichtig, obwohl noch keine Suchaufgabe abgeschlossen ist.
- **P2 – Sprache:** Technische Suchparameter stehen vor einer einfachen Arbeitsanweisung.

## Gelungene Aspekte

- Die Suche ist unmittelbar über die Shell erreichbar.
- Filterzustand und Eingabemöglichkeit sind sichtbar.
- Der leere Zustand ist reproduzierbar und nicht mit zufälligen Ergebnissen vermischt.

## Folgerungen ohne Featureausweitung

- Vorhandene Eingabe als primären Arbeitsabschnitt führen und Filter sekundär bündeln.
- Den bestehenden Leerzustand kurz auf die nächste Suchhandlung ausrichten.
- Keine Suchlogik, Rankingregel oder neue Filterfunktion ändern.
- Ergebnisse und Filter im M1.3-Re-Audit erneut vergleichen.

## Abhängigkeiten

Suchparameter, Ergebnis-/Navigationsvertrag, Accessibility; nachgelagerter Kandidat außerhalb M1.2.

## Audit-Lücken

Keine.
