# M6 – Betriebs- und Qualitätshärtung

[Roadmap-Index](../Roadmap.md)

- [ ] **M6 abschließen**

Abhängigkeit: [M1](01-webhost-und-mcp-http.md) bis [M5](05-rollen-content-und-rich-text.md)

Ziel: Das Kernfrontend ist unter realistischen Daten-, Parallelitäts- und Intranetbedingungen reproduzierbar betreibbar.

Referenzen: [Betriebsabnahme](../konzept/06-betrieb-sicherheit-und-risiken.md#betriebsabnahme), [Risiken](../konzept/06-betrieb-sicherheit-und-risiken.md#risiken-und-gegenmaßnahmen)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

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
    - Umfang: realistische Datenprofile, Tree-Lazy-Loading, Paging, Virtualisierung, Breadcrumbs und Nodewechsel.
    - Messen: Serverlatenz, SQL-Aufwand, übertragene Daten, Renderzeit und Speicher pro Circuit.
    - Regel: Grenzwerte vor der Optimierung festlegen; keine rein synthetische Mikrooptimierung.
    - Abnahme: die in O-017 festgelegten Interaktionszeiten sind belegt; bis dahin bleibt der Task offen.

  - [ ] **M6.3-T2 – Search, Diff und große Inhalte messen und optimieren**
    - Voraussetzung: O-017 definiert Referenzdatenmenge und messbare Zielwerte.
    - Umfang: große Trefferlisten, große Diffs und umfangreiches Markdown innerhalb der Limits.
    - Prüfen: Paging, Abbruch, Timeouts, Speicher und blockierte UI-Circuits.
    - Abnahme: große zulässige Vorgänge bleiben bedienbar und ressourcenbegrenzt.

## M6.4 – Intranetdeployment

- [ ] **M6.4 abschließen**

  - [ ] **M6.4-T1 – Reverse-Proxy-Betrieb abnehmen**
    - Voraussetzung: O-012 zur Zieldeploymenttopologie ist durch den Benutzer entschieden.
    - Umfang: vorgesehener Proxy mit Blazor-WebSockets, Reconnect, MCP Streamable HTTP und Download.
    - Prüfen: Forwarded Headers, Host, TLS-Terminierung, Body-/Timeoutlimits und Streamingpuffer.
    - Abnahme: reale Deploymentkette besteht definierte Browser- und MCP-Smokes.

  - [ ] **M6.4-T2 – Netzwerk- und Hostkonfiguration härten**
    - Umfang: Host/Port, `AllowedHosts`, Firewallsegment, TLS, CORS-Default, Secrets/Connection String und Logging.
    - Grenze: weiterhin keine Anwendungsauthentifizierung; kein Internet-Exposure.
    - Dokumentation: Konfiguration, Betrieb und bekannte Sicherheitsgrenze aktualisieren.
    - Abnahme: nur vorgesehene Netzsegmente erreichen den Dienst; keine Wildcardfreigaben.

## M6.5 – Wiederherstellung und Abschluss

- [ ] **M6.5 abschließen**

  - [ ] **M6.5-T1 – Backup, Restore und Neustart abnehmen**
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
