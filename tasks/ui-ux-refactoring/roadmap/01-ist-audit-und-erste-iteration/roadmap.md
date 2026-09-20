# M1 – Ist-Audit und erste visuelle Iteration

## Ziel

Die bestehende Oberfläche wird anhand reproduzierbarer Desktop-Zustände auf Verständlichkeit, Layoutführung und sichtbare Handlungsfähigkeit geprüft. M1 liefert zunächst eine belastbare Ist-Basis und danach kleine, verifizierbare Korrekturen. Es werden keine neuen Fachfunktionen und keine geänderten Fachverträge eingeführt.

## Designlinie

- Support/Consultants erkennen Aufgabe, Inhalt und nächste Aktion ohne technische Vorbildung.
- Deutsche, aufgabenorientierte Begriffe sind sichtbar; technische Details erscheinen progressiv im nativen Expertenbereich.
- Je gleichzeitig sichtbarem Arbeitsabschnitt gibt es eine primäre Aktion; Inhalt und Aufgabe stehen vor Systemzustand.
- Accessibility, Responsive, Dirty-State und Transaktionsverhalten bleiben Verträge. Bestehende native Tastatur-/Fokussemantik darf ohne Zusatzaufwand erhalten bleiben, ist aber kein eigenes Produktziel oder Abnahmekriterium.

## Meilensteine

- [x] **M1.0 Planung** – Zielgruppe, Leitplanken, Evidenz- und Entscheidungsregeln in `planning.md`.
- [x] **M1.1 Ist-Audit**
  - [x] [M1.1-T1 – Ist-Audit anhand der 20 Screenshot-Zustände](tasks/M1.1-T1.md) – Befunde und Synthese abgeschlossen.
- [x] **M1.2 Erste Korrektur**
  - [x] [M1.2-T1 – Audit-Capture-Zustände 03, 09 und 20 stabilisieren](tasks/M1.2-T1.md) – Runner/Test-Zustände sichtbar und semantisch korrekt erfassen; keine Produktlösung vortäuschen.
  - [x] [M1.2-T2 – Wissensarbeitsplatz und Editor im ersten Viewport](tasks/M1.2-T2.md) – bestehende Workflows handlungsfähig machen: Speichern sichtbar, Read-only/Working eindeutig, Metadaten progressiv, Struktur und Content unterscheidbar.
- [x] **M1.3 manueller Re-Audit** – ohne eigenes Leaf; der grüne Lauf bestätigt M1.2 und schließt die Capture-Lücken 03, 09 und 20.
  - Nachweis: `temp/ui-audit/2026-09-20_20-05-26/`, `manifest.json`, 20 Zustände bei 1280×800.
  - Ergebnis: 03 zeigt den bestätigten Root, 09 einen sichtbaren Snapshot-Diff und 20 den fokussierten bestehenden Löschdialog. 15–17 zeigen Strukturaktionen beziehungsweise Dirty/Save im ersten Viewport; die Verträge bleiben unverändert.
- [x] **M1.4 Belegte Folgekorrekturen** – sequenzielle, kleine UI-Slices aus dem Re-Audit; mit dem Re-Audit-Lauf `temp/ui-audit/2026-09-20_21-35-15` abgeschlossen.
  - [x] [M1.4-T1 – Transaktion öffnen und fortsetzen](tasks/M1.4-T1.md) – Zustand 11; bestehende Fortsetzen-Aktion im ersten Viewport geführt, technische Details progressiv.
  - [x] [M1.4-T2 – Transaktionsdetail und Abschlussreihenfolge](tasks/M1.4-T2.md) – Zustand 12; Validierung und Abschluss sind im ersten Viewport klar sequenziert, technische Details progressiv.
  - [x] [M1.4-T3 – Gemeinsame Transaktionsdialogdarstellung](tasks/M1.4-T3.md) – Zustände 13/14.
  - [x] [M1.4-T7 – Moderne Shell-Navigation](tasks/M1.4-T7.md) – vier bestehende Ziele als moderne Linkflächen mit sichtbarem aktivem Zustand; Routen und Responsive-Vertrag unverändert.
  - [x] [M1.4-T4 – Trefferfokus in der Suche](tasks/M1.4-T4.md) – Zustand 07; Filter kompakt sekundär, Trefferzahl und erste Trefferkarte im ersten Viewport.
  - [x] [M1.4-T5 – Wissenszugang auf dem Dashboard führen](tasks/M1.4-T5.md) – Zustand 01; der bestehende Wissensbaum ist primär, Systemstand/Release bleiben sekundär.
  - [x] [M1.4-T6 – Sichere Kontext-Microcopy für Rolle und Fallback](tasks/M1.4-T6.md) – Zustände 02/05; fachliche Bedeutung erklärt, Verträge unverändert.

## M1.5 – Moderne Detail- und Kontextführung

M1.5 setzt die im Re-Audit belegten, rein visuellen Folgekorrekturen in der
angegebenen Reihenfolge um. Der neue Shell-Befund erhält als dringendster
Leaf die stabile ID T0; die bestehenden T1–T7 bleiben unverändert. Jeder Leaf
bleibt auf die vorhandenen Routen, Aktionen, Daten und Verträge beschränkt.
Die letzte Harmonisierung ist wegen der disjunkten Verantwortungen in zwei
Leaves geteilt.

- [x] [M1.5-T0 – Dauerhafter Menübutton für die Shell-Navigation](tasks/M1.5-T0.md) – Desktop offen/schließen/wieder öffnen, kompakter Drawer bleibt erhalten; exakt vier bestehende Ziele.
- [x] [M1.5-T1 – Capture 04 Shell/Header-Präsenz deterministisch absichern](tasks/M1.5-T1.md) – Test-only; der Capture darf Produktzustände weder kaschieren noch erzeugen.
- [x] [M1.5-T2 – ContextSelector als moderne AppDialog-Oberfläche](tasks/M1.5-T2.md) – Zustand 02, Darstellung passend zu 13/14; keine Dialogsemantik ändern.
- [x] [M1.5-T3 – Read-only Knowledge Detail vor History/Download führen](tasks/M1.5-T3.md) – Zustände 04/05; Inhalt/Fallback vor bestehenden sekundären Links, Funktionen unverändert.
- [x] [M1.5-T4 – KnowledgeContextBar klar segmentieren](tasks/M1.5-T4.md) – Snapshot, Bereich und Working-Transaction getrennt lesbar; Verträge und Routen unverändert.
- [x] [M1.5-T5 – History-Auswahl und Vergleichskontext führen](tasks/M1.5-T5.md) – Zustände 08/09; bestehende Ausgang/Ziel-Auswahl sichtbar, IDs progressiv/sekundär.
- [x] [M1.5-T6 – Suchbegriffe Freshness/Findings eindeutschen](tasks/M1.5-T6.md) – sichtbare UI-Begriffe, technische Codes und Suchverträge unverändert; Zustände 06/07 bei 1280×800 visuell bestätigt.
- [ ] [M1.5-T7 – Offene Transactions als Ein-Karten-Grid harmonisieren](tasks/M1.5-T7.md) – keine leere Spalte, bestehende Karten und Aktionen unverändert.

## Nicht freigegebene Kandidaten

Zustand 06 bleibt der belegte Suchgrundzustand; ein Nulltreffer-Produktbefund ist
nicht belegt und erhält keinen eigenen Task. Die P2-Nacharbeiten 15–17
(gleichrangige Strukturaktionen, doppelter Dirty-Hinweis, Fokus-Scroll) bleiben
im Backlog. `RolesPage.razor.cs` und die Rollen-Verwaltung bleiben ein separater
Out-of-scope-Task und sind nicht Teil von M1.5.

Das Verhaltenstor **„Commit vor Validierung“** ist ausdrücklich zurückgestellt:
M1.4 dokumentiert weiterhin die bestehende Reihenfolge Validieren →
Commit/Verwerfen. Eine Umkehrung oder sonstige Änderung des Transaction-
Verhaltens wird in M1.5 nicht implementiert und erst nach einer separaten
fachlichen Entscheidung geplant.

## Abschlusskriterien

M1.1 ist mit den 20 Befunddateien und der Synthese abgeschlossen. M1.2 gilt nach gezieltem Testlauf, visueller Prüfung und dokumentiertem Vergleich als abgeschlossen. M1.3 ist mit dem Lauf `temp/ui-audit/2026-09-20_20-05-26` abgeschlossen. M1.4 ist nach sieben Leaf-Abnahmen und dem begrenzten Re-Audit `temp/ui-audit/2026-09-20_21-35-15` abgeschlossen. M1.5 wird nach den acht Leaf-Abnahmen und einem begrenzten Re-Audit geschlossen.
