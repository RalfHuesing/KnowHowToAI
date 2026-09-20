# 02 – Rollenauswahl

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/02_knowledge_role-selection_desktop_1280x800.png` · Route `/knowledge` · Rollenauswahl-Modal geöffnet · Desktop 1280×800.

## Neutrale Beobachtung

- Der Shell-Kontext bleibt hinter einem fokussierten Modal sichtbar.
- Das Modal verlangt eine Auswahl aus vorhandenen Wissensrollen.
- Die Rolle wird als notwendiger Kontext für den Wissenszugriff präsentiert.
- Auswahl und Bestätigung liegen im Dialog und konkurrieren nicht mit den Shell-Links.
- Der Zweck einer Rolle für die konkrete Wissensarbeit ist nicht prominent erklärt.
- Die Aufnahme zeigt keine Node oder Contentfläche hinter der Auswahl.

## Probleme und Schwere

- **P1 – Zweck:** Für Support/Consultants ist nicht unmittelbar sichtbar, welche Inhalte oder Perspektive die Wahl bestimmt.
- **P1 – Folge:** Die Konsequenz nach Bestätigung bleibt sprachlich schwach.
- **P2 – Begriff:** Rollenbezeichnung und technische Rollenbedeutung werden nicht sauber getrennt.
- **P2 – Dialogführung:** Die primäre Bestätigung ist nicht durch eine kurze aufgabenorientierte Zusammenfassung vorbereitet.

## Gelungene Aspekte

- Der Dialog fokussiert die Auswahl und verhindert konkurrierende Seitenaktionen.
- Der erforderliche Kontext ist sichtbar statt implizit im Hintergrund zu wechseln.
- Der bestehende Rollenvertrag kann mit derselben Auswahl erhalten bleiben.

## Folgerungen ohne Featureausweitung

- Zweck und Folge der vorhandenen Rollenauswahl direkt im Dialog priorisieren.
- Genau eine primäre Aktion je gleichzeitig sichtbarem Dialog-Arbeitsabschnitt führen.
- Technische Rollen-IDs nur ergänzend und progressiv zeigen.
- Keine neue Rollenlogik oder automatische Auswahl einführen.

## Abhängigkeiten

Rollen-/Kontextvertrag, Dialog- und Keyboard-Fokus, Knowledge-Route; M1.3-Re-Audit.

## Audit-Lücken

Keine.

## M1.3-Re-Audit und Folgeentscheidung

Aktuelle Quelle: `temp/ui-audit/2026-09-20_20-05-26/02_knowledge_role-selection_desktop_1280x800.png`. Die bestehende Rollenauswahl bleibt fachlich erforderlich und wird nicht um eine neue Handlung erweitert. Eine Folgearbeit darf nur Zweck und Konsequenz mit fachlich sicherer Microcopy erklären; Rollenlogik, Auswahlvertrag und Fokus bleiben unverändert. Das ist Teil von [M1.4-T6](../../tasks/M1.4-T6.md).

## M1.4-T6-Ergebnis

Die Rollenauswahl erklärt jetzt direkt im bestehenden Dialog, dass die Rolle die
Zielgruppe beziehungsweise Perspektive des Wissensinhalts bestimmt und keine
Zugriffsberechtigung ist. Für die Pflichtauswahl wird außerdem die vorhandene
Folge nach der Auswahl benannt: Die Wissensbasis öffnet sich in dieser
Perspektive. Auswahl, Bestätigung, Rollenquelle und Navigation bleiben
unverändert. Der Capture-Lauf `temp/ui-audit/2026-09-20_21-35-15/` bestätigt
Zustand 02 bei 1280×800; die Erläuterung bleibt innerhalb des nativen Dialogs
und verdrängt keine bestehende Aktion.
