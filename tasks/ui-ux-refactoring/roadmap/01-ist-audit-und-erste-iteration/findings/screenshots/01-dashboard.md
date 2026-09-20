# 01 – Dashboard

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/01_dashboard_desktop_1280x800.png` · Route `/` · Dashboard-Grundzustand · Desktop 1280×800.

## Neutrale Beobachtung

- Der obere Shell-Bereich zeigt den Produktnamen, den Wissens-/Transaktionskontext und globale Navigation.
- Der linke Navigationsbereich nimmt dauerhaft Raum ein und bietet Start, Suche, Transaktionen und Rollen als gleichartige Links.
- Der zentrale Einstieg ist von technischen Status-/Diagnoseinformationen geprägt.
- Ein Diagnosebutton ist sichtbar und erhält durch seine Platzierung Aufmerksamkeit.
- Der sichtbare Bereich erklärt keine konkrete erste Aufgabe für Support oder Consulting.
- Die Aufnahme zeigt keine inhaltliche Node-Auswahl und keinen unmittelbar sichtbaren Wissensarbeitsfluss.

## Probleme und Schwere

- **P1 – Hierarchie:** Technische Dashboarddominanz erschwert die Antwort auf „Was ist meine nächste Aufgabe?“
- **P1 – Orientierung:** Es bleibt unklar, ob der nächste Schritt Start, Rollenwahl oder Wissenssuche ist.
- **P2 – Aktion:** Der Diagnosebutton konkurriert mit dem eigentlichen Einstieg, ohne seine Zielgruppe im sichtbaren Text zu erklären.
- **P2 – Sprache:** Shell-/Systembegriffe sind nicht in eine aufgabenorientierte Startbotschaft übersetzt.

## Gelungene Aspekte

- Produktname und Shell sind stabil und visuell ruhig.
- Die Hauptbereiche sind vollständig und unmittelbar erreichbar.
- Der Zustand ist kompakt genug, um als reproduzierbare Ausgangslage zu dienen.

## Folgerungen ohne Featureausweitung

- Den vorhandenen Wissenseinstieg im sichtbaren Arbeitsbereich vor Diagnoseinformationen führen.
- Eine bestehende primäre Aktion je gleichzeitig sichtbarem Arbeitsabschnitt klar hervorheben.
- Diagnose als sekundären, erklärten Systemzustand behandeln.
- Keine neue Startfunktion und keine Änderung der Backenddiagnose ableiten.

## Abhängigkeiten

Bestehende Shell-/Navigationsstruktur, Rollen-/Kontextauswahl, Dashboard- und Diagnosevertrag; manuelle Prüfung in M1.3.

## Audit-Lücken

Keine.

## M1.3-Re-Audit und Folgeentscheidung

Aktuelle Quelle: `temp/ui-audit/2026-09-20_20-05-26/01_dashboard_desktop_1280x800.png`. Der Re-Audit bestätigt den P1-Befund: Der vorhandene Wissenszugang bleibt die fachlich naheliegende primäre Einstiegsaufgabe, während Diagnose und Snapshot technisch sekundär sind. M1.4-T5 schneidet ausschließlich diese bestehende Hierarchie und sichere Microcopy; es entsteht keine neue Dashboard-Handlung.

Ein ergänzender Nutzerbefund beschreibt die linke Shell-Navigation als rohe href-Linkliste mit historischer Anmutung. Dieser Befund wird separat in [M1.4-T7](../../tasks/M1.4-T7.md) behandelt: bestehende Ziele, moderne visuelle Hierarchie und aktiver Zustand, aber keine neuen Routen oder Features.
