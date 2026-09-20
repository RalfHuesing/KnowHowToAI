# 05 – Fallback-Detail

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/05_knowledge_fallback-detail_desktop_1280x800.png` · Route `/knowledge/<NodeId>` · Fallback-/nicht lokaler Inhaltszustand · Desktop 1280×800.

## Neutrale Beobachtung

- Die Ansicht verwendet denselben Shell- und Detailrahmen wie die Node-Ansicht.
- Ein Fallback-/Verfügbarkeitskontext ist sichtbar, aber nicht als klare nächste Arbeitsentscheidung formuliert.
- Technische Metadaten und Systemlinks stehen oberhalb bzw. neben dem Contentbereich.
- Der eigentliche Inhalt bzw. sein Fehlen erhält wenig Fläche und Erklärung.
- Die Bearbeitungsaffordance bleibt im sichtbaren Bereich schwach.
- Der Breadcrumb-/Node-Kontext bleibt grundsätzlich erhalten.

## Probleme und Schwere

- **P1 – Einordnung:** Ohne Fachwissen ist nicht klar, ob Fallback lesbar, bearbeitbar oder nur diagnostisch ist.
- **P1 – Aufgabe:** Die nächste Support-Aktion nach dem Fallback-Zustand ist nicht sichtbar.
- **P2 – Content:** Zu wenig Gewicht liegt auf dem vorhandenen oder fehlenden Text.
- **P2 – Sprache:** Technischer Fallback-Zustand wirkt wie ein internes Systemdetail.

## Gelungene Aspekte

- Fallback wird sichtbar gemacht und nicht still als normale lokale Node ausgegeben.
- Node-Kontext und bestehende Navigationswege bleiben erhalten.
- Die Aufnahme kann mit dem normalen Detailzustand verglichen werden.

## Folgerungen ohne Featureausweitung

- Vorhandene Fallback-Bedeutung im Content-Arbeitsabschnitt verständlicher gruppieren.
- Lesbarkeit und nächste bestehende Aktion vor technischen Werten führen.
- Technische Verfügbarkeitsdetails progressiv anzeigen, nicht entfernen.
- Keine neue Fallback-Funktion oder neue Inhaltsquelle ableiten.

## Abhängigkeiten

Inhaltsmodus-/Fallback-Vertrag, Knowledge-Detail und M1.2-T2; späterer manueller Re-Audit.

## Audit-Lücken

Keine.
