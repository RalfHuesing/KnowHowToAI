# M1 – Gemeinsamer Webhost und MCP HTTP

[Roadmap-Index](../Roadmap.md)

- [ ] **M1 abschließen**

Abhängigkeit: [M0](00-komponenten-und-architektur.md)

Ziel: `KnowHowToAI.Server` stellt Blazor und die bestehenden MCP-Funktionen zentral über HTTP in einer EXE und auf einem Port bereit.

Referenzen: [Zielbild](../konzept/05-architektur-api-und-mcp.md#zielbild-des-ersten-schritts), [Blazor-interne Aufrufe](../konzept/05-architektur-api-und-mcp.md#blazor-interne-aufrufe), [MCP-Transport](../konzept/05-architektur-api-und-mcp.md#mcp-transport), [Projektstruktur](../konzept/05-architektur-api-und-mcp.md#projekt--und-namespace-struktur)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## M1.1 – ASP.NET-Core-Host

- [ ] **M1.1 abschließen**

  - [ ] **M1.1-T1 – Serverprojekt auf Webhost umstellen**
    - Umfang: `Microsoft.NET.Sdk.Web`, `WebApplication`, Composition Root und geordneten Start/Stop einführen.
    - Erhalten: bestehende `DatabaseConnection`-Sektion einschließlich SQL-Authentifizierung aus der einzigen `appsettings.json`, Konfigurationsvalidierung, SQL-Migrationen, Logging, Exitcodes und Application-/Repository-Registrierungen.
    - Nicht enthalten: Windows-Servicekonto, gMSA, Secret Provider oder andere produktive Credential-Infrastruktur; diese Betriebswahl gehört in M6.0.
    - Tests: Hoststart, Konfigurationsfehler, Migrationserfolg/-fehler und Shutdown.
    - Abnahme: bestehende fachliche Tests bleiben grün; Host läuft ohne Blazor- oder MCP-Funktionsausbau.

  - [ ] **M1.1-T2 – Endpunktrouting und Hostkonfiguration absichern**
    - Umfang: gemeinsamen Origin und Port, feste Route `/mcp` und reserviertes `/api` konfigurieren.
    - Prüfen: Fallback verschluckt keine reservierte Route; Limits, Streaming und Timeouts sind endpunktbezogen konfigurierbar.
    - Tests: Routing-Smokes für `/`, `/mcp` und reserviertes `/api`.
    - Abnahme: Routingregeln aus dem Konzept sind automatisiert belegt.

## M1.2 – Blazor-Grundhost

- [ ] **M1.2 abschließen**

  - [ ] **M1.2-T1 – Blazor-Interactive-Server-Shell bereitstellen**
    - Umfang: minimale Shell unter `/`, Web-Namespace, Fehlergrenze und direkte DI-Anbindung an einen read-only Application Service.
    - Nicht enthalten: Designsystem, finale Navigation oder Fachseiten.
    - Tests: Render-Smoke, Circuit-Erstellung und Nachweis ohne internen HTTP-Loopback.
    - Abnahme: Browser erreicht eine funktionierende serverinteraktive Shell.

## M1.3 – MCP Streamable HTTP

- [ ] **M1.3 abschließen**

  - [ ] **M1.3-T1 – MCP-HTTP-Transport produktiv integrieren**
    - Umfang: offizielles ASP.NET-Core-MCP-Paket registrieren und stateless Streamable HTTP auf `/mcp` mappen.
    - Erhalten: Tool-Namen, Schemas, Envelopes, Fehler- und Warnverträge.
    - Tests: Initialisierung, Tool Discovery, je ein repräsentativer Read/Write, Streaming und Fehlerfälle über HTTP.
    - Abnahme: alle bestehenden MCP-Kategorien sind über HTTP erreichbar.

  - [ ] **M1.3-T2 – MCP-Vertragsregression vollständig abdecken**
    - Umfang: bestehende STDIO-Vertragstests transportneutral machen oder durch HTTP-Tests ersetzen, ohne Fachtests zu duplizieren.
    - Prüfen: Paging, Cursor, Read Context, Transactions, Mutationen, Export, Historie und Fehlerkatalog.
    - Abnahme: HTTP weist fachliche Parität zum bisherigen MCP-Vertrag nach; kein neuer Test setzt STDIO voraus.

  - [ ] **M1.3-T3 – Reale Zielclients abnehmen**
    - Voraussetzung: O-019 legt die verbindlichen MCP-Zielclients und Versionen fest.
    - Umfang: mindestens die tatsächlich verwendeten MCP-Clients gegen den zentralen HTTP-Endpunkt testen.
    - Prüfen: Konfiguration, Verbindung, Tool Discovery, langer Read, Transaction-Workflow und Fehlermeldungen.
    - Ergebnis: clientspezifische Konfiguration und bestätigte Einschränkungen dokumentieren.
    - Abnahme: der geplante tägliche Agentenzugriff funktioniert ohne lokalen Child Process.

## M1.4 – Hard Cut von STDIO

- [ ] **M1.4 abschließen**

  - [ ] **M1.4-T1 – STDIO-Transport entfernen und Ist-Dokumentation umstellen**
    - Voraussetzung: M1.3 vollständig abgenommen.
    - Umfang: STDIO-Runner, Startmodus, Konfiguration, Pakete, Deployment-/Startanweisungen und ausschließlich transportgebundene Tests entfernen.
    - Erhalten: transportneutrale MCP-Contracts, Mapper, Tools und Application Services.
    - Dokumentation: Architektur, MCP-API, Konfiguration und Betrieb vollständig auf HTTP aktualisieren.
    - Abnahme: Repository und ausgeliefertes Produkt enthalten keinerlei STDIO-Unterstützung, Fallback oder Kompatibilitätsmodus; Build, Tests und Linter sind grün.

## Milestone-Abnahme

- Eine EXE bedient auf einem Port Blazor-Shell, notwendige Web-Endpunkte und stateless MCP HTTP.
- Bestehende MCP-Funktionen sind fachlich erhalten und mit realen Zielclients geprüft.
- Streamable HTTP ist der einzige MCP-Transport; STDIO ist vollständig und ohne Fallback entfernt.
