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

## M1.3-Re-Audit und Folgeentscheidung

Aktuelle Quelle: `temp/ui-audit/2026-09-20_20-05-26/05_knowledge_fallback-detail_desktop_1280x800.png`. Der Fallback bleibt ein fachlicher Inhalts-/Verfügbarkeitskontext, nicht der Anlass für eine neue Aktion oder Quelle. M1.4-T6 darf nur die vorhandene Bedeutung und den nächsten bekannten Kontextschritt sicherer formulieren; Fallback-Semantik, Datenquelle und technische Details bleiben unverändert.

## M1.4-T6-Ergebnis

Der bestehende Detailrahmen zeigt bei verwendeten Fallback-Inhalten jetzt eine
kurze Einordnung vor dem Inhalt: Für die angefragte Rolle ist an diesem Knoten
kein eigener Inhalt hinterlegt; angezeigt wird der vorhandene Inhalt der
aufgelösten Fallback-Rolle. Die angefragte Rolle und der Wissenskontext bleiben
dabei unverändert. Änderungshistorie, Download und technische Details bleiben
als bestehende Wege beziehungsweise progressive Diagnose erhalten. Der
Capture-Lauf `temp/ui-audit/2026-09-20_21-35-15/` bestätigt Zustand 05 bei
1280×800; die Microcopy ist sichtbar, ohne neue Quelle, Aktion oder
Fallback-Semantik einzuführen.

Der gleiche Re-Audit-Lauf bestätigt den bestehenden Shell-/Header- und
Detailrahmen. M1.5-T3 führt den vorhandenen Fallback-/Inhaltskontext vor den
sekundären History-/Download-Links; Linkziele, Datenquelle und Fallback-
Semantik bleiben unverändert.

## M1.5-T3-Ergebnis

Der UiAudit-Lauf `temp/ui-audit/m1-5-t3/2026-09-20_22-45-00/` bestätigt Zustand
05 bei 1280×800. Die bestehende fachliche Einordnung des fehlenden eigenen
Inhalts und der aufgelösten Fallback-Rolle steht unmittelbar vor dem angezeigten
Inhalt; danach folgt die harmonische sekundäre Aktionsgruppe mit denselben
History-/Download-Zielen. Die technischen Rollen-, Verfügbarkeits- und
Revisionsdetails bleiben unter „Technische Details“ progressiv zugänglich.
Es wurde keine neue Fallback-Aktion oder Inhaltsquelle eingeführt.
