# 15 – Working Knowledge: Struktur

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/15_working-knowledge-structure_desktop_1280x800.png` · Route `/knowledge/<NodeId>` · Working Transaction, Struktur-/Node-Kontext · Desktop 1280×800.

## Neutrale Beobachtung

- Der Header benennt Wissensbasis, Transaction und Working Transaction.
- Links ist die Navigation geöffnet; daneben zeigt ein Baum nur den Root/den ausgewählten Wissensbereich.
- Der zentrale Node-Titel „Browser-Testwissen“ und die Verfügbarkeit „Eigener Inhalt“ sind sichtbar.
- Metadaten wie Rolle, Aktualität, Revision, Inhaltsmodus, Position und Node-ID stehen vor dem Editor.
- History-Link und Markdown-Download sind sichtbar.
- WYSIWYG/Markdown-Quelle sind als Modusauswahl vorhanden.
- Strukturaktionen und der eigentliche Editorinhalt liegen teilweise unterhalb des 1280×800-Viewport.
- Leere technische Werte erzeugen zusätzlichen vertikalen Raum, ohne die Arbeitsaufgabe zu erklären.

## Probleme und Schwere

- **P1 – Above the fold:** Strukturpflege ist im ersten Viewport nicht handlungsfähig; Aktionen liegen below fold.
- **P1 – Raum:** Metadaten und Links verdrängen die Struktur-/Content-Aufgabe.
- **P1 – Trennung:** Strukturarbeit und Textarbeit erscheinen als ein langer, nicht klar getrennter Arbeitsabschnitt.
- **P2 – Leere Werte:** Revision und Node-ID bleiben prominent, obwohl kein sichtbarer Wert zur Aufgabe beiträgt.
- **P2 – Fokus:** WYSIWYG/Markdown ist erkennbar, aber die nächste Arbeitsaktion nicht.

## Gelungene Aspekte

- Working-Kontext ist im Header eindeutig.
- Baum, Breadcrumb/Node-Titel und eigener Inhalt geben solide Orientierung.
- Editor-Modusgruppe ist sichtbar und verständlich benannt.
- Technische Informationen sind vollständig vorhanden und damit grundsätzlich für Experten bewahrbar.

## Folgerungen ohne Featureausweitung

- Bestehende Strukturaktion im ersten sichtbaren Arbeitsabschnitt führen.
- Struktur und Content räumlich/typografisch als zwei aufgabenbezogene Abschnitte ordnen.
- Technische Metadaten in einen nativen Expertenbereich verschieben oder kompakt sekundär machen.
- Eine primäre bestehende Aktion je gleichzeitig sichtbarem Arbeitsabschnitt führen.
- Keine neue Struktur- oder Editorfunktion aus dem Layoutbefund ableiten.

## Abhängigkeiten

M1.2-T2, Working-/Dirty-/Transaction-Verträge, Responsive- und Keyboard-Reihenfolge, bestehende Node-Struktur.

## Audit-Lücken

Keine.
