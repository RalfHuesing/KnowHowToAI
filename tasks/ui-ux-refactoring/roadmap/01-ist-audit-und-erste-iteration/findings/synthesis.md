# Ist-Synthese – UI-Audit

## Evidenzbasis

Die historischen 20 Einzelbefunde unter [findings/screenshots](screenshots) beziehen sich auf den reproduzierbaren Lauf `temp/ui-audit/2026-09-20_18-36-16`. Der aktuelle M1.3-Re-Audit verwendet `temp/ui-audit/2026-09-20_20-05-26`, Desktop 1280×800, mit `manifest.json` und erneut 20 Zuständen. Die Bilder und Manifeste sind temporär; diese Synthese und die Einzelbefunde sind der versionierte Nachweis. Ein Befund beschreibt den sichtbaren Zustand, nicht eine vermutete Produktabsicht.

## Priorisierte Cluster

### P0/P1 – Handlungsfähigkeit im Wissensarbeitsplatz

In 15 sind Strukturaktionen im ersten Viewport nicht sichtbar. In 16 und 17 sind Dirty-State und Editorinhalt sichtbar, das Speichern liegt jedoch unterhalb des Viewports. Damit ist die zentrale Support-Aufgabe – Text einer Node bearbeiten und sicher abschließen oder verwerfen – nicht als zusammenhängender Ablauf lesbar. Die technische Metadaten-/Linkfläche nimmt gleichzeitig viel vertikalen Raum ein. Siehe 15–17.

Der M1.2-T2-Re-Audit vom 20.09.2026 zeigt den korrigierten Ist-Zustand:
Strukturaktionen stehen in 15 im ersten Arbeitsabschnitt, Speichern und Dirty-
Status in 16/17 direkt am Editor. „Working“ beziehungsweise „Nur lesen“ sind
am Node sichtbar; technische Metadaten liegen unter „Technische Details“.
Nachweis: `temp/ui-audit/2026-09-20_20-05-26/`.

### P0/P1 – Bestätigung und destruktive Sicherheit

20 beweist den ausgelösten Löschpfad, zeigt aber im 1280×800-Viewport keine sichtbare Bestätigung; die erwartete Bestätigung liegt unterhalb des sichtbaren Bereichs. Das ist eine Audit-Lücke mit hoher Relevanz für destruktives Verhalten, keine Freigabe für eine Produktannahme. 13/14 zeigen zusätzlich inkonsistente, harte Dialograhmen. Siehe 13, 14 und 20.

### P1 – Technischer Zustand verdrängt Inhalt

15–17 führten Revision, Node-ID, Verfügbarkeit, Inhaltsmodus und Links vor dem eigentlichen Inhalt. 18/19 machen RoleId und den globalen Kontext prominent, während die aufgabenorientierte Erklärung zurücktritt. In 15–17 sind diese technischen Angaben nun unter „Technische Details“ progressiv offengelegt; die Information bleibt erhalten. Siehe 15–19.

### P1/P2 – Modus und Kontext sind implizit

18 kennzeichnet Read-only verständlich, 19 leitet Working nur aus der sichtbaren Formular-/Aktionsfläche ab. Der globale Hinweis „Keine Rolle ausgewählt“ bleibt daneben stehen. In 15–17 ist der Arbeitsmodus am Node sichtbar; 16/17 führen den Dirty-Hinweis und Speichern im selben Inhaltsabschnitt. Transaktionsweites Verwerfen bleibt separat.

### P2 – Navigation, Suche und History als Folgearbeit

01 wirkt technisch-diagnostisch dominiert; 02/03 erklären Rollenauswahl und Bestätigung schwach. 06/07 lassen Filterfläche Ergebnisse verdrängen, 08 ist technisch formuliert. Der neue Lauf bestätigt für 09 einen sichtbaren Diff-Zustand; 06 bleibt der Grundzustand ohne Nulltreffer-Produktbeleg. Diese Bereiche werden nur in den kleinen M1.4-Leaves weitergeführt.

## Audit-Lücken und Konsequenz

03 ist nur der ausgewählte Zustand vor einer belastbaren Root-Bestätigung, 09 belegt keinen Snapshot-Diff, 20 zeigt keine sichtbare Löschbestätigung. Diese drei Lücken sind in den Einzeldateien markiert und bilden M1.2-T1. Bis dahin werden sie nicht als Produktbefund „behoben“ dargestellt.

Der stabilisierte UiAudit-Lauf `temp/ui-audit/2026-09-20_19-48-27` war der technische Zwischenstand für M1.2-T1. Der grüne manuelle M1.3-Re-Audit `temp/ui-audit/2026-09-20_20-05-26` bestätigt denselben capture-seitigen Abschluss erneut: 03 belegt den gerenderten Root nach geschlossener Rollenauswahl, 09 zeigt sichtbare Snapshot-Änderungen und 20 zeigt den fokussierten vorhandenen Löschdialog. Die ursprünglichen Befunde bleiben als historische M1.1-Beobachtung erhalten; sie sind nicht als Produktkorrektur zu lesen.

## M1.3-Re-Audit-Ergebnis

M1.2 ist abgenommen. Die Zustände 15–17 bestätigen die beabsichtigte erste Viewport-Führung: Strukturaktionen beziehungsweise Dirty-Status und Speichern sind sichtbar, technische Details bleiben progressiv zugänglich. Die Zustände 03/09/20 sind semantisch und visuell auditierbar. Der Re-Audit ändert keine Fachverträge und eröffnet keine neue Produktfunktion.

## M1.4-Folgepriorität

1. Transaktionen: 11 führt die bestehende Öffnen-/Fortsetzen-Aktion und ordnet technische Leerwerte sekundär; 12 führt die bestehende Reihenfolge Validieren → Commit/Verwerfen und ordnet technische Metadaten sekundär; 13/14 erhalten eine gemeinsame moderne Dialogdarstellung bei unverändertem Vertrag.
2. Shell-Navigation: Ein neuer Nutzerbefund beschreibt die linke Navigation als rohe, historisch wirkende href-Linkliste. M1.4-T7 ordnet ausschließlich bestehende Ziele in einer modernen, cleanen visuellen Hierarchie mit aktivem Zustand; Hover/Focus bleiben normale CSS-Zustände, ohne eigenes Keyboard-Ziel.
3. Suche 07: Filter kompakter/sekundär, Trefferzahl und vollständige erste vorhandene Trefferkarte im 1280×800-Viewport; Suchsemantik bleibt unverändert. 06 ist nur Grundzustand und kein Nulltreffer-Nachweis.
4. Dashboard 01: vorhandener Wissenszugang primär, Diagnose/Snapshot sekundär. 02 und 05 erhalten nur fachlich sichere Microcopy ohne neue Handlung.

P2-Nacharbeiten 15–17 (gleichrangige Strukturaktionen, doppelter Dirty-Hinweis, Fokus-Scroll) sind ausdrücklich nicht Teil der ersten M1.4-Reihe. `RolesPage.razor.cs` bleibt außerhalb des Scopes.

## Nächster kleiner Slice

M1.2-T1 stabilisierte nur die capture-seitige Sichtbarkeit und Assertions.
M1.2-T2 ist mit dem dokumentierten Re-Audit abgeschlossen. M1.3 ist anhand des
Laufs `temp/ui-audit/2026-09-20_20-05-26` abgeschlossen; Folgearbeit bleibt auf
die sechs kleinen, belegten M1.4-Slices begrenzt.

## M1.4-T7-Ergebnis

Die linke Shell-Navigation verwendet weiterhin exakt die vier Ziele Start (`/`),
Suche (`/search`), Transactions (`/transactions`) und Rollen (`/roles`), stellt
sie aber als ruhig gruppierte Linkflächen mit klaren Abständen, Surface und
aktivem `NavLink`-Zustand dar. Der Scope bleibt rein visuell; Rollen-, Routing-
und Responsive-Verträge bleiben unverändert. Der UiAudit-Lauf
`temp/ui-audit/2026-09-20_21-09-03` bestätigt die Navigation in 01, 11, 12 und
15 sowie die Regression 20 bei 1280×800. Die Shell-Baseline für 1280×720 wurde
nach manueller Diff-Prüfung aktualisiert; die kompakte 1024×720-Baseline blieb
unverändert.

## M1.4-T1-Ergebnis

Zustand 11 ist mit dem Capture `temp/ui-audit/2026-09-20_20-25-44/11_transactions_open_desktop_1280x800.png` erneut belegt. Die vorhandene Fortsetzen-Aktion ist im ersten Arbeitsabschnitt primär sichtbar; Lifecycle-Metadaten sind unter „Technische Details“ progressiv erreichbar. Die bestehende Route, Working-/Dirty-/Commit-/Discard-Semantik und Accessibility-Reihenfolge wurden nicht verändert. Der kleinste Web-Komponententest, der Transaktions-Browser-Smoke und der gezielte UiAudit-Lauf sind grün; M1.4-T3 bis T6 bleiben offen.

## M1.4-T2-Ergebnis

Zustand 12 ist mit dem Capture `temp/ui-audit/2026-09-20_20-45-17/12_transaction_detail_desktop_1280x800.png` belegt. Die bestehende Reihenfolge Validieren → Commit/Verwerfen ist im ersten Viewport als Arbeitsablauf geführt; Commit ist die primäre Abschlussaktion, Verwerfen bleibt sicher erreichbar und sekundär. Technische IDs, Statusdetails und bekannte Leerwerte sind unter „Technische Details“ progressiv angeordnet, der unveränderte Netto-Diff steht nach dem Abschlussabschnitt. Web-Komponententest, Transaktions-Browser-Smoke, UiAudit und Build sind grün; eigene Tastatur-/Fokusnachweise waren nicht Teil des Produktziels. M1.4-T3 bis T6 und T7 bleiben offen.

## M1.4-T3-Ergebnis

Zustände 13 und 14 teilen mit dem Capture-Lauf `temp/ui-audit/2026-09-20_20-58-00/` eine gemeinsame Bestätigungsdialogdarstellung: ruhige Oberfläche, konsistente Breite und Abstände sowie genau eine visuell primäre Abschlussaktion. Commit bleibt blau primär, Verwerfen bleibt als destruktive rote Aktion klar erkennbar. Die bestehenden Folgeformulierungen, Button-Reihenfolge, Escape-/Abbruchpfad und Lifecycle-Verträge wurden nicht verändert; native Fokussemantik bleibt im bestehenden `AppDialog`-Pfad. Beide 1280×800-Captures zeigen den vollständigen Dialog. M1.4-T4 bis T6 und T7 bleiben offen.

## M1.4-T4-Ergebnis

Die Suche führt die Treffer wieder vor den Filterdetails: Das bestehende
Filterfeld ist bei unveränderter Semantik in einem kompakten Desktop-
2-Spaltenraster sekundär angeordnet, während Trefferzahl und die vollständige
erste Trefferkarte im 1280×800-Viewport sichtbar sind. Der Such-Smoke prüft
weiterhin den vorhandenen Fallback-Filter und die bestehende Navigation. Der
UiAudit-Lauf `temp/ui-audit/2026-09-20_21-18-44/` bestätigt die Zustände 06 als
Regression und 07 als Ergebnisfokus; Suchroute, Ranking, Reihenfolge und
Trefferaktion wurden nicht verändert. M1.4-T5 und T6 bleiben offen.

## M1.4-T5-Ergebnis

Zustand 01 führt den bestehenden Wissenszugang im ersten Arbeitsbereich als
primäre Aufgabe: Der vorhandene `/knowledge`-Einstieg erscheint als klar
markierte primäre Linkfläche mit aufgabenorientierter Startbotschaft. Der
technische Snapshot-/Release-Stand bleibt vollständig erhalten und ist unter
„Systemstand und Release anzeigen“ progressiv erreichbar; der vorhandene
Historie-Link bleibt als sekundäre Folgeaktion sichtbar. Es wurden keine neuen
Routen, Aktionen, Daten oder Verträge eingeführt. Der Capture-Lauf
`temp/ui-audit/2026-09-20_21-26-50/` bestätigt Zustand 01 bei 1280×800; der
gezielte Dashboard-Komponententest und Dashboard-Browser-Smoke sind grün.
M1.4-T6 bleibt offen.
