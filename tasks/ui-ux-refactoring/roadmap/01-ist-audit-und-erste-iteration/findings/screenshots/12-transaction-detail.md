# 12 – Transaction-Detail

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/12_transaction_detail_desktop_1280x800.png` · Route `/transactions/<TransactionId>` · Detail einer offenen Transaction · Desktop 1280×800.

## Neutrale Beobachtung

- Die Detailroute zeigt einen einzelnen Transaktionskontext statt einer reinen Übersicht.
- Technische Identifikatoren, Statusfelder und Werte sind im zentralen Bereich präsent.
- Inhalt bzw. betroffene Arbeitsobjekte werden nicht mit derselben visuellen Stärke geführt.
- Leere Werte und technische Labels erhalten viel vertikalen Raum.
- Die bestehende Aktion zum Weiterarbeiten, Abschließen oder Zurückkehren ist nicht eindeutig primär.
- Der globale Shell-Kontext bleibt sichtbar.

## Probleme und Schwere

- **P1 – Inhaltspriorität:** Technische Transaction-Daten überlagern die konkrete Aufgabe.
- **P1 – Aktion:** Aktionshierarchie zwischen Weiterarbeiten, Commit und Verwerfen ist nicht sofort lesbar.
- **P2 – IDs:** Technische Identifikatoren sind für die Zielgruppe zu prominent.
- **P2 – Leerwerte:** Der Grund leerer Felder wird nicht im sichtbaren Kontext erklärt.

## Gelungene Aspekte

- Die Detailroute macht den offenen Arbeitskontext nachvollziehbar.
- Technische Werte sind verfügbar und können für Experten erhalten bleiben.
- Die bestehende Transaktionsnavigation ist nicht versteckt.

## Folgerungen ohne Featureausweitung

- Betroffene Arbeitsinhalte im sichtbaren Detailabschnitt vor Diagnosewerten führen.
- Technische IDs progressiv in einem Expertenbereich darstellen.
- Eine vorhandene nächste Arbeitsaktion je sichtbarem Detailabschnitt priorisieren.
- Commit-/Discard-Verträge nur layoutseitig lesbarer machen.

## Abhängigkeiten

Transaction-Detailkomponente, Persistenz-/Commit-/Discard-Vertrag; M1.3-Kandidat.

## Audit-Lücken

Keine.

## M1.3-Re-Audit und Folgeentscheidung

Aktuelle Quelle: `temp/ui-audit/2026-09-20_20-05-26/12_transaction_detail_desktop_1280x800.png`. Der P1-Befund bleibt bestehen: Die bestehende Reihenfolge Validieren → Commit/Verwerfen soll visuell führen; technische IDs und Leerwerte werden sekundär beziehungsweise progressiv angeordnet. [M1.4-T2](../../tasks/M1.4-T2.md) lässt Abschluss-, Dirty-, Transaktions- und Fokusvertrag unverändert.
