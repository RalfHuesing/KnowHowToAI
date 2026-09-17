# M0 – Komponenten- und Architekturentscheidungen

[Roadmap-Index](../Roadmap.md)

- [ ] **M0 abschließen**

Ziel: Technische Risiken und produktprägende Fremdkomponenten sind vor der eigentlichen Webimplementierung anhand realistischer Anforderungen entschieden.

Referenzen: [Komponentenstrategie](../konzept/02-bedienkonzept-und-ui.md#komponentenstrategie), [Offene Fragen](../konzept/07-entscheidungen-und-offene-fragen.md), [Ein Prozess und ein Port](../konzept/05-architektur-api-und-mcp.md#ein-prozess-und-ein-port)

## M0.1 – Ausgangslage

- [ ] **M0.1 abschließen**

  - [ ] **M0.1-T1 – Build-, Test- und Linter-Baseline nachweisen**
    - Umfang: aktuellen Build, FastTests, erforderliche Integrationstests und AiNetLinter gemäß Projektregeln ausführen.
    - Ergebnis: reproduzierbare Befehle, Laufzeiten und bekannte Abweichungen festhalten; keine Webänderung.
    - Abnahme: Baseline ist grün oder jede bestehende Abweichung ist vor weiterer Arbeit geklärt und separat behoben.
    - Abschluss: betroffene Ist-Dokumentation nur bei tatsächlicher Änderung aktualisieren; Task und Parentstatus committen.

## M0.2 – Host- und Routing-Spike

- [ ] **M0.2 abschließen**

  - [ ] **M0.2-T1 – Blazor und MCP HTTP in einem Host validieren**
    - Umfang: isolierter Spike für `WebApplication`, Blazor Interactive Server und MCP Streamable HTTP auf einem Kestrel-Port.
    - Prüfen: Endpoint Routing, Blazor-Circuit, MCP-Streaming, DI-Scopes, Start/Stop und Route-Kollisionen.
    - Nicht enthalten: produktive Hostmigration, REST/OpenAPI, UI-Design oder STDIO-Entfernung.
    - Abnahme: technische Machbarkeit und notwendige Hostleitplanken sind belegt.
    - Abschluss: Ergebnis im [Architekturkonzept](../konzept/05-architektur-api-und-mcp.md) festhalten und den geschlossenen Punkt aus den offenen Fragen entfernen; Spike-Code verwerfen oder bewusst übernehmen.

  - [ ] **M0.2-T2 – Routing- und Proxyannahmen verifizieren**
    - Umfang: `/`, `/mcp`, `/assets` und reserviertes `/api` mit realistischem Reverse-Proxy-Verhalten prüfen.
    - Prüfen: WebSocket-Upgrade, Streaming, Timeouts, Fallback-Routing und ein gemeinsamer Origin.
    - Nicht enthalten: produktives Deployment oder Security-Härtung.
    - Abnahme: ein Port ist bestätigt oder eine abweichende Entscheidung mit konkretem Befund dokumentiert.

## M0.3 – UI-Komponenten

- [ ] **M0.3 abschließen**

  - [ ] **M0.3-T1 – Allgemeines Blazor-Komponentenpaket auswählen**
    - Umfang: Layout, Navigation, Formulare, Dialoge, Tabellen, Benachrichtigungen, Theme und Barrierefreiheit anhand eines kleinen Prototyps vergleichen.
    - Prüfen: .NET-/Blazor-Kompatibilität, aktive Pflege, Lizenz, keine erzwungene Cloud, Testbarkeit und Bundle-/Betriebsauswirkungen.
    - Nicht enthalten: Knowledge Tree und Rich-Text-Editor; diese werden separat entschieden.
    - Abnahme: O-001 ist mit Entscheidung, Begründung, Lizenz und verworfenen Alternativen geschlossen.

  - [ ] **M0.3-T2 – Knowledge-Tree-Komponente auswählen**
    - Umfang: tiefe und breite Beispieldaten mit Lazy Loading, Paging, Virtualisierung, Auswahl, Tastaturbedienung sowie Drag-and-drop testen.
    - Prüfen: Zielvorschau, kontrolliertes Reordering und Integration in das gewählte Blazor-Paket.
    - Abnahme: O-003 ist geschlossen; Grenzen der Komponente sind dokumentiert.

  - [ ] **M0.3-T3 – Markdown-fähigen Rich-Text-Editor auswählen**
    - Umfang: realistische KnowHowTo-Inhalte mit Listen, Tabellen, Code, Links, Zitaten und Bildern roundtrippen.
    - Prüfen: Markdown als kanonisches Format, deaktivierbare Headings, Upload-Hooks, optionaler Quellmodus, Lizenz und Wartung.
    - Abnahme: O-002 ist geschlossen; bekannte Roundtrip-Grenzen besitzen Tests oder eine explizite Ablehnung.

## Milestone-Abnahme

- O-001 bis O-003 sind geschlossen; die niedrig priorisierte Assetentscheidung O-004 bleibt bis M8 offen.
- Jede gewählte Abhängigkeit besitzt eine geprüfte Lizenz- und Wartungsbewertung.
- Kein Kernmilestone M1 bis M6 hängt von einer unbekannten Kernkomponente oder unbestätigten Hostannahme ab.
