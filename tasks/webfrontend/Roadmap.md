# Roadmap: Webfrontend und Wissensplattform

Stand: 2026-09-17

## Arbeitsregeln

- Milestones werden in Reihenfolge umgesetzt; Abweichungen erfordern dokumentierte Begründung.
- Eine Parent-Checkbox wird erst gesetzt, wenn alle Unterpunkte und Abnahmekriterien erfüllt sind.
- Jede Aufgabe folgt den Projektregeln und der Lese-Matrix in [`docs/`](../../docs/README.md).
- Code, Tests, verbindliche Ist-Dokumentation, Konzeptstatus und Checkbox werden im selben atomaren Commit aktualisiert.
- Ein Spike endet mit einer dokumentierten Entscheidung; wegwerfbarer Spike-Code gelangt nicht ungeprüft in Produktion.
- Authentifizierung und Autorisierung gehören ausdrücklich nicht zu dieser Roadmap.

## M0 – Komponenten- und Architekturentscheidungen

Referenzen: [Komponentenstrategie](konzept/02-bedienkonzept-und-ui.md#komponentenstrategie), [Offene Entscheidungen](konzept/07-entscheidungen-und-offene-fragen.md#offene-entscheidungen), [Ein Prozess und ein Port](konzept/05-architektur-api-und-mcp.md#ein-prozess-und-ein-port)

- [ ] M0 abschließen
  - [ ] Aktuellen Build-, Test- und Linter-Baseline-Nachweis herstellen.
  - [ ] Kombinierten ASP.NET-Core-Host mit Blazor, Minimal API und `/mcp` in einem technischen Spike validieren.
  - [ ] Routing auf einem gemeinsamen Port inklusive Blazor-Circuit, REST und MCP-Streaming validieren.
  - [ ] UI-Komponentenpaket anhand realer Layout-, Formular-, Tabellen- und Dialoganforderungen auswählen.
  - [ ] Baumkomponente mit Lazy Loading, Virtualisierung, Drag-and-drop und Tastaturbedienung auswählen.
  - [ ] Rich-Text-Komponente mit Markdown-Roundtrip, Heading-Sperre, Tabellen, Code und Upload-Hook auswählen.
  - [ ] Asset-Speicheroptionen und Deduplizierungsmodell entscheiden.
  - [ ] PDF-Renderer mit TOC, Header/Footer, Fonts und realistischem Deployment evaluieren.
  - [ ] Entscheidungen O-001 bis O-007 im [Entscheidungsregister](konzept/07-entscheidungen-und-offene-fragen.md) schließen.
  - [ ] Keine produktive Abhängigkeit ohne Lizenz- und Wartungsprüfung aufnehmen.

Abnahme: Alle hoch priorisierten Komponenten- und Architekturfragen sind entschieden; kein Implementierungsmilestone hängt von einem unbekannten Kernbaustein ab.

## M1 – Gemeinsamer Webhost und MCP HTTP

Abhängigkeit: M0. Referenzen: [Zielbild](konzept/05-architektur-api-und-mcp.md#zielbild), [MCP-Transport](konzept/05-architektur-api-und-mcp.md#mcp-transport), [Projektstruktur](konzept/05-architektur-api-und-mcp.md#projekt--und-namespace-struktur)

- [ ] M1 abschließen
  - [ ] `KnowHowToAI.Server` auf `Microsoft.NET.Sdk.Web` und `WebApplication` umstellen.
  - [ ] Bestehende Konfiguration, Migrationen, Logging und Application-Service-Registrierung unverändert integrieren.
  - [ ] Blazor-Interactive-Server-Shell unter `/` bereitstellen.
  - [ ] REST-Route Group `/api/v1` und OpenAPI-Dokument-Endpunkt anlegen.
  - [ ] Asset-Route reservieren, ohne vorgezogenes Asset-Fachmodell.
  - [ ] `ModelContextProtocol.AspNetCore` integrieren und stateless Streamable HTTP auf `/mcp` mappen.
  - [ ] Bestehende MCP-Tools und Fehlerverträge unverändert über HTTP abnehmen.
  - [ ] Routing-Smoke-Tests für `/`, `/api/v1`, `/openapi`, `/mcp` und `/assets` erstellen.
  - [ ] Reale Zielclients gegen MCP HTTP testen.
  - [ ] STDIO-Konfiguration, Runner und nicht mehr benötigte Pakete per Hard Cut entfernen.
  - [ ] `docs/Architektur.md`, `docs/McpApi.md`, Betrieb und Konfiguration auf den Ist-Stand aktualisieren.

Abnahme: Eine EXE bedient auf einem Port Blazor-Shell, OpenAPI und stateless MCP HTTP; alle bisherigen MCP-Funktionen bestehen ihre Abnahme ohne STDIO.

## M2 – Designsystem und Anwendungsshell

Abhängigkeit: M1. Referenzen: [Visueller Stil](konzept/02-bedienkonzept-und-ui.md#visueller-stil), [Grundlayout](konzept/02-bedienkonzept-und-ui.md#grundlayout), [Blazor-Betrieb](konzept/06-betrieb-sicherheit-und-risiken.md#blazor-betrieb)

- [ ] M2 abschließen
  - [ ] Gewähltes UI-Komponentenpaket und zentrales Theme integrieren.
  - [ ] Sachlich-seriöse Typografie, Farben, Raster, Abstände und Icons definieren.
  - [ ] Hauptlayout mit Navigation, Arbeitsfläche, Kontextbereich und globaler Kontextleiste implementieren.
  - [ ] Globale Darstellung für Snapshot/Transaction, Rolle und Änderungszustand implementieren.
  - [ ] Wiederverwendbare Lade-, Leer-, Fehler-, Warnungs- und Bestätigungszustände erstellen.
  - [ ] Responsive Mindestdarstellung und Tastaturnavigation sicherstellen.
  - [ ] Reconnect- und Circuit-Verlust-Oberfläche definieren.
  - [ ] Komponenten- und visuelle Smoke-Tests etablieren.

Abnahme: Einheitliche, moderne Anwendungsshell ohne fachliche Platzhalterlogik; alle folgenden Features verwenden dasselbe Designsystem.

## M3 – Read-only Wissenscockpit

Abhängigkeit: M2. Referenzen: [Dashboard](konzept/02-bedienkonzept-und-ui.md#dashboard), [Wissensbaum](konzept/02-bedienkonzept-und-ui.md#wissensbaum), [Historie und Releases](konzept/02-bedienkonzept-und-ui.md#historie-und-releases)

- [ ] M3 abschließen
  - [ ] Dashboard für Current Snapshot, letzten Release, offene Transactions, stale Content und Findings implementieren.
  - [ ] Lazy Knowledge Tree mit Paging, Breadcrumbs, Auswahl und virtueller Darstellung implementieren.
  - [ ] Globalen Rollen- und Lesekontext-Selektor implementieren.
  - [ ] Read-only Node-Ansicht mit Struktur-, Content-, Fallback-, Revision- und Freshness-Daten implementieren.
  - [ ] Suche mit Paging und Rollenauflösung implementieren.
  - [ ] Snapshot-, Release- und Diff-Ansichten implementieren.
  - [ ] Bestehenden Markdown-Export zugänglich machen.
  - [ ] Korrespondierende versionierte REST-Leseendpunkte implementieren und in OpenAPI dokumentieren.
  - [ ] UI-, REST- und MCP-Ergebnisse mit transportübergreifenden Tests vergleichen.

Abnahme: Der gesamte bestehende Wissensstand ist ohne MCP-Client navigierbar und lesbar; REST und UI zeigen denselben Zustand wie MCP.

## M4 – Transactions und Strukturpflege

Abhängigkeit: M3. Referenzen: [Transaction-Arbeitsbereich](konzept/02-bedienkonzept-und-ui.md#transaction-arbeitsbereich), [Wissensbaum](konzept/02-bedienkonzept-und-ui.md#wissensbaum), [DI-Grenzen](konzept/05-architektur-api-und-mcp.md#di--und-zustandsgrenzen)

- [ ] M4 abschließen
  - [ ] Transaction beginnen, auswählen, fortsetzen, validieren, committen und verwerfen.
  - [ ] Pro Browserarbeitskontext genau eine aktive Transaction führen; Kontextwechsel absichern.
  - [ ] Nodes erstellen, Titel/Description bearbeiten und löschen.
  - [ ] Nodes per Drag-and-drop verschieben und sortieren.
  - [ ] Zielvorschau und serverseitige Strukturvalidierung anzeigen.
  - [ ] Strukturierten Transaction-Diff und Findings vor Commit darstellen.
  - [ ] `SnapshotConflict` mit Base/Current-Vergleich und geführtem Reapply behandeln.
  - [ ] Korrespondierende REST-Schreibendpunkte mit expliziter `TransactionId` implementieren.
  - [ ] Browsernavigation, Reconnect und Prozessneustart gegen persistierte Working Transactions testen.

Abnahme: Die komplette Node-Struktur ist ohne Agent sicher und transaktional pflegbar.

## M5 – Rollen-Content und Rich Text

Abhängigkeit: M4. Referenzen: [Node-Ansicht und Editor](konzept/02-bedienkonzept-und-ui.md#node-ansicht-und-editor), [Rich-Text-Editor](konzept/03-redaktion-und-assets.md#rich-text-editor), [Rollenverwaltung](konzept/02-bedienkonzept-und-ui.md#rollenverwaltung)

- [ ] M5 abschließen
  - [ ] Gewählten Rich-Text-Editor ohne Heading-Funktionen integrieren.
  - [ ] Verlustarmen Markdown-Roundtrip durch Golden-Master- und Property-nahe Beispieldaten absichern.
  - [ ] Optionalen Markdown-Quellmodus integrieren.
  - [ ] Rollen-Content erstellen, ersetzen und löschen.
  - [ ] `Independent`/`Derived`, Quellen, Revisionen und Freshness bearbeiten bzw. anzeigen.
  - [ ] Rollen und Resolution Orders vollständig pflegen.
  - [ ] Fallback-Auswirkung vor Änderungen visualisieren.
  - [ ] Heading- und Content-Validierung unmittelbar und nach Serverantwort darstellen.
  - [ ] REST-Endpunkte und OpenAPI für Rollen- und Contentpflege vervollständigen.

Abnahme: Rollenabhängiger Markdown-Content ist vollständig ohne Agent und ohne Wegwerfeditor pflegbar.

## M6 – Bilder und Assetverwaltung

Abhängigkeit: M5. Referenz: [Bilder und Assets](konzept/03-redaktion-und-assets.md#bilder-und-assets)

- [ ] M6 abschließen
  - [ ] Entschiedenes Asset-Datenmodell und notwendige Migrationen implementieren.
  - [ ] Immutable Speicherung, Hash-Deduplizierung und Metadatenvalidierung implementieren.
  - [ ] Upload, Download und kontrollierte Asset-URLs bereitstellen.
  - [ ] Upload, Drag-and-drop und Zwischenablage im Rich-Text-Editor integrieren.
  - [ ] Markdown-Assetreferenzen roundtrip-sicher speichern.
  - [ ] Assetauflösung in Browser, REST, MCP und Markdown-Export vereinheitlichen.
  - [ ] Verwaiste, referenzierte und historische Assets korrekt behandeln.
  - [ ] Größen-, Typ-, Bilddimensions- und Schadinhaltgrenzen testen.

Abnahme: Bilder sind versionierbar, dedupliziert, historisch reproduzierbar und in allen Ausgabekanälen konsistent.

## M7 – Redaktioneller Workflow

Abhängigkeit: M5; nutzt optional M6. Referenzen: [Redaktionelle Hinweise](konzept/03-redaktion-und-assets.md#redaktionelle-hinweise), [Agentenaufträge](konzept/03-redaktion-und-assets.md#agentenauftraege)

- [ ] M7 abschließen
  - [ ] Versionierungsmodell für Editorial Notes entscheiden und dokumentieren.
  - [ ] Editorial-Note-Datenmodell, Persistenz und Application Services implementieren.
  - [ ] TODO, Question, Review und AgentTask mit Statusworkflow implementieren.
  - [ ] Hinweise an Node und optional Rolle anzeigen und bearbeiten.
  - [ ] Arbeitslisten für TODOs, stale Content, fehlende Inhalte und Refactoring-Kandidaten bereitstellen.
  - [ ] MCP- und REST-Zugriff für redaktionelle Aufgaben implementieren.
  - [ ] Auflösung und zugehörige fachliche Transaction nachvollziehbar verbinden.

Abnahme: Mensch und externer Agent können redaktionelle Arbeit gezielt finden, bearbeiten und nachvollziehbar abschließen.

## M8 – Publikationsprofile und PDF

Abhängigkeit: M6 und M7. Referenzen: [Publikationsprofil](konzept/04-publikation-und-pdf.md#publikationsprofil), [Pipeline](konzept/04-publikation-und-pdf.md#pipeline), [Freigabe](konzept/04-publikation-und-pdf.md#freigabe)

- [ ] M8 abschließen
  - [ ] Versioniertes Publikationsprofil und Profilassets modellieren.
  - [ ] Rollenbezogene Selektion für einen oder mehrere Teilbäume implementieren.
  - [ ] Neutrales Dokumentmodell erzeugen.
  - [ ] Layout, Logo, Fonts, Deckblatt, TOC, Header/Footer und Seitenzahlen implementieren.
  - [ ] HTML-Vorschau für Working Transaction bereitstellen und kennzeichnen.
  - [ ] Offiziellen PDF-Export auf committed Snapshot/Release begrenzen.
  - [ ] Stale-, Finding- und Editorial-Note-Freigaberegeln implementieren.
  - [ ] Exportprotokoll mit Snapshot, Release, Profil- und Assetrevisionen erzeugen.
  - [ ] Reproduzierbarkeit durch deterministische Vergleichstests nachweisen.

Abnahme: Ein Endkunden-Teilbaum lässt sich mit einem Klick reproduzierbar im definierten Corporate Layout als PDF erzeugen.

## M9 – Betriebs- und Qualitätshärtung

Abhängigkeit: M1 bis M8. Referenzen: [Betriebsabnahme](konzept/06-betrieb-sicherheit-und-risiken.md#betriebsabnahme), [Risiken](konzept/06-betrieb-sicherheit-und-risiken.md#risiken-und-gegenmaßnahmen)

- [ ] M9 abschließen
  - [ ] Vollständige Browser-End-to-End-Tests für zentrale Lese-, Schreib- und Publikationsabläufe.
  - [ ] Transportübergreifende REST-/MCP-Vertragstests vervollständigen.
  - [ ] Reale tiefe und breite Wissensbäume messen; Lazy Loading und Virtualisierung nachweisen.
  - [ ] Gleichzeitige UI-, REST- und MCP-Transactions sowie `SnapshotConflict` testen.
  - [ ] Deployment hinter vorgesehenem Reverse Proxy mit WebSockets und MCP-Streaming abnehmen.
  - [ ] Host-, Port-, Firewall-, TLS- und CORS-Konfiguration dokumentieren und prüfen.
  - [ ] Backup-/Restore- und Prozessneustart-Szenarien inklusive Assets prüfen.
  - [ ] Vollständige Build-, Linter-, FastTest- und erforderliche Integrationstest-Gates grün abschließen.
  - [ ] Verbindliche `docs/` vollständig gegen implementierten Ist-Stand prüfen.

Abnahme: Der definierte Intranetbetrieb ist reproduzierbar deploybar, performant und gegen die bekannten Risiken getestet.

## Separate spätere Vorhaben

Nicht als Checkboxen dieser Roadmap ausführen:

- Authentifizierung, Autorisierung, ACL, Audit und Mandantenmodell.
- Direkter Endkundenzugang.
- Integrierte Agentenorchestrierung und Semantic Kernel.
- Semantic Search und Embeddings.
- Kollaboratives Live-Editing oder automatisches Merge/Rebase.
- Presentation Views mit alternativen Navigationsstrukturen.
