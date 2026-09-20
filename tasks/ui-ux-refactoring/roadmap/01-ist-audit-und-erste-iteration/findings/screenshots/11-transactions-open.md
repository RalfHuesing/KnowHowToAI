# 11 – Offene Transaction

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/11_transactions_open_desktop_1280x800.png` · Route `/transactions` · offene Working Transaction · Desktop 1280×800.

## Neutrale Beobachtung

- Eine offene Working Transaction ist im Hauptbereich erkennbar.
- Status, technische Werte und Aktionsmöglichkeiten stehen gleichzeitig im sichtbaren Bereich.
- Einige Wertfelder erscheinen leer oder ohne unmittelbare Erläuterung.
- Die Ansicht verwendet mehrere englisch/technisch geprägte Transaction-Begriffe.
- Der nächste Schritt nach dem Öffnen ist nicht als zusammenhängender Arbeitsauftrag formuliert.
- Shell-Navigation und Transaktionsinhalt konkurrieren um die verfügbare Breite.

## Probleme und Schwere

- **P1 – Kontext:** „Offen“ und „Working“ erklären nicht allein, was Support jetzt tun soll.
- **P1 – Hierarchie:** Bestehende Aktionen wirken gleichrangig, statt eine Fortsetzung der Arbeit zu führen.
- **P2 – Leere Felder:** Unklare Leerwerte erzeugen Diagnosebedarf vor der eigentlichen Aufgabe.
- **P2 – Terminologie:** Transaction-Lebenszyklus ist stärker sichtbar als der zu bearbeitende Inhalt.

## Gelungene Aspekte

- Der offene Zustand ist sichtbar und nicht mit einer abgeschlossenen Arbeit verwechselt.
- Die bestehenden Arbeitsaktionen sind erreichbar.
- Die Ansicht kann als Ausgangspunkt für Commit-/Discard-Zustände dienen.

## Folgerungen ohne Featureausweitung

- Working-Kontext mit einer kurzen vorhandenen Aufgabenbeschreibung rahmen.
- Eine bestehende primäre Aktion im sichtbaren Arbeitsabschnitt führen.
- Leere Werte sekundär und verständlich beschriften.
- Commit-/Discard-Semantik unverändert lassen.

## Abhängigkeiten

Working-Transaction, Dirty-State, Commit-/Discard-Dialoge und Keyboard-Fokus; M1.3.

## Audit-Lücken

Keine.
