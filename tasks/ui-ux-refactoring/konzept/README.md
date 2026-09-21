# Bedienkonzept UI/UX-Refactoring

**Version:** 1.0 · **Stand:** 2026-09-20 · **Status:** verbindliches Konzept für das Vorhaben

Dieses Verzeichnis ist die verbindliche Ziel- und Entscheidungsgrundlage für das UI/UX-Refactoring. Es beschreibt weder einen bereits implementierten Produktstand noch neue Fachfunktionen. Der belegte Ist-Zustand bleibt in [`docs/`](../../../docs/README.md); die historische Umsetzungsgeschichte bleibt in [M1](../roadmap/01-ist-audit-und-erste-iteration/roadmap.md) und der Git-Historie.

## Verbindlichkeit und Vorrang

- Die Zielgruppe sind Support-Mitarbeitende und Consultants. Die Oberfläche soll modern-clean, harmonisch und aufgabenorientiert wirken, ohne bestehende Funktionen, Routen, Fachverträge oder technische Daten zu verlieren.
- Aussagen mit **Verbindlich** sind Invarianten für Konzept- und Umsetzungsentscheidungen. **Empfohlen** bezeichnet eine bevorzugte Ausprägung; **Offen** verweist auf das Entscheidungsregister.
- Dieses Konzept ist nachgeordnet zu den Ist-Dokumenten unter [`docs/`](../../../docs/README.md) und den allgemeinen Regeln in [`Richtlinien.mdc`](../../../.agents/rules/Richtlinien.mdc). Bei Widerspruch stoppt die Umsetzung.
- Für UI-Basis, Zielstruktur und Webgrenzen gelten zusätzlich [`tasks/webfrontend/konzept/02-bedienkonzept-und-ui.md`](../../webfrontend/konzept/02-bedienkonzept-und-ui.md) und [`08-projektstruktur-und-codekonventionen.md`](../../webfrontend/konzept/08-projektstruktur-und-codekonventionen.md).

## Lesen nach Zweck

| Zweck | Primärdokumente |
|---|---|
| Einstieg, Geltungsbereich, Status | dieses Dokument, [Findings und Evidenz](05-findings-und-evidenz.md) |
| Bedienmodell und mentale Modelle | [Bedienmodell](01-bedienmodell-und-mentale-modelle.md) |
| Workflow, Zustände, Fehler- und Dirty-Pfade | [Nutzerreisen](02-nutzerreisen-und-zustandsfluesse.md) |
| Navigation und Informationsarchitektur | [IA-Optionen](03-ia-und-navigationsoptionen.md), [Entscheidungsregister](06-entscheidungsregister.md) |
| Layout, Tokens und Aktionen | [Layout- und Aktionssystem](04-layout-und-aktionssystem.md) |
| Re-Audit, Befunde und Codeinventar | [Findings und Evidenz](05-findings-und-evidenz.md) |
| Umsetzung der nächsten Etappe | [M2-Roadmap](../roadmap/02-systemrahmen-und-navigationsfluss/roadmap.md), [M2-Planung](../roadmap/02-systemrahmen-und-navigationsfluss/planning.md) |

## Gültiger Planungsstand

M1.0–M1.5 sind abgeschlossen. M2 berücksichtigt den abgeschlossenen Ein-Karten-Grid-Nachweis aus M1.5-T7 als Regression und legt keinen Duplikat-Leaf an. M2 startet mit drei sicheren Korrekturslices und endet vor weiteren IA-/Terminologieentscheidungen an einem manuellen Entscheidungsgate.

Die erste M2-Etappe ist ausdrücklich keine Gesamtneugestaltung: Sie liefert einen gemeinsamen Vollbreiten-Seitenrahmen mit lesbarer Prosa-Messung und einer Action-Group-Basis, korrigiert Mutation-Selection/URL-Synchronisation red-test-first und führt vom Transaktionsdetail sichtbar in den Bearbeitungsfluss. Featureweise Migration wird an 1280, 1920, 2560 und Responsive belegt; `AudiencesPage` bleibt außerhalb dieses Slices.

## Änderungsregeln für dieses Konzept

- Eine neue normative Aussage steht an genau einer Stelle; andere Dokumente verlinken dorthin.
- Ist-Befunde erhalten Quelle, Zustand, Datum/Lauf und die Unterscheidung zwischen historischem Befund und aktuellem Stand.
- Offene Entscheidungen nennen Optionen, Auswirkungen und Empfehlung. Empfehlung ist keine Freigabe zur Implementierung.
- Jede M2-Ausführung aktualisiert die betroffenen Konzept-/Roadmap-Nachweise im selben atomaren Dokumentations- beziehungsweise Codecommit; dieses Doku-Update selbst enthält keine Produktdateien.
