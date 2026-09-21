# UI/UX-Refactoring

Dieses eigenständige, rollierende Vorhaben verbessert die Verständlichkeit und die visuelle Führung der bestehenden Weboberfläche für Support-Mitarbeitende und Consultants. Es verändert weder Fachverträge noch bestehende Funktionen; neue Features sind ausdrücklich nicht Teil des Vorhabens.

## Leitplanken

- aufgabenorientierte, sichtbare deutsche Begriffe und genau eine primäre Aktion je gleichzeitig sichtbarem Arbeitsabschnitt;
- Inhalt und nächste Aufgabe vor technischem Systemzustand;
- technische Begriffe, IDs und Versionen progressiv in einem nativen Expertenbereich;
- Accessibility, Responsive-Verhalten sowie Dirty- und Transaktionsverträge bleiben erhalten. Bestehende native Tastatur-/Fokussemantik darf ohne Zusatzaufwand erhalten bleiben, ist aber kein eigenes Produktziel und kein eigenes Abnahmekriterium;
- kleine vertikale Slices mit Belegen statt Big Bang;
- Backend-Fachbegriffe und technische Verträge bleiben unverändert.

## Aufbau

Der verbindliche konzeptionelle Einstieg liegt in [Bedienkonzept UI/UX-Refactoring](konzept/README.md). Dort sind Bedienmodell, mentale Modelle, Nutzerreisen, IA-Optionen, Layout-/Aktionssystem, Evidenzinventar und Entscheidungsregister schlank verlinkt.

Der abgeschlossene historische Slice ist unter [M1 Ist-Audit und erste Iteration](roadmap/01-ist-audit-und-erste-iteration/roadmap.md) dokumentiert. Die [M1-Planung](roadmap/01-ist-audit-und-erste-iteration/planning.md) beschreibt Reihenfolge und Entscheidungsgrenzen. Befunde liegen in [synthesis.md](roadmap/01-ist-audit-und-erste-iteration/findings/synthesis.md) und je Screenshot in den zugehörigen Dateien. M1.5-T0–T7 sind erledigt.

Die nächste Etappe ist [M2 Systemrahmen und Navigationsfluss](roadmap/02-systemrahmen-und-navigationsfluss/roadmap.md), mit [M2-Planung](roadmap/02-systemrahmen-und-navigationsfluss/planning.md) und vier sequenziellen Leaves plus Abschlussaudit. M2 ist auf Basis der gesetzten Nutzerentscheidungen freigegeben und autonom ausführbar. M2 berücksichtigt M1.5-T7 als bestehende Verantwortung und legt keinen Duplikat-Task an; ein manueller Zwischenstopp für IA, Terminologie oder Bearbeitungseinstieg ist nicht vorgesehen.

Screenshots und `manifest.json` sind reproduzierbare, temporäre Audit-Artefakte unter `temp/ui-audit/` und werden nicht versioniert. Versioniert werden die textlichen Befunde, die Nachweise und die daraus abgeleiteten kleinen Tasks.
