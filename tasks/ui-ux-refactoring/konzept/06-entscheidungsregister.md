# Entscheidungsregister

**Version:** 1.0 · **Status:** offene Entscheidungen und gesetzte Leitplanken

Dieses Register trennt verbindliche Leitplanken von noch nicht freigegebenen Produktentscheidungen. Eine Empfehlung erlaubt keine Implementierung außerhalb des genannten Scopes.

| ID | Entscheidung | Optionen und Auswirkungen | Empfehlung / Status |
|---|---|---|---|
| UX-001 | Einstieg Read-only Node → Bearbeitung | **Node-lokal:** direkter, auffindbarer Einstieg; benötigt Transaction-Start und klare Rückkehr. **Transaction-first:** Nutzer startet/öffnet erst Arbeitskopie; fachlich sicher, aber mehr Weg. | **Offen, M2-Gate.** Hybrid vorbereiten; bestehende Wege sichtbar machen, Semantik nicht erfinden. |
| UX-002 | IA-/Navigationsziel | Wenige dichte Bereiche halten Kontext und Routen stabil; mehr Task-Seiten vereinfachen Einzelaufgaben, erhöhen aber Navigation und Kontextverlust. | **Offen, Empfehlung Hybrid.** M2-Basis ohne neue Routen; Gate nach ersten Slices. |
| UX-003 | Terminologie | „Node“, „Knoten“, „Inhalt“, „Arbeitskopie“, „Transaction“ unterschiedlich technische Last. | **Offen.** Bestehende Domänenbegriffe nicht eigenmächtig umbenennen; Begriffstest mit Support/Consultants vor Gate. |
| UX-004 | Fallback / erster eigener Content | Fallback read-only lassen; eigenen Content anlegen; Fallback übernehmen; explizit leer bestätigen. | **Entschieden im Webfrontend-M5.4:** Umsetzung nur dort, nicht in Layout-Leaves. |
| UX-005 | Commit vor Validierung | **Validieren → Commit/Discard** schützt bewusst; **Commit → Validieren** wäre schneller, verändert aber Sicherheits- und Fehlervertrag. | **Offen zurückgestellt.** Bis Entscheidung unverändert Validieren → Commit/Discard. |
| UX-006 | Icon-/Textdarstellung | Text ist selbsterklärend; Icon+Text kompakt; Icon-only spart Platz, erhöht Entdeckungs- und A11y-Risiko. | **Empfehlung:** vorläufig Text oder Icon+Text; History/Download selten und kompakt. |
| UX-007 | PageActions-Kandidaten | Keine Nutzung; nur seitenweite Aktionen; lokale Actions in Featureabschnitt; globale Registry. | **Entschieden:** Slot nur für echte seitenweite Aktionen, keine Action Registry. |
| UX-008 | Pflichtrolle vor Navigation | Rolle erst nach `/knowledge` auswählen; Rolle global vorher wählen; stille Defaultrolle. | **Entschieden im Webfrontend:** Pflichtauswahl ohne stille Standardrolle; Rolle bleibt Perspektive, nicht ACL. |
| UX-009 | Mutation URL/Selection | URL nach Mutation aktiv synchronisieren; nur Selection aktualisieren; Reload erzwingen. | **Empfehlung und M2-Leaf:** URL/Selection atomar als sichtbaren Zustand führen, red-test-first; Fachroute bleibt stabil. |
| UX-010 | M1.5-T7 Grid | Separater M1.5-Leaf erledigt T7; M2 dupliziert ihn; M2 prüft nur Regression. | **Entschieden:** M1.5-T7 bleibt offen und wird nicht dupliziert; M2 referenziert ihn als Abhängigkeit/Nachweis. |

## Bereits gesetzte Systemleitplanken

Modern-clean, harmonisch, volle Monitorbreite für Seitenrahmen, lesbare Measure nur für Prosa, DRY Tokens/Base-Patterns, eine primäre Aktion je Abschnitt, progressive technische Details, vorhandene Funktionen/Routen/Kontexte erhalten, Tastatur nicht als eigenständiges UI-Refactoring-Ziel und `RolesPage` außerhalb M2. Diese Leitplanken stehen ausführlich in [Bedienmodell](01-bedienmodell-und-mentale-modelle.md) und [Layout-/Aktionssystem](04-layout-und-aktionssystem.md).
