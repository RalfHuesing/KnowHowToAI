# Planung M1 – Ist-Audit und erste visuelle Iteration

## Status und Absicht

M1.0 ist abgeschlossen. Die Planung verwendet den historischen Lauf `temp/ui-audit/2026-09-20_18-36-16` für die M1.1-Basis und den grünen Re-Audit-Lauf `temp/ui-audit/2026-09-20_20-05-26` mit festem Desktop-Viewport 1280×800 für den aktuellen Stand; Bilder und Manifeste bleiben temporär. Die textlichen Beobachtungen und Re-Audit-Nachweise sind der versionierte Audit-Nachweis.

Zielgruppe sind Support-Mitarbeitende und Consultants. Die Oberfläche soll clean, modern und einfach wirken, ohne bestehende Funktionen, Fachverträge oder technische Backendbegriffe umzudeuten. Der Weg ist bewusst vertikal und klein: erst Evidenz, dann ein eng begrenzter Korrekturslice, danach manueller Re-Audit.

## Verbindliche Design- und Prüfregeln

1. Sichtbare deutsche Begriffe beschreiben Aufgabe und Ergebnis; technische Begriffe, IDs und Versionen werden progressiv im nativen aufklappbaren Expertenbereich gezeigt.
2. Je gleichzeitig sichtbarem Arbeitsabschnitt wird genau eine primäre Aktion visuell geführt.
3. Inhalt und nächste Aufgabe stehen vor Diagnose- und Systemzustand.
4. Accessibility, Responsive-Verhalten, Dirty-State und Transaktionsverträge dürfen nicht regressieren. Bestehende native Tastatur-/Fokussemantik darf ohne Zusatzaufwand erhalten bleiben, ist aber kein eigenes Produktziel, Abnahmekriterium oder Testbudget.
5. Ein Befund ist neutral belegt, nach Schwere priorisiert und führt nur zu einer Folgerung innerhalb des bestehenden Funktionsumfangs.
6. Ein nicht sichtbarer oder semantisch falscher Capture-Zustand wird als Audit-Lücke markiert; er darf nicht durch eine Produktannahme ersetzt werden.

## Reihenfolge

- **M1.1:** 20 Zustände erfassen, einzeln beschreiben und in priorisierten Clustern synthetisieren.
- **M1.2-T1:** ausschließlich Runner-/Test-Vorbedingungen und Assertions für die Audit-Zustände 03, 09 und 20 präzisieren. Keine Produktlösung vortäuschen.
- **M1.2-T2:** den bestehenden Wissensarbeitsplatz und Editor so ordnen, dass der erste Viewport die bestehende Aufgabe, den Modus und das Speichern verständlich führt.
- **M1.3:** manueller Re-Audit mit Manifest und 20 Captures. Der Lauf ist abgeschlossen: 03 bestätigt den Root, 09 den Snapshot-Diff und 20 den bestehenden Löschdialog. M1.2 ist damit abgenommen.
- **M1.4:** zuerst die Transaktionszustände 11, 12 und 13/14; danach der neue Navigation-Leaf M1.4-T7; anschließend Suche 07, Dashboard 01 und die fachlich sichere Microcopy für 02/05. Die bereits vergebenen IDs T4–T6 bleiben stabil, werden aber erst nach T7 ausgeführt. Jeder Leaf bleibt auf vorhandene Ziele, Aktionen und Verträge beschränkt.
- **M1.5:** der Re-Audit `temp/ui-audit/2026-09-20_21-35-15` ist die Evidenzbasis. Der bestätigte Shell-Codebefund erhält als dringenden ersten Leaf T0: dauerhafter Menübutton, Desktop schließen/wieder öffnen und kompakter Drawer ohne Funktionsverlust. Danach wird die Capture-Präsenz von Shell/Header in Zustand 04 ausschließlich im Test deterministisch abgesichert; es folgen ContextSelector 02, Knowledge Detail 04/05, die segmentierte KnowledgeContextBar und History 08/09. Die unabhängige Schluss-Harmonisierung ist in Suchbegriffe (T6) und Transaction-Grid (T7) geteilt. Die Reihenfolge ist verbindlich; Routen, Daten, Aktionen und Verträge bleiben unverändert.

## M1.4-Entscheidungsgrenzen

- Verbindlich: Zustand 11 führt die bestehende Öffnen-/Fortsetzen-Aktion, Zustand 12 die bestehende Reihenfolge Validieren → Commit/Verwerfen; technische IDs und Leerwerte werden nur sekundär beziehungsweise progressiv angeordnet.
- Verbindlich: Zustände 13/14 erhalten eine gemeinsame moderne Dialogdarstellung. Commit-/Discard-Semantik, Abbruchweg und Transaktionsverträge bleiben unverändert; bestehende native Fokussemantik darf ohne Zusatzaufwand erhalten bleiben, ist aber keine eigene Abnahme.
- Verbindlich: Suche 07 zeigt Trefferzahl und die vollständige erste vorhandene Trefferkarte im 1280×800-Viewport; Filter werden kompakter/sekundär. Suchsemantik, Ranking und Trefferreihenfolge ändern sich nicht. Zustand 06 wird nicht als Nulltreffer-Beleg erweitert.
- Verbindlich: Dashboard 01 führt den vorhandenen Wissenszugang als primären Einstieg, Diagnose/Snapshot bleiben sekundär. Zustände 02/05 erhalten ausschließlich fachlich sichere Microcopy; keine neue Handlung, Rollenlogik oder Fallback-Funktion.
- Verbindlich: M1.4-T7 ordnet ausschließlich die bestehenden Shell-Ziele in einer modernen, cleanen visuellen Navigation mit erkennbarem aktivem Zustand. Hover-/Focus-Zustände sind normale CSS-Zustände; es gibt keine neue Route, kein neues Feature und keine Tastatur-Abnahme.
- Nicht freigegeben: Änderungen an `RolesPage.razor.cs`, neue Aktionen, neue Dialogverträge, neue Such-/Fallback-/Rollenlogik oder P2-Nacharbeiten 15–17.
- Nicht freigegeben: Produktänderungen zur Capture-Korrektur in Zustand 04; der Capture-Leaf ist test-only und darf fehlende Präsenz nicht durch Scroll-, Overlay- oder DOM-Manipulation kaschieren.
- Nicht freigegeben: eine Änderung der Transaction-Reihenfolge. Das mögliche Verhalten „Commit vor Validierung“ ist ein zurückgestelltes Entscheidungsgate; bis zu einer separaten fachlichen Entscheidung bleibt Validieren → Commit/Verwerfen unverändert und wird in M1.5 nicht implementiert.
- Nicht freigegeben: `RolesPage.razor.cs` und Rollen-Verwaltung; sie bleiben ein separater Out-of-scope-Task. Die P2-Befunde 15–17 bleiben im Backlog.
- Neu zu bewerten: konkrete Abstände, Reihenfolge innerhalb des bestehenden Arbeitsabschnitts und Formulierung, sofern Fachbedeutung und Verträge unverändert bleiben. Bei einer nötigen Vertragsänderung stoppt der Ausführer und eskaliert.

## M1.5-Entscheidungsgrenzen

- **Verbindlich:** Der Menübutton steht in der App-Leiste oben links, ist in allen Breiten sichtbar, verwendet ein gängiges Drei-Linien-Symbol und einen zugänglichen Namen mit dem aktuellen Zustand. Desktop startet mit geöffneter Sidebar; Schließen gibt dem Hauptinhalt den frei gewordenen Platz, und derselbe Button öffnet wieder.
- **Verbindlich:** In kompakter Breite bleibt die Navigation ein Drawer/Overlay. Die vier bestehenden Ziele Start (`/`), Suche (`/search`), Transactions (`/transactions`) und Rollen (`/roles`) bleiben exakt erhalten.
- **Verbindlich:** Der redundante interne Navigationstitel, die Beschreibung „Arbeitsbereiche“ und der Textbutton „Navigation schließen“ werden entfernt oder auf eine nicht redundante kompakte Steuerung reduziert. Die bestehende native Bediensemantik darf ohne Zusatzaufwand erhalten bleiben; Tastatur ist kein eigenes Produktziel.
- **Verbindlich:** Red-Test-first: Zuerst schlagen Browser-/Layouttests für Desktop Schließen und anschließendes Wiederöffnen fehl; danach wird die minimale Shell-/Navigation-Änderung umgesetzt und die responsive Regression geprüft.
- **Nicht freigegeben:** neue Routen, Ziele, Navigationslogik, Rollen-/Berechtigungsbedeutung, Overlay-Verträge oder eigene Keyboard-Abnahme.

## Entscheidungsgrenze

Aktuell blockiert keine Nutzerentscheidung. Normale visuelle Detailentscheidungen (Abstände, Reihenfolge, Beschriftung innerhalb der Leitplanken) trifft die Umsetzung anhand der Evidenz. Eine neue Nutzerentscheidung ist nur erforderlich, wenn Fachbedeutung, destruktives Verhalten oder ein bestehender Vertrag verändert würde.

## Testbudget und Artefakte

M1.1 und dieses Planungsupdate sind Doku-only-Slices: Struktur-/Linkprüfung und `git diff --check`, kein Produkt- oder Testcode. M1.2-T1 nutzt den bestehenden on-demand UiAudit-Runner und normale Skip-/Filterverträge; M1.2-T2 ergänzt nur risikogerechte bestehende Browser-/Komponententests. M1.4 nutzt je Leaf den kleinsten betroffenen Komponententest-/Browserfilter plus den passenden 1280×800-Capture; kein vollständiger visueller Umbau, keine eigenen Keyboard-Tests und kein ungezielter Volltest als Leaf-Voraussetzung. Bestehende native Tastatur-/Fokussemantik darf ohne Zusatzaufwand erhalten bleiben, blockiert aber keinen Slice. Auditbilder werden bei Bedarf neu erzeugt, aber nicht committed.

Für M1.5 gilt dasselbe kleinste Testbudget: T0 beginnt mit einem gezielt roten
Desktop-Browser-/Layouttest für Schließen und Wiederöffnen und prüft danach die
kompakte Responsive-Regression; eigene Keyboard-Tests sind nicht erforderlich.
T1 ergänzt nur deterministische
Browser-/UiAudit-Wartebedingungen und Assertions; keine Produkt- oder CSS-
Änderung im Capture-Leaf. T2–T7 verwenden je Leaf den kleinsten betroffenen
Web-Komponenten-/Browserfilter und den jeweils genannten 1280×800-Zustand;
ein vollständiger visueller Umbau, eigene Keyboard-Tests und ein ungezielter
Volltest sind nicht erforderlich. Auditbilder und Manifeste bleiben temporär.
