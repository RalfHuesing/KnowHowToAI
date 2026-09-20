# 07 – Suchtreffer

## Quelle und Zustand

`temp/ui-audit/2026-09-20_18-36-16/07_search_results_desktop_1280x800.png` · Route `/search` · Trefferliste mit gesetztem Filter · Desktop 1280×800.

## Neutrale Beobachtung

- Die gesetzten Suchparameter und Filter bleiben sichtbar.
- Eine Trefferliste ist vorhanden, wird aber räumlich unter bzw. neben der Filterfläche geführt.
- Treffertexte und Auswahlziel konkurrieren mit der Filterdarstellung.
- Der nächste Schritt – einen Treffer öffnen – wird nicht als primäre sichtbare Handlung geführt.
- Die Shell-Navigation bleibt dauerhaft präsent und reduziert den zentralen Arbeitsraum.
- Es ist kein Detailcontent geöffnet.

## Probleme und Schwere

- **P1 – Ergebnisfokus:** Die eigentlichen Treffer stehen nicht klar vor den Filtern.
- **P1 – Aufgabe:** Relevanz und Auswahlhandlung sind visuell schwach.
- **P2 – Dichte:** Filterdetails verbrauchen Raum, obwohl die Suche bereits ein Ergebnis liefert.
- **P2 – Hierarchie:** Mehrere gleichartige Links/Zeilen erschweren die erste Auswahl.

## Gelungene Aspekte

- Ergebnisse und aktiver Filterzustand sind gleichzeitig nachvollziehbar.
- Treffer bleiben erreichbar und die Suchroute ist stabil.
- Der Zustand eignet sich für einen reproduzierbaren Vergleich mit dem leeren Suchzustand.

## Folgerungen ohne Featureausweitung

- Treffer als primären Arbeitsabschnitt vor Filterdetails führen.
- Filter sichtbar, aber kompakter bzw. sekundär gruppieren.
- Den vorhandenen Treffer-Link als einzige primäre Aktion des Ergebnisabschnitts hervorheben.
- Suchsemantik und Trefferreihenfolge unverändert lassen.

## Abhängigkeiten

Search-/Navigationsvertrag, Responsive-/Keyboard-Verhalten; M1.3-Kandidat.

## Audit-Lücken

Keine.

## M1.3-Re-Audit und Folgeentscheidung

Aktuelle Quelle: `temp/ui-audit/2026-09-20_20-05-26/07_search_results_desktop_1280x800.png`. Der Re-Audit bestätigt den P1-Ergebnisfokus-Befund. [M1.4-T4](../../tasks/M1.4-T4.md) soll die vorhandene Trefferzahl und die vollständige erste Trefferkarte im 1280×800-Viewport führen und Filter kompakter/sekundär machen. Suchsemantik, Ranking, Trefferreihenfolge, Route und bestehende Trefferaktion bleiben unverändert.

## M1.4-T4-Ergebnis

Die Filterfläche nutzt die vorhandene semantische Gruppierung jetzt in einem
kompakten Desktop-2-Spaltenraster mit ruhiger Secondary-Surface und kleineren
Abständen. Suchfeld und Trefferkarte verwenden die globalen Button-/Design-
Tokens; Suchsemantik, Ranking, Trefferreihenfolge, Route und Trefferaktion
blieben unverändert. Der UiAudit-Lauf `temp/ui-audit/2026-09-20_21-18-44`
bestätigt bei 1280×800 die sichtbare Trefferzahl und die vollständig sichtbare
erste Karte. Die Browser-Smoke- und Web-Komponententests bestätigen weiterhin
Filterzugriff und Navigation.

## M1.5-T6-Re-Audit

Der UiAudit-Lauf `temp/ui-audit/m1-5-t6/2026-09-20_23-25-32/07_search_results_desktop_1280x800.png`
bestätigt bei 1280×800 die sichtbaren deutschen Gruppenbezeichnungen
„Aktualität“ und „Befunde“ auch im Trefferzustand. Trefferzahl, Ranking,
Reihenfolge, Suchsemantik, Route und Trefferaktion bleiben unverändert.
