# 04 – Node-Detail

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/04_knowledge_node-detail_desktop_1280x800.png` · Route `/knowledge/<NodeId>` · ausgewählte Node im Read-only-Zustand · Desktop 1280×800.

## Neutrale Beobachtung

- Die Ansicht zeigt Shell, Navigation, Baum-/Breadcrumb-Kontext und eine ausgewählte Node.
- Der zentrale Bereich stellt Node-Titel und technische Metadaten vor den eigentlichen bearbeitbaren Inhalt.
- Links zu History und Markdown-Download sind sichtbar.
- WYSIWYG-/Markdown-Moduswahl ist vorhanden.
- Eine direkte, aufgabenorientierte Bearbeitungsaffordance ist im sichtbaren Bereich nicht stark ausgeprägt.
- Der Read-only-/Working-Unterschied wird nicht als klarer Arbeitsauftrag formuliert.

## Probleme und Schwere

- **P1 – Bearbeitung:** Nutzer erkennen nicht schnell, wie sie Text zu dieser Node bearbeiten.
- **P1 – Inhalt:** Der Content erhält weniger visuelles Gewicht als Metadaten und Systemlinks.
- **P2 – Modus:** Die vorhandene Moduswahl erklärt nicht, welche Aufgabe jeder Modus unterstützt.
- **P2 – Sprache:** Node-/Systemzustand steht vor der Support-Aufgabe.

## Gelungene Aspekte

- Breadcrumb, Baum und Titel geben eine belastbare Orientierung.
- History- und Download-Wege bleiben erreichbar.
- Beide vorhandenen Editor-Modi sind sichtbar.
- Der Read-only-Zustand wird nicht durch irreführende Editieraktionen verfälscht.

## Folgerungen ohne Featureausweitung

- Die vorhandene Bearbeitung im Arbeitsabschnitt unmittelbar und verständlich führen.
- Content vor technischen Metadaten platzieren oder Metadaten progressiv offenlegen.
- Moduslabels mit vorhandener Aufgabe statt mit neuer Funktion erklären.
- Read-only/Working nur klarer darstellen, fachlich aber unverändert lassen.

## Abhängigkeiten

Knowledge-Detail-Komponente, Read-only-/Working-Vertrag, Editor- und Dirty-State, Keyboard und M1.2-T2.

## Audit-Lücken

Keine.
