# M6 – Betriebs- und Qualitätshärtung

[Roadmap-Index](../../Roadmap.md)

- [ ] **M6 abschließen**

Abhängigkeit: [M1](../01-webhost-und-mcp-http/roadmap.md) bis [M5](../05-zielgruppen-content-und-rich-text/roadmap.md)

Verbindliche M0-Basis: gemeinsamer Kestrel-Origin und stateless `/mcp` werden gehärtet, nicht neu entworfen. Tree-Messungen beziehen sich auf natives 100er-Cursor-Paging mit höchstens zehn Circuit-Seiten; Radzen/alternative Trees und eine allgemeine UI-Bibliothek bleiben ausgeschlossen. Browser-E2E verwendet Playwright .NET mit der installierten aktuellen Chrome-Stable-Version, `Channel = "chrome"`, `Headless = true`.

Ziel: Das Kernfrontend ist unter realistischen Daten-, Parallelitäts- und Intranetbedingungen reproduzierbar betreibbar.

Referenzen: [Betriebsabnahme](../../konzept/06-betrieb-sicherheit-und-risiken.md#betriebsabnahme), [Risiken](../../konzept/06-betrieb-sicherheit-und-risiken.md#risiken-und-gegenmaßnahmen)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../../konzept/08-projektstruktur-und-codekonventionen.md)

## M6.0 – Manuelle Planung und Konzeptschärfung

- [ ] **M6.0 abschließen**
  - Durchführung: gemeinsam mit dem Benutzer nach Abschluss von M5; kein delegierbarer Implementierungs-Leaf-Task.
  - Entscheiden: Zieldeployment (O-012), gegebenenfalls produktive Prozess-/SQL-Identität und Secretbehandlung (O-022), Last- und Performanceziele (O-017), Recovery und Aufbewahrung (O-023) sowie Betriebsbeobachtung (O-024).
  - Prüfen: real implementierte Browser- und MCP-Workflows, gemessene Datenmengen, produktive Infrastruktur und verbliebene Qualitätsrisiken gegen die bisherigen Entwurfstasks.
  - Ergebnis: betroffene Konzepte, offene Fragen und alle nachfolgenden M6-Leaf-Tasks sind aktualisiert, eindeutig abnehmbar und atomar committed.
  - Gate: M6.1 und folgende Arbeitspakete dürfen erst danach durch Implementierungsagenten begonnen werden.

## M6.1 – Browser-End-to-End-Abnahme

- [ ] **M6.1 abschließen**

  - [ ] **M6.1-T1 – [Zentrale Leseabläufe als Browser-E2E absichern](tasks/M6.1-T1.md)**
  - [ ] **M6.1-T2 – [Zentrale Schreibabläufe als Browser-E2E absichern](tasks/M6.1-T2.md)**
## M6.2 – Verträge und Parallelität

- [ ] **M6.2 abschließen**

  - [ ] **M6.2-T1 – [UI-nahe Services und MCP gegen gemeinsame Use Cases prüfen](tasks/M6.2-T1.md)**
  - [ ] **M6.2-T2 – [Gleichzeitige UI-/MCP-Transactions und Konflikte testen](tasks/M6.2-T2.md)**
## M6.3 – Performance

- [ ] **M6.3 abschließen**

  - [ ] **M6.3-T1 – [Tiefe und breite Wissensbäume messen und optimieren](tasks/M6.3-T1.md)**
  - [ ] **M6.3-T2 – [Search, Diff und große Inhalte messen und optimieren](tasks/M6.3-T2.md)**
## M6.4 – Intranetdeployment

- [ ] **M6.4 abschließen**

  - [ ] **M6.4-T1 – [Entschiedene Deploymentkette abnehmen](tasks/M6.4-T1.md)**
  - [ ] **M6.4-T2 – [Netzwerk- und Hostkonfiguration härten](tasks/M6.4-T2.md)**
## M6.5 – Wiederherstellung und Abschluss

- [ ] **M6.5 abschließen**

  - [ ] **M6.5-T1 – [Backup, Restore und Neustart abnehmen](tasks/M6.5-T1.md)**
  - [ ] **M6.5-T2 – [Qualitätsgates vollständig grün schließen](tasks/M6.5-T2.md)**
  - [ ] **M6.5-T3 – [Ist-Dokumentation und Konzeptstatus final auditieren](tasks/M6.5-T3.md)**
## Milestone-Abnahme

- Kernfrontend und HTTP-MCP sind unter der realen Intranet-Topologie geprüft.
- Zentrale Browserabläufe, Adapterparität, Parallelität, Performance und Wiederherstellung sind belegt.
- Alle Qualitätsgates und die verbindliche Ist-Dokumentation sind vollständig grün beziehungsweise aktuell.
