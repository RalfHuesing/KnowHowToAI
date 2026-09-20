# Planung M1 – Ist-Audit und erste visuelle Iteration

## Status und Absicht

M1.0 ist abgeschlossen. Die Planung verwendet den historischen Lauf `temp/ui-audit/2026-09-20_18-36-16` für die M1.1-Basis und den grünen Re-Audit-Lauf `temp/ui-audit/2026-09-20_20-05-26` mit festem Desktop-Viewport 1280×800 für den aktuellen Stand; Bilder und Manifeste bleiben temporär. Die textlichen Beobachtungen und Re-Audit-Nachweise sind der versionierte Audit-Nachweis.

Zielgruppe sind Support-Mitarbeitende und Consultants. Die Oberfläche soll clean, modern und einfach wirken, ohne bestehende Funktionen, Fachverträge oder technische Backendbegriffe umzudeuten. Der Weg ist bewusst vertikal und klein: erst Evidenz, dann ein eng begrenzter Korrekturslice, danach manueller Re-Audit.

## Verbindliche Design- und Prüfregeln

1. Sichtbare deutsche Begriffe beschreiben Aufgabe und Ergebnis; technische Begriffe, IDs und Versionen werden progressiv im nativen aufklappbaren Expertenbereich gezeigt.
2. Je gleichzeitig sichtbarem Arbeitsabschnitt wird genau eine primäre Aktion visuell geführt.
3. Inhalt und nächste Aufgabe stehen vor Diagnose- und Systemzustand.
4. Accessibility, Tastatur, Responsive-Verhalten, Dirty-State und Transaktionsverträge dürfen nicht regressieren.
5. Ein Befund ist neutral belegt, nach Schwere priorisiert und führt nur zu einer Folgerung innerhalb des bestehenden Funktionsumfangs.
6. Ein nicht sichtbarer oder semantisch falscher Capture-Zustand wird als Audit-Lücke markiert; er darf nicht durch eine Produktannahme ersetzt werden.

## Reihenfolge

- **M1.1:** 20 Zustände erfassen, einzeln beschreiben und in priorisierten Clustern synthetisieren.
- **M1.2-T1:** ausschließlich Runner-/Test-Vorbedingungen und Assertions für die Audit-Zustände 03, 09 und 20 präzisieren. Keine Produktlösung vortäuschen.
- **M1.2-T2:** den bestehenden Wissensarbeitsplatz und Editor so ordnen, dass der erste Viewport die bestehende Aufgabe, den Modus und das Speichern verständlich führt.
- **M1.3:** manueller Re-Audit mit Manifest und 20 Captures. Der Lauf ist abgeschlossen: 03 bestätigt den Root, 09 den Snapshot-Diff und 20 den bestehenden Löschdialog. M1.2 ist damit abgenommen.
- **M1.4:** zuerst die Transaktionszustände 11, 12 und 13/14; danach Suche 07; anschließend Dashboard 01 und die fachlich sichere Microcopy für 02/05. Jeder Leaf bleibt auf vorhandene Aktionen und Verträge beschränkt.

## M1.4-Entscheidungsgrenzen

- Verbindlich: Zustand 11 führt die bestehende Öffnen-/Fortsetzen-Aktion, Zustand 12 die bestehende Reihenfolge Validieren → Commit/Verwerfen; technische IDs und Leerwerte werden nur sekundär beziehungsweise progressiv angeordnet.
- Verbindlich: Zustände 13/14 erhalten eine gemeinsame moderne Dialogdarstellung. Commit-/Discard-Semantik, Keyboard-Fokus, Abbruchweg und Transaktionsverträge bleiben unverändert.
- Verbindlich: Suche 07 zeigt Trefferzahl und die vollständige erste vorhandene Trefferkarte im 1280×800-Viewport; Filter werden kompakter/sekundär. Suchsemantik, Ranking und Trefferreihenfolge ändern sich nicht. Zustand 06 wird nicht als Nulltreffer-Beleg erweitert.
- Verbindlich: Dashboard 01 führt den vorhandenen Wissenszugang als primären Einstieg, Diagnose/Snapshot bleiben sekundär. Zustände 02/05 erhalten ausschließlich fachlich sichere Microcopy; keine neue Handlung, Rollenlogik oder Fallback-Funktion.
- Nicht freigegeben: Änderungen an `RolesPage.razor.cs`, neue Aktionen, neue Dialogverträge, neue Such-/Fallback-/Rollenlogik oder P2-Nacharbeiten 15–17.
- Neu zu bewerten: konkrete Abstände, Reihenfolge innerhalb des bestehenden Arbeitsabschnitts und Formulierung, sofern Fachbedeutung und Verträge unverändert bleiben. Bei einer nötigen Vertragsänderung stoppt der Ausführer und eskaliert.

## Entscheidungsgrenze

Aktuell blockiert keine Nutzerentscheidung. Normale visuelle Detailentscheidungen (Abstände, Reihenfolge, Beschriftung innerhalb der Leitplanken) trifft die Umsetzung anhand der Evidenz. Eine neue Nutzerentscheidung ist nur erforderlich, wenn Fachbedeutung, destruktives Verhalten oder ein bestehender Vertrag verändert würde.

## Testbudget und Artefakte

M1.1 und dieses Planungsupdate sind Doku-only-Slices: Struktur-/Linkprüfung und `git diff --check`, kein Produkt- oder Testcode. M1.2-T1 nutzt den bestehenden on-demand UiAudit-Runner und normale Skip-/Filterverträge; M1.2-T2 ergänzt nur risikogerechte bestehende Browser-/Komponententests. M1.4 nutzt je Leaf den kleinsten betroffenen Komponententest-/Browserfilter plus den passenden 1280×800-Capture; kein vollständiger visueller Umbau und kein ungezielter Volltest als Leaf-Voraussetzung. Auditbilder werden bei Bedarf neu erzeugt, aber nicht committed.
