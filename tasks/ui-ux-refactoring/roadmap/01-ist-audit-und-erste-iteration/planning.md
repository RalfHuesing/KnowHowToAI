# Planung M1 – Ist-Audit und erste visuelle Iteration

## Status und Absicht

M1.0 ist abgeschlossen. Die Planung verwendet den reproduzierbaren Lauf `temp/ui-audit/2026-09-20_18-36-16` mit festem Desktop-Viewport 1280×800 als Evidenzquelle; die Bilder und das Manifest bleiben temporär. Die textlichen Beobachtungen sind der versionierte Audit-Nachweis.

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
- **M1.3:** manueller Re-Audit; neue Arbeit wird erst aus den neuen Belegen geschnitten.

## Entscheidungsgrenze

Aktuell blockiert keine Nutzerentscheidung. Normale visuelle Detailentscheidungen (Abstände, Reihenfolge, Beschriftung innerhalb der Leitplanken) trifft die Umsetzung anhand der Evidenz. Eine neue Nutzerentscheidung ist nur erforderlich, wenn Fachbedeutung, destruktives Verhalten oder ein bestehender Vertrag verändert würde.

## Testbudget und Artefakte

M1.1 ist ein Doku-only-Slice: Struktur-/Linkprüfung und `git diff --check`, kein Produkt- oder Testcode. M1.2-T1 nutzt den bestehenden on-demand UiAudit-Runner und normale Skip-/Filterverträge; M1.2-T2 ergänzt nur risikogerechte bestehende Browser-/Komponententests. Auditbilder werden bei Bedarf neu erzeugt, aber nicht committed.
