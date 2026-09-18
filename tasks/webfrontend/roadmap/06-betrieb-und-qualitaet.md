# M6 – Betriebs- und Qualitätshärtung

[Roadmap-Index](../Roadmap.md)

- [ ] **M6 abschließen**

Abhängigkeit: [M1](01-webhost-und-mcp-http.md) bis [M5](05-rollen-content-und-rich-text.md)

Verbindliche M0-Basis: gemeinsamer Kestrel-Origin und stateless `/mcp` werden gehärtet, nicht neu entworfen. Tree-Messungen beziehen sich auf natives 100er-Cursor-Paging mit höchstens zehn Circuit-Seiten; Radzen/alternative Trees und eine allgemeine UI-Bibliothek bleiben ausgeschlossen. Browser-E2E bleibt auf Playwright .NET `1.62.0` mit Chrome (installierte aktuelle Version), `Channel = "chrome"`, `Headless = true` festgelegt.

Ziel: Das Kernfrontend ist unter realistischen Daten-, Parallelitäts- und Intranetbedingungen reproduzierbar betreibbar.

Referenzen: [Betriebsabnahme](../konzept/06-betrieb-sicherheit-und-risiken.md#betriebsabnahme), [Risiken](../konzept/06-betrieb-sicherheit-und-risiken.md#risiken-und-gegenmaßnahmen)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## M6.0 – Manuelle Planung und Konzeptschärfung

- [ ] **M6.0 abschließen**
  - Durchführung: gemeinsam mit dem Benutzer nach Abschluss von M5; kein delegierbarer Implementierungs-Leaf-Task.
  - Entscheiden: Zieldeployment (O-012), gegebenenfalls produktive Prozess-/SQL-Identität und Secretbehandlung (O-022), Last- und Performanceziele (O-017), Recovery und Aufbewahrung (O-023) sowie Betriebsbeobachtung (O-024).
  - Prüfen: real implementierte Browser- und MCP-Workflows, gemessene Datenmengen, produktive Infrastruktur und verbliebene Qualitätsrisiken gegen die bisherigen Entwurfstasks.
  - Ergebnis: betroffene Konzepte, offene Fragen und alle nachfolgenden M6-Leaf-Tasks sind aktualisiert, eindeutig abnehmbar und atomar committed.
  - Gate: M6.1 und folgende Arbeitspakete dürfen erst danach durch Implementierungsagenten begonnen werden.

## M6.1 – Browser-End-to-End-Abnahme

- [ ] **M6.1 abschließen**

  - [ ] **M6.1-T1 – Zentrale Leseabläufe als Browser-E2E absichern**
    - Umfang: Start, Kontext/Rolle, Baum, Node, Suche, Historie, Diff und Markdown-Export.
    - Daten: deterministischer realistischer Testbestand mit Fallback, stale Content und tiefer Hierarchie.
    - Prüfen: Navigation, Refresh, leere Zustände und Fehlerdarstellung.
    - Abnahme: vollständiger Read-only-Arbeitsweg läuft reproduzierbar im Browser.

  - [ ] **M6.1-T2 – Zentrale Schreibabläufe als Browser-E2E absichern**
    - Umfang: Transaction, Nodeänderungen, Rolle/Content, Editor, Validierung, Diff, Commit und Discard.
    - Prüfen: Reconnect, Navigation mit ungespeichertem Formzustand und serverseitige Ablehnung.
    - Abnahme: je ein vollständiger erfolgreicher und abgelehnter Schreibweg ist automatisiert.

## M6.2 – Verträge und Parallelität

- [ ] **M6.2 abschließen**

  - [ ] **M6.2-T1 – UI-nahe Services und MCP gegen gemeinsame Use Cases prüfen**
    - Umfang: repräsentative Reads/Writes, Fehler, Warnungen, Paging und Kontextparameter.
    - Ziel: semantische Drift der Adapter erkennen; keine unnötige Testduplikation.
    - Abnahme: alle fachlichen Unterschiede sind beabsichtigt und dokumentiert.

  - [ ] **M6.2-T2 – Gleichzeitige UI-/MCP-Transactions und Konflikte testen**
    - Umfang: parallele Working Transactions, konkurrierende Commits, `SnapshotConflict` und Reapply.
    - Prüfen: kein versteckter Circuit-/Request-State und kein Lost Update.
    - Abnahme: Parallelitätsverhalten entspricht vollständig den dokumentierten Invarianten.

## M6.3 – Performance

- [ ] **M6.3 abschließen**

  - [ ] **M6.3-T1 – Tiefe und breite Wissensbäume messen und optimieren**
    - Voraussetzung: O-017 definiert Referenzdatenmenge und messbare Zielwerte.
    - Umfang: realistische Datenprofile, natives Tree-Lazy-Loading, opakes 100er-Paging, Zehn-Seiten-LRU, Neuzentrierung, Breadcrumbs und Nodewechsel. Viewport-Virtualisierung ist keine bereits vorhandene Eigenschaft; sie darf nur bei einem im Task belegten Zielverstoß als eigener dokumentierter Umsetzungsslice ergänzt werden.
    - Messen: Serverlatenz, SQL-Aufwand, übertragene Daten, Renderzeit und Speicher pro Circuit.
    - Regel: Grenzwerte vor der Optimierung festlegen; keine rein synthetische Mikrooptimierung.
    - Abnahme: die in O-017 festgelegten Interaktionszeiten sind für 1, 10 und 11 geladene Seiten, mehr als 1.000 Geschwister und die festgelegte Referenztiefe belegt; der Circuit hält nie mehr als zehn Tree-Seiten. Bis O-017 geschlossen ist, bleibt der Task offen.

  - [ ] **M6.3-T2 – Search, Diff und große Inhalte messen und optimieren**
    - Voraussetzung: O-017 definiert Referenzdatenmenge und messbare Zielwerte.
    - Umfang: große Trefferlisten, große Diffs und umfangreiches Markdown innerhalb der Limits.
    - Prüfen: Paging, Abbruch, Timeouts, Speicher und blockierte UI-Circuits.
    - Abnahme: große zulässige Vorgänge bleiben bedienbar und ressourcenbegrenzt.

## M6.4 – Intranetdeployment

- [ ] **M6.4 abschließen**

  - [ ] **M6.4-T1 – Entschiedene Deploymentkette abnehmen**
    - Voraussetzung: O-012 zur Zieldeploymenttopologie ist durch den Benutzer entschieden.
    - Umfang: die in O-012 festgelegte reale Kette mit Blazor-WebSockets, Reconnect, MCP Streamable HTTP und Download. Entscheidet O-012 direkten Kestrel-Betrieb, wird kein Proxy eingeführt; entscheidet O-012 einen Proxy, wird ausschließlich das konkret benannte Produkt konfiguriert und geprüft.
    - Prüfen: immer Host, Scheme, Port, TLS-Terminierung und Netzwerkpfad; bei einem Proxy zusätzlich Forwarded Headers, WebSocket-Upgrade, Body-/Timeoutlimits und Streamingpuffer.
    - Abnahme: die konkret entschiedene Deploymentkette besteht definierte Browser- und MCP-Smokes; es existiert keine ungenutzte Proxykonfiguration oder zweite Portarchitektur.

  - [ ] **M6.4-T2 – Netzwerk- und Hostkonfiguration härten**
    - Voraussetzung: O-022 zur produktiven Secretquelle und O-024 zur Betriebsbeobachtung sind durch den Benutzer entschieden.
    - Umfang: Host/Port, `AllowedHosts`, Firewallsegment, TLS, CORS-Default, Secrets/Connection String und Logging.
    - Grenze: weiterhin keine Anwendungsauthentifizierung; kein Internet-Exposure.
    - Dokumentation: Konfiguration, Betrieb und bekannte Sicherheitsgrenze aktualisieren.
    - Abnahme: nur vorgesehene Netzsegmente erreichen den Dienst; keine Wildcardfreigaben.

## M6.5 – Wiederherstellung und Abschluss

- [ ] **M6.5 abschließen**

  - [ ] **M6.5-T1 – Backup, Restore und Neustart abnehmen**
    - Voraussetzung: O-023 definiert RPO, RTO und Aufbewahrung.
    - Umfang: SQL konsistent sichern/wiederherstellen; Neustart mit committed und offenen Working Transactions.
    - Prüfen: Migration beim Start und dokumentiertes Recoveryverfahren.
    - Abnahme: definierter Datenstand ist auf einer frischen Zielumgebung fachlich rekonstruierbar.

  - [ ] **M6.5-T2 – Qualitätsgates vollständig grün schließen**
    - Umfang: Build, Linter, FastTests, erforderliche Integrationstests und Browser-E2E gemäß Projektregeln.
    - Prüfen: keine übersprungenen Tests ohne dokumentierte Begründung; Warnungen und Flakes geklärt.
    - Abnahme: reproduzierbarer grüner Gesamtstand.

  - [ ] **M6.5-T3 – Ist-Dokumentation und Konzeptstatus final auditieren**
    - Umfang: sämtliche betroffenen `docs/` gegen Code und Tests prüfen; Roadmap-Checkboxen und Konzeptentscheidungen abgleichen.
    - Regel: `docs/` beschreibt nur implementiertes Verhalten; zukünftige Optionen verbleiben unter `tasks/`.
    - Abnahme: keine bekannte Abweichung zwischen Implementierung, Tests und verbindlicher Dokumentation.

## Milestone-Abnahme

- Kernfrontend und HTTP-MCP sind unter der realen Intranet-Topologie geprüft.
- Zentrale Browserabläufe, Adapterparität, Parallelität, Performance und Wiederherstellung sind belegt.
- Alle Qualitätsgates und die verbindliche Ist-Dokumentation sind vollständig grün beziehungsweise aktuell.
