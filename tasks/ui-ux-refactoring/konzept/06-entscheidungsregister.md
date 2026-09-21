# Entscheidungsregister

**Version:** 1.1 · **Status:** M2-Entscheidungen gesetzt; spätere Themen ausdrücklich außerhalb des M2-Scopes

Dieses Register trennt verbindliche Leitplanken von noch nicht freigegebenen Produktentscheidungen. Für M2 sind die folgenden Entscheidungen gesetzt; offene spätere Themen blockieren M2 nicht und dürfen von M2-Leaves nicht vorweggenommen werden.

| ID | Entscheidung | Optionen und Auswirkungen | Empfehlung / Status |
|---|---|---|---|
| UX-001 | Einstieg Read-only Node → Bearbeitung | **Node-lokal:** direkter, auffindbarer Einstieg; benötigt Transaction-Start und klare Rückkehr. **Transaction-first:** Nutzer startet/öffnet erst Arbeitskopie; fachlich sicher, aber mehr Weg. | **Entschieden:** Node-lokale sichtbare `Bearbeiten`-Aktion für Explicit+Independent. Kleiner Dialog: kompatible offene Arbeitskopie ausdrücklich wählen oder `Neue Arbeitskopie beginnen`; danach derselbe Wissenseintrag und dieselbe Zielgruppe direkt im vorhandenen Editor. |
| UX-002 | IA-/Navigationsziel | Wenige dichte Bereiche halten Kontext und Routen stabil; mehr Task-Seiten vereinfachen Einzelaufgaben, erhöhen aber Navigation und Kontextverlust. | **Entschieden:** Hybrid mit bestehenden Fachrouten. M2 legt keine neue Route und keinen zweiten fachlichen Einstieg an. |
| UX-003 | Terminologie | „Node“, „Knoten“, „Inhalt“, „Arbeitskopie“ und „Transaction“ tragen unterschiedlich viel technische Last. Das fachliche Adressatenmodell bezeichnet eine Zielgruppe und keine ACL-Zielgruppe. | **Entschieden für M2:** sichtbar `Wissenseintrag`, `Arbeitskopie`, `Zielgruppe`, `Bearbeiten`; `Node`, `Transaction`, IDs und Revisionen nur progressive technische Details. |
| UX-004 | Fallback / erster eigener Content | Fallback read-only lassen; eigenen Content anlegen; Fallback übernehmen; explizit leer bestätigen. | **Entschieden im Webfrontend-M5.4:** Umsetzung nur dort, nicht in Layout-Leaves. |
| UX-005 | Commit vor Validierung | **Validieren → Commit/Discard** schützt bewusst; **Commit → Validieren** wäre schneller, verändert aber Sicherheits- und Fehlervertrag. | **Außerhalb M2 zurückgestellt.** Bis Entscheidung unverändert Validieren → Commit/Discard. |
| UX-006 | Icon-/Textdarstellung | Text ist selbsterklärend; Icon+Text kompakt; Icon-only spart Platz, erhöht Entdeckungs- und A11y-Risiko. | **Für M2 gesetzt:** Bearbeitungs- und Hauptaktionen verwenden sichtbaren Text; History/Download bleiben sekundär. Icon-only wird nicht als M2-Einstieg verwendet. |
| UX-007 | PageActions-Kandidaten | Keine Nutzung; nur seitenweite Aktionen; lokale Actions in Featureabschnitt; globale Registry. | **Entschieden:** Slot nur für echte seitenweite Aktionen, keine Action Registry. |
| UX-008 | Pflichtzielgruppe vor Navigation | Zielgruppe erst nach `/knowledge` auswählen; Zielgruppe global vorher wählen; stille Defaultzielgruppe. | **Entschieden im Webfrontend:** Pflichtauswahl ohne stille Standardzielgruppe; Zielgruppe bleibt Perspektive, nicht ACL. |
| UX-009 | Mutation URL/Selection | URL nach Mutation aktiv synchronisieren; nur Selection aktualisieren; Reload erzwingen. | **Empfehlung und M2-Leaf:** URL/Selection atomar als sichtbaren Zustand führen, red-test-first; Fachroute bleibt stabil. |
| UX-010 | M1.5-T7 Grid | Separater M1.5-Leaf erledigt T7; M2 dupliziert ihn nicht und prüft nur Regression. | **Erledigt:** M1.5-T7 ist abgeschlossen; M2 referenziert seinen Nachweis. |
| UX-011 | Geführter Abschluss als Assistent | **Stepper auf derselben Transaction-Seite:** Validieren, Änderungen prüfen, Commit-Nachricht und Bestätigung werden als wenige lineare Schritte geführt; Kontext und Route bleiben erhalten. **Mehrere Einzelseiten:** noch weniger Inhalt pro Ansicht, aber zusätzliche Routen, Rücksprünge und Zustandsübergänge. **Aktuelle Abschnittsseite:** geringste Änderung, aber weniger klare Führung für seltene irreversible Abläufe. | **Außerhalb M2 offen:** später separat entscheiden. Kein M2-Leaf darf den Abschluss- oder Commit-Vertrag ändern. |

## Bereits gesetzte Systemleitplanken

Modern-clean, harmonisch, volle Monitorbreite für Seitenrahmen, lesbare Measure nur für Prosa, DRY Tokens/Base-Patterns, eine primäre Aktion je Abschnitt, progressive technische Details, vorhandene Funktionen/Routen/Kontexte erhalten, Tastatur nicht als eigenständiges UI-Refactoring-Ziel und `AudiencesPage` außerhalb M2. Diese Leitplanken stehen ausführlich in [Bedienmodell](01-bedienmodell-und-mentale-modelle.md) und [Layout-/Aktionssystem](04-layout-und-aktionssystem.md).
