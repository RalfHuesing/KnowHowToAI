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

## M1.4-Folgepriorität (abgeschlossen)

1. Transaktionen: 11 führt die bestehende Öffnen-/Fortsetzen-Aktion und ordnet technische Leerwerte sekundär; 12 führt die bestehende Reihenfolge Validieren → Commit/Verwerfen und ordnet technische Metadaten sekundär; 13/14 erhalten eine gemeinsame moderne Dialogdarstellung bei unverändertem Vertrag.
2. Shell-Navigation: Ein neuer Nutzerbefund beschreibt die linke Navigation als rohe, historisch wirkende href-Linkliste. M1.4-T7 ordnet ausschließlich bestehende Ziele in einer modernen, cleanen visuellen Hierarchie mit aktivem Zustand; Hover/Focus bleiben normale CSS-Zustände, ohne eigenes Keyboard-Ziel.
3. Suche 07: Filter kompakter/sekundär, Trefferzahl und vollständige erste vorhandene Trefferkarte im 1280×800-Viewport; Suchsemantik bleibt unverändert. 06 ist nur Grundzustand und kein Nulltreffer-Nachweis.
4. Dashboard 01: vorhandener Wissenszugang primär, Diagnose/Snapshot sekundär. 02 und 05 erhalten nur fachlich sichere Microcopy ohne neue Handlung.

P2-Nacharbeiten 15–17 (gleichrangige Strukturaktionen, doppelter Dirty-Hinweis, Fokus-Scroll) sind ausdrücklich nicht Teil der ersten M1.4-Reihe. `RolesPage.razor.cs` bleibt außerhalb des Scopes.

## Nächster kleiner Slice

M1.2-T1 stabilisierte nur die capture-seitige Sichtbarkeit und Assertions.
M1.2-T2 ist mit dem dokumentierten Re-Audit abgeschlossen. M1.3 ist anhand des
Laufs `temp/ui-audit/2026-09-20_20-05-26` abgeschlossen; M1.4 ist mit seinen
sieben Leaves und dem Lauf `temp/ui-audit/2026-09-20_21-35-15` abgeschlossen.
Die Folgearbeit ist als M1.5 auf die sieben oben beschriebenen, kleinen Slices
begrenzt.

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

Zustand 11 ist mit dem Capture `temp/ui-audit/2026-09-20_20-25-44/11_transactions_open_desktop_1280x800.png` erneut belegt. Die vorhandene Fortsetzen-Aktion ist im ersten Arbeitsabschnitt primär sichtbar; Lifecycle-Metadaten sind unter „Technische Details“ progressiv erreichbar. Die bestehende Route, Working-/Dirty-/Commit-/Discard-Semantik und Accessibility-Reihenfolge wurden nicht verändert. Der kleinste Web-Komponententest, der Transaktions-Browser-Smoke und der gezielte UiAudit-Lauf sind grün.

## M1.4-T2-Ergebnis

Zustand 12 ist mit dem Capture `temp/ui-audit/2026-09-20_20-45-17/12_transaction_detail_desktop_1280x800.png` belegt. Die bestehende Reihenfolge Validieren → Commit/Verwerfen ist im ersten Viewport als Arbeitsablauf geführt; Commit ist die primäre Abschlussaktion, Verwerfen bleibt sicher erreichbar und sekundär. Technische IDs, Statusdetails und bekannte Leerwerte sind unter „Technische Details“ progressiv angeordnet, der unveränderte Netto-Diff steht nach dem Abschlussabschnitt. Web-Komponententest, Transaktions-Browser-Smoke, UiAudit und Build sind grün; eigene Tastatur-/Fokusnachweise waren nicht Teil des Produktziels.

## M1.4-T3-Ergebnis

Zustände 13 und 14 teilen mit dem Capture-Lauf `temp/ui-audit/2026-09-20_20-58-00/` eine gemeinsame Bestätigungsdialogdarstellung: ruhige Oberfläche, konsistente Breite und Abstände sowie genau eine visuell primäre Abschlussaktion. Commit bleibt blau primär, Verwerfen bleibt als destruktive rote Aktion klar erkennbar. Die bestehenden Folgeformulierungen, Button-Reihenfolge, Escape-/Abbruchpfad und Lifecycle-Verträge wurden nicht verändert; native Fokussemantik bleibt im bestehenden `AppDialog`-Pfad. Beide 1280×800-Captures zeigen den vollständigen Dialog.

## M1.4-T4-Ergebnis

Die Suche führt die Treffer wieder vor den Filterdetails: Das bestehende
Filterfeld ist bei unveränderter Semantik in einem kompakten Desktop-
2-Spaltenraster sekundär angeordnet, während Trefferzahl und die vollständige
erste Trefferkarte im 1280×800-Viewport sichtbar sind. Der Such-Smoke prüft
weiterhin den vorhandenen Fallback-Filter und die bestehende Navigation. Der
UiAudit-Lauf `temp/ui-audit/2026-09-20_21-18-44/` bestätigt die Zustände 06 als
Regression und 07 als Ergebnisfokus; Suchroute, Ranking, Reihenfolge und
Trefferaktion wurden nicht verändert.

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

## M1.4-T6-Ergebnis

Zustände 02 und 05 führen die vorhandene fachliche Bedeutung jetzt direkt am
Arbeitsabschnitt: Die Rollenauswahl erklärt Zielgruppe/Perspektive und die
Trennung von Inhaltskontext und Zugriffsberechtigung; der Fallback-Detailzustand
erklärt den fehlenden eigenen Inhalt und die Anzeige aus der aufgelösten
Fallback-Rolle. Auswahl, Bestätigung, Rollenauflösung, Datenquelle,
Änderungshistorie und Download bleiben unverändert. Der gezielte UiAudit-Lauf
`temp/ui-audit/2026-09-20_21-35-15/` bestätigt 20/20 Zustände bei 1280×800;
02 und 05 wurden visuell geprüft. Der kleinste Web-Komponententest und Build
sind grün. Die ausdrücklich ausgeschlossene Änderung an `RolesPage.razor.cs`
blieb unangetastet.

## M1.4-Abschluss und Re-Audit-Nachweis

Die sieben M1.4-Leaves T1–T7 sind mit ihren Detail-Checklisten, Nachweisen und
der Roadmap-Checkbox abgeschlossen. Der begrenzte Re-Audit-Lauf
`temp/ui-audit/2026-09-20_21-35-15/manifest.json` enthält erneut alle 20
Zustände bei 1280×800. Der Lauf belegt den erreichten M1.4-Stand; er ist kein
Nachweis für eine neue Produktfunktion und ändert keine Fachverträge. Die in
T5 und T7 zunächst offen gebliebenen, durch die Abschlussnachweise tatsächlich
erfüllten Akzeptanz-Checkboxen wurden bei der Dokumentationskorrektur
konsistent geschlossen.

## M1.5-T0-Ergebnis

Die Shell führt den dauerhaft sichtbaren, quadratischen Drei-Linien-Iconbutton
oben links in der App-Leiste jetzt in Desktop und kompakter Breite; der aktuelle
Zustand ist nicht als sichtbarer Text, sondern zugänglich über den dynamischen
Namen und Tooltip benannt. Desktop startet mit
geöffneter Navigation; derselbe Button schließt sie, gibt die Sidebar-Spalte
an die Arbeitsfläche frei und öffnet sie wieder. Der Button führt den
zugänglichen Zustandsnamen sowie aria-expanded/aria-controls. In kompakter
Breite bleibt die Navigation ein Drawer/Overlay; Escape und der Button geben
den Fokus an den Auslöser zurück. Der redundante Paneltitel, „Arbeitsbereiche“
und „Navigation schließen“ sind entfernt. Start (/), Suche (/search),
Transactions (/transactions) und Rollen (/roles) bleiben die exakt vier
erreichbaren Ziele.

Der Red-Test in MainLayoutTests war vor der Produktänderung rot und ist danach
mit 11/11 grün. LayoutShellSmokeTests (2/2), ResponsiveShellSmokeTests (2/2)
und VisualShellSmokeTests (2/2) sind grün. Die visuellen Shell-Baselines für
1280 × 720 und 1024 × 720 wurden nach manueller Prüfung der aktuellen
Captures übernommen. RolesPage und Rollenverwaltung blieben unangetastet.

## M1.5-T1-Ergebnis

Zustand 04 wartet im bestehenden UiAudit-Runner vor dem Screenshot auf die
vorhandene Shell, den Shell-Header, die Hauptnavigation, den globalen
Shell-Main-Bereich und den sichtbaren Node-Detailtitel. Header, Navigation und
Detailtitel werden zusätzlich mit den Playwright-Viewport-Assertions im
1280×800-Viewport abgesichert. Der gezielte Lauf
`temp/ui-audit/m1-5-t1-rerun/2026-09-20_22-00-50/` ist mit 20/20 Zuständen grün;
das Bild `04_knowledge_node-detail_desktop_1280x800.png` bestätigt die
tatsächlich gerenderte Präsenz. Bei fehlender Präsenz würde der Test als
Audit-Lücke fehlschlagen. Es wurden ausschließlich Testbedingungen geändert;
Produktdateien, Routen, Capture-Namen und Manifestformat blieben unverändert.

## M1.5-T2-Ergebnis

Der ContextSelector in Zustand 02 teilt jetzt die gemeinsame `AppDialog`-Basis
mit den Zuständen 13/14: Surface, feiner Rand, Radius, Schatten, Backdrop,
Breite und Innenabstände sind harmonisiert. Die bestehende Rollen-/Auswahl-
und Dialogsemantik blieb unverändert; `CreateReleaseDialog` erhält als weiterer
AppDialog-Nutzer nur dieselbe Basestyle. Der UiAudit-Lauf
`temp/ui-audit/m1-5-t2/2026-09-20_22-36-23/` bestätigt 20/20 Zustände bei
1280×800, einschließlich 02 sowie der Regressionen 13/14.

## M1.5-T3-Ergebnis

Die Read-only-Detailansicht führt in 04 den vorhandenen Inhalt und in 05 die
vorhandene Fallback-Einordnung mit dem aufgelösten Inhalt vor die bestehenden
sekundären History-/Markdown-Download-Wege. Beide Links bleiben mit denselben
Zielen erreichbar und sind als gemeinsame, ruhige Aktionsgruppe angeordnet.
Technische Details sind weiterhin vollständig vorhanden, aber im nativen
Bereich „Technische Details“ progressiv offengelegt. Read-only-/Fallback-/Rollen-
Verträge, Routen, Datenquellen und Working-Editor 15–17 blieben unverändert.
Der gezielte Lauf `temp/ui-audit/m1-5-t3/2026-09-20_22-45-00/` ist mit 20/20
Zuständen grün; 04/05 sowie 15–17 wurden bei 1280×800 visuell geprüft.

## M1.5-T4-Ergebnis

Die globale `KnowledgeContextBar` trennt Wissensbasis/Read-Context,
fachlichen Bereich und Working-Transaction jetzt als eigene, kompakte
Segmente. Ein `DisplayName` wird bei Current-/Nicht-Transaction-Kontexten als
`Bereich` geführt; bei einer Transaction steht der vorhandene Zweck bzw. Name
als gefüllter `Working-Transaction`-Wert. Ein Working-Segment ohne Zweck und
ohne technische Werte wird nicht gerendert. Base-Snapshot und Änderungsversion
bleiben dort als sekundäre technische Werte verfügbar; Rolle, Dirty-Status und
der vorhandene Kontext-/Rollen-Selektor bleiben eigenständig und unverändert
erreichbar. Read-Context-Auflösung, ViewModel, Query-Parameter, Routen,
Aktionen und Rollenverwaltung wurden nicht geändert.

Der korrigierte UiAudit-Lauf `temp/ui-audit/2026-09-20_23-05-15/` ist mit
20/20 Zuständen bei 1280×800 grün. Die Zustände 10, 12 und 15 wurden visuell
geprüft; 10 zeigt `Bereich: Transactions`, 12/15 den gefüllten Working-
Transaction-Zweck. Die ergänzten Transaktions-Regressionstests decken Zweck
mit und ohne technische Werte ab. `ResponsiveShellSmokeTests` (2/2) ist grün.

## M1.5-T5-Ergebnis

History 08/09 führt die bestehende Ausgangs-/Zielauswahl und das vorhandene
Vergleichsergebnis jetzt als gemeinsamen Arbeitsabschnitt. Der aktuelle
Auswahlstatus ist sichtbar; Zustand 09 zeigt zusätzlich die bestehende
„Snapshot-Diff“-Überschrift, die Änderungszusammenfassung und repräsentative
Einträge. Fachliche Entität, Änderung und Vorher/Nachher-Werte stehen vor den
technischen Primär-/Sekundär-IDs. Snapshot-, Transaction- und Node-IDs bleiben
unter „Technische Details“ vollständig erreichbar. Nicht ausgewählte, leere,
fehlerhafte und ladende Vergleiche bleiben als ihre tatsächlichen Zustände
erkennbar; es wurde keine Auswahl-, Lade-, Paging- oder Diff-Logik verändert.

Der UiAudit-Lauf
`temp/ui-audit/m1-5-t5/2026-09-20_23-20-36/manifest.json` ist mit 20/20
Zuständen bei 1280×800 grün. Die Captures 08 und 09 wurden visuell geprüft;
History-FastTests (8/8), History-Browser-Smoke (1/1), Build und der
AiNetLinter-Verify für die Änderungen (Score 10.0, 0 Verstöße) sind grün. Der
Solution-Verify bleibt wegen des vorbestehenden, außerhalb dieses Scopes
liegenden `AIContextFootprint`-Befunds in `RolesPage.razor.cs` bei Score 9.2;
die Rollenverwaltung wurde gemäß Leaf nicht verändert.

## M1.5-Folgepriorität aus dem Re-Audit

M1.5 bleibt eine sequenzielle Reihe kleiner, rein visueller Slices:

1. **Dringender Shell-Befund:** Die aktuelle Implementierung rendert den
   Header-Toggle nur in `_isCompactMode`, während `PrimaryNavigation` auf
   Desktop den eigenen Textbutton „Navigation schließen“ rendert. Nach dem
   Schließen gibt es auf Desktop keinen sichtbaren Wiederöffnungsweg. Zusätzlich
   sind „Navigation“, „Arbeitsbereiche“ und der Close-Text redundant. M1.5-T0
   führt einen dauerhaften Menübutton oben links in der App-Leiste mit
   zugänglichem Zustandsnamen ein, hält Desktop standardmäßig offen, gibt beim
   Schließen dem Hauptinhalt Platz frei und erhält den kompakten Drawer/Overlay.
   Red-Test-first und die vier bestehenden Routen sind verbindlich.
2. Capture 04 erhält deterministische Shell-/Header-Wartebedingungen und
   Assertions im Test. Der Runner darf Produktzustände weder erzeugen noch
   kaschieren.
3. Zustand 02 erhält die gemeinsame moderne `AppDialog`-Oberfläche von 13/14;
   die Dialogsemantik bleibt unverändert.
4. In den Read-only-Detailzuständen 04/05 stehen Inhalt beziehungsweise die
   fachliche Fallback-Einordnung vor den bestehenden sekundären History- und
   Download-Links; Links und Funktionen bleiben erhalten.
5. Die `KnowledgeContextBar` segmentiert Snapshot, Bereich und Working-
   Transaction sichtbar, ohne Read-Context-Verträge oder Routen anzutasten.
6. History 08/09 führt die vorhandene Ausgang/Ziel-Auswahl und den
   Vergleichskontext. Technische IDs bleiben progressiv und sekundär; die
   Auswahl- und Diff-Logik wird nicht geändert.
7. Die sichtbaren Suchbegriffe `Freshness` und `Findings` werden in einem
   eigenen kleinen Leaf deutsch benannt. M1.5-T6 führt dafür die Gruppenlabels
   „Aktualität“ und „Befunde“ ein; Filterwerte, Suchsemantik, Ranking,
   Trefferreihenfolge, Route und technische Codes bleiben unverändert. Der
   UiAudit-Lauf `temp/ui-audit/m1-5-t6/2026-09-20_23-25-32` bestätigt die
   Darstellung in den Zuständen 06/07 bei 1280×800.
8. Das offene-Transaction-Grid wird in einem eigenen kleinen Leaf als
   Ein-Karten-Grid ohne leere Spalte harmonisiert; Karten, Routen und Aktionen
   bleiben unverändert.

Das Verhaltenstor **„Commit vor Validierung“** ist ausdrücklich zurückgestellt
und nicht implementiert: Bis zu einer separaten fachlichen Entscheidung gilt
weiterhin Validieren → Commit/Verwerfen. `RolesPage.razor.cs` und Rollen-
Verwaltung bleiben ein separater Out-of-scope-Task. Die P2-Befunde 15–17
(gleichrangige Strukturaktionen, doppelter Dirty-Hinweis, Fokus-Scroll) bleiben
im Backlog.

## M1.5-T7-Ergebnis und Abschluss-Re-Audit

Das offene-Transaction-Grid verwendet bei genau einer Karte eine einzelne,
maximal `48rem` breite Grid-Spur. Dadurch entfällt die zuvor sichtbare leere
zweite Spalte; Titel, Kontextmetadaten, technische Details, Fortsetzen- und
Detailaktion bleiben unverändert. Mehrere Karten verwenden weiterhin das
bestehende responsive Raster und behalten Reihenfolge, Inhalte und Links.

Der Abschlusslauf `temp/ui-audit/m1-5-t7/final/manifest.json` ist mit 20/20
Zuständen bei 1280×800 grün. Zustand 10 zeigt den leeren Transactions-
Grundzustand; Zustand 11 zeigt genau eine offene Transaction als ruhige,
harmonisch begrenzte Einzelkarte. Beide Captures wurden visuell geprüft. Der
Transactions-Komponententest ist mit 6/6 grün; der direkte Browser-Smoke für
Refresh und Browsernavigation ist mit 1/1 grün. Build und vollständige
FastTests sind nach Abschluss der parallel laufenden, außerhalb dieses Scopes
liegenden Rollen-Refaktorierung grün; der Solution-Verify blieb mit Score 10.0
und null Verstößen grün. Der separate Reconnect-/Host-Restart-Smoke ließ den
Testhost abstürzen, ohne Bezug zum Transaction-Grid. M1.5 ist mit diesem
begrenzten Re-Audit abgeschlossen.
