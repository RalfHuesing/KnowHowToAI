# 02 – Zielgruppenauswahl

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/02_knowledge_audience-selection_desktop_1280x800.png` · Route `/knowledge` · Zielgruppenauswahl-Modal geöffnet · Desktop 1280×800.

## Neutrale Beobachtung

- Der Shell-Kontext bleibt hinter einem fokussierten Modal sichtbar.
- Das Modal verlangt eine Auswahl aus vorhandenen Wissenszielgruppen.
- Die Zielgruppe wird als notwendiger Kontext für den Wissenszugriff präsentiert.
- Auswahl und Bestätigung liegen im Dialog und konkurrieren nicht mit den Shell-Links.
- Der Zweck einer Zielgruppe für die konkrete Wissensarbeit ist nicht prominent erklärt.
- Die Aufnahme zeigt keine Node oder Contentfläche hinter der Auswahl.

## Probleme und Schwere

- **P1 – Zweck:** Für Support/Consultants ist nicht unmittelbar sichtbar, welche Inhalte oder Perspektive die Wahl bestimmt.
- **P1 – Folge:** Die Konsequenz nach Bestätigung bleibt sprachlich schwach.
- **P2 – Begriff:** Zielgruppenbezeichnung und technische Zielgruppenbedeutung werden nicht sauber getrennt.
- **P2 – Dialogführung:** Die primäre Bestätigung ist nicht durch eine kurze aufgabenorientierte Zusammenfassung vorbereitet.

## Gelungene Aspekte

- Der Dialog fokussiert die Auswahl und verhindert konkurrierende Seitenaktionen.
- Der erforderliche Kontext ist sichtbar statt implizit im Hintergrund zu wechseln.
- Der bestehende Zielgruppenvertrag kann mit derselben Auswahl erhalten bleiben.

## Folgerungen ohne Featureausweitung

- Zweck und Folge der vorhandenen Zielgruppenauswahl direkt im Dialog priorisieren.
- Genau eine primäre Aktion je gleichzeitig sichtbarem Dialog-Arbeitsabschnitt führen.
- Technische Zielgruppen-IDs nur ergänzend und progressiv zeigen.
- Keine neue Zielgruppenlogik oder automatische Auswahl einführen.

## Abhängigkeiten

Zielgruppen-/Kontextvertrag, Dialog- und Keyboard-Fokus, Knowledge-Route; M1.3-Re-Audit.

## Audit-Lücken

Keine.

## M1.3-Re-Audit und Folgeentscheidung

Aktuelle Quelle: `temp/ui-audit/2026-09-20_20-05-26/02_knowledge_audience-selection_desktop_1280x800.png`. Die bestehende Zielgruppenauswahl bleibt fachlich erforderlich und wird nicht um eine neue Handlung erweitert. Eine Folgearbeit darf nur Zweck und Konsequenz mit fachlich sicherer Microcopy erklären; Zielgruppenlogik, Auswahlvertrag und Fokus bleiben unverändert. Das ist Teil von [M1.4-T6](../../tasks/M1.4-T6.md).

## M1.4-T6-Ergebnis

Die Zielgruppenauswahl erklärt jetzt direkt im bestehenden Dialog, dass die Zielgruppe die
Zielgruppe beziehungsweise Perspektive des Wissensinhalts bestimmt und keine
Zugriffsberechtigung ist. Für die Pflichtauswahl wird außerdem die vorhandene
Folge nach der Auswahl benannt: Die Wissensbasis öffnet sich in dieser
Perspektive. Auswahl, Bestätigung, Zielgruppenquelle und Navigation bleiben
unverändert. Der Capture-Lauf `temp/ui-audit/2026-09-20_21-35-15/` bestätigt
Zustand 02 bei 1280×800; die Erläuterung bleibt innerhalb des nativen Dialogs
und verdrängt keine bestehende Aktion.

## M1.5-T2-Ergebnis

Der ContextSelector verwendet jetzt die gemeinsame `AppDialog`-Oberfläche:
weiße Surface, feiner Rand, abgerundeter Radius, ruhiger Overlay-Schatten und
ein gedämpfter Backdrop. Breite und Innenabstände entsprechen damit den
Commit-/Verwerfen-Dialogen 13/14. Auswahl, Bestätigung, Abbruch, Zielgruppenauflösung
und der native Dialog-Lifecycle bleiben unverändert. Der Nachweis liegt im
Capture `temp/ui-audit/m1-5-t2/2026-09-20_22-36-23/02_knowledge_audience-selection_desktop_1280x800.png`.
