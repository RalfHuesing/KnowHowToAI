# M0 – Komponenten- und Architekturentscheidungen

[Roadmap-Index](../Roadmap.md)

- [ ] **M0 abschließen**

Ziel: Technische Risiken und produktprägende Fremdkomponenten sind vor der eigentlichen Webimplementierung anhand realistischer Anforderungen entschieden.

Referenzen: [Komponentenstrategie](../konzept/02-bedienkonzept-und-ui.md#komponentenstrategie), [Offene Fragen](../konzept/07-entscheidungen-und-offene-fragen.md), [Ein Prozess und ein Port](../konzept/05-architektur-api-und-mcp.md#ein-prozess-und-ein-port)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## M0.1 – Ausgangslage

- [ ] **M0.1 abschließen**

  - [ ] **M0.1-T1 – Build-, Test- und Linter-Baseline nachweisen**
    - Umfang: aktuellen Build, FastTests, erforderliche Integrationstests und AiNetLinter gemäß Projektregeln ausführen.
    - Ergebnis: reproduzierbare Befehle, Laufzeiten und bekannte Abweichungen festhalten; keine Webänderung.
    - Abnahme: Baseline ist grün oder jede bestehende Abweichung ist vor weiterer Arbeit geklärt und separat behoben.
    - Abschluss: betroffene Ist-Dokumentation nur bei tatsächlicher Änderung aktualisieren; Task und Parentstatus committen.

  - [ ] **M0.1-T2 – MIT-Lizenz und Abhängigkeitsbaseline herstellen**
    - Ausgangslage: Die MIT-`LICENSE` mit `Copyright (c) 2026 Ralf Hüsing` ist im Repository-Root vorhanden.
    - Umfang: vorhandene `LICENSE` auf unveränderten MIT-Text prüfen; vollständigen direkten und transitiven Abhängigkeitsgraph aller Solution-Projekte einschließlich Build- und Testwerkzeugen ermitteln.
    - Ergebnis: reproduzierbares Inventar mit Paket, Version, Quelle, Lizenz und einzuhaltenden Copyright-/Lizenz-/NOTICE-Pflichten in `THIRD-PARTY-NOTICES.md`; erforderliche Originalhinweise beilegen.
    - Abnahme: keine Abhängigkeit ist kostenpflichtig, lizenzseitig ungeklärt oder mit der MIT-Distribution unvereinbar; Abweichungen werden ersetzt oder vor Fortsetzung dem Benutzer vorgelegt.
    - Abschluss: Lizenz-/Inventardateien und die dauerhaft erforderliche Aktualisierungsanweisung dokumentieren und atomar committen.

## M0.2 – Host- und Routing-Spike

- [ ] **M0.2 abschließen**

  - [ ] **M0.2-T1 – Blazor und MCP HTTP in einem Host validieren**
    - Umfang: isolierter Spike für `WebApplication`, Blazor Interactive Server und MCP Streamable HTTP auf einem Kestrel-Port.
    - Prüfen: Endpoint Routing, Blazor-Circuit, MCP-Streaming, DI-Scopes, Start/Stop und Route-Kollisionen.
    - Nicht enthalten: produktive Hostmigration, REST/OpenAPI, UI-Design oder STDIO-Entfernung.
    - Abnahme: technische Machbarkeit und notwendige Hostleitplanken sind belegt.
    - Abschluss: Ergebnis im [Architekturkonzept](../konzept/05-architektur-api-und-mcp.md) festhalten; Spike-Code wird nicht als Produktionscode committed und die produktive Umsetzung beginnt erst in M1.

  - [ ] **M0.2-T2 – Routing- und Proxyannahmen verifizieren**
    - Umfang: `/`, `/mcp` und reserviertes `/api` mit realistischem Reverse-Proxy-Verhalten prüfen; spätere Download-/Assetpräfixe gegen die Fallbackregeln konzeptionell abgleichen.
    - Prüfen: WebSocket-Upgrade, Streaming, Timeouts, Fallback-Routing und ein gemeinsamer Origin.
    - Nicht enthalten: produktives Deployment oder Security-Härtung.
    - Abnahme: der gemeinsame Port ist bestätigt. Ist das nicht möglich, bleibt der Task offen und der Agent fragt den Benutzer, statt selbst auf mehrere Ports auszuweichen.

## M0.3 – UI-Komponenten

- [ ] **M0.3 abschließen**

  - [ ] **M0.3-T1 – UI-Komponentenbasis auswählen**
    - Voraussetzung: O-013 zu unterstützten Browsern und O-021 zum Barrierefreiheitsziel sind durch den Benutzer entschieden.
    - Umfang: native Blazor-/HTML-/CSS-Lösung und höchstens leichte OSS-Bibliotheken für Layout, Navigation, Formulare, Dialoge, Tabellen, Benachrichtigungen, Theme und Barrierefreiheit anhand eines kleinen Prototyps vergleichen.
    - Prüfen: fachlicher Mehrwert gegenüber Eigenlösung, .NET-/Blazor-Kompatibilität, aktive Pflege, kostenlose MIT-distributionskompatible Lizenz einschließlich transitiver Abhängigkeiten und NOTICE-Pflichten, Paket-/Bundle-Gewicht, zusätzliche Toolchain, JS-Interop, keine erzwungene Cloud/CDN-Nutzung sowie Testbarkeit und Betriebsauswirkungen.
    - Nicht enthalten: Knowledge Tree und Rich-Text-Editor; diese werden separat entschieden.
    - Abnahme: O-001 ist mit Entscheidung, Begründung, Lizenzprüfung und verworfenen Alternativen geschlossen; Browserunterstützung erfüllt O-013. „Keine allgemeine Komponentenbibliothek“ ist ausdrücklich zulässig.

  - [ ] **M0.3-T2 – Knowledge-Tree-Komponente auswählen**
    - Voraussetzung: O-013 zu unterstützten Browsern und O-021 zum Barrierefreiheitsziel sind durch den Benutzer entschieden.
    - Umfang: tiefe und breite Beispieldaten mit Lazy Loading, Paging, Virtualisierung, Auswahl, Tastaturbedienung sowie Drag-and-drop testen.
    - Prüfen: Zielvorschau, kontrolliertes Reordering, Integration in die gewählte UI-Basis sowie kostenlose MIT-distributionskompatible Lizenz aller direkten und transitiven Abhängigkeiten.
    - Abnahme: O-003 ist geschlossen; Grenzen der Komponente sind dokumentiert.

  - [ ] **M0.3-T3 – Markdown-fähigen Rich-Text-Editor auswählen**
    - Voraussetzung: O-013 zu unterstützten Browsern und O-020 zur Content-Sicherheits-/Fremdressourcen-Policy sind durch den Benutzer entschieden.
    - Umfang: realistische KnowHowTo-Inhalte mit Listen, Tabellen, Code, Links, Zitaten und Bildern roundtrippen.
    - Prüfen: Markdown als kanonisches Format, deaktivierbare Headings, Upload-Hooks, optionaler Quellmodus, kostenlose MIT-distributionskompatible Lizenz aller direkten und transitiven Abhängigkeiten sowie Wartung.
    - Abnahme: O-002 ist geschlossen und alle im Contentkonzept geforderten Markdownstrukturen bestehen den Roundtrip; ein Kandidat mit semantischem Verlust wird nicht gewählt.

  - [ ] **M0.3-T4 – Web-Komponenten- und Browser-Testwerkzeuge auswählen**
    - Umfang: bUnit für Razor-Komponententests, Microsoft Playwright .NET für echte Headless-Chrome-E2E und bei nichttrivialer eigener JavaScript-Logik Vitest als JS-Unit-Testwerkzeug gegen .NET 10, xUnit v3, CI-/lokalen Betrieb und die gewählten UI-Komponenten prüfen.
    - Prüfen: Interaktion, JS-Interop, Screenshots, reproduzierbare nichtinteraktive Chrome-Installation, ausschließlicher Headless-Betrieb, kostenlose MIT-distributionskompatible Lizenz aller direkten und transitiven Abhängigkeiten sowie Wartung.
    - Regel: keine Browsermatrix und keine sichtbaren Browserstarts durch Implementierungsagenten. Vitest wird nur eingeführt, wenn eigenes JavaScript eigenständige testwürdige Logik enthält; dünnes JS-Interop wird über Komponenten- und Browsertests abgedeckt.
    - Nicht enthalten: Testprojekte oder produktive Testfälle; diese folgen in M2.
    - Abnahme: O-015 ist geschlossen; Werkzeuge und verworfene Alternativen sind im Strukturkonzept dokumentiert.

## Milestone-Abnahme

- O-001 bis O-003 und O-015 sind geschlossen; die niedrig priorisierte Assetentscheidung O-004 bleibt bis M8 offen.
- Jede gewählte direkte und transitive Abhängigkeit ist kostenlos nutzbar, mit der MIT-Distribution vereinbar und besitzt eine dokumentierte Lizenz-, Pflicht- und Wartungsbewertung.
- Kein Kernmilestone M1 bis M6 hängt von einer unbekannten Kernkomponente oder unbestätigten Hostannahme ab.
