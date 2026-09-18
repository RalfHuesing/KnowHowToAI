# M1 – Gemeinsamer Webhost und MCP HTTP

[Roadmap-Index](../Roadmap.md)

- [x] **M1 abschließen**

Abhängigkeit: [M0](00-komponenten-und-architektur.md)

Verbindliche M0-Basis: das offizielle Paket `ModelContextProtocol.AspNetCore`, ein direkter Kestrel-Origin, `/mcp` stateless, kein Legacy-SSE, kein zweiter Port und kein Reverse Proxy. Für Tests gelten bUnit mit xUnit v3, Microsoft.Playwright .NET und die installierte aktuelle Google-Chrome-Stable-Version ausschließlich mit `Channel = "chrome"` und `Headless = true`. Die konkrete Abhängigkeitsauswahl folgt der Regel im [Strukturkonzept](../konzept/08-projektstruktur-und-codekonventionen.md). Die Referenz-Fixtures unter `temp/webfrontend-spikes/` bleiben uncommittet und werden nicht in Produktions- oder Testprojekte übernommen.

Ziel: `KnowHowToAI.Server` stellt Blazor und die bestehenden MCP-Funktionen zentral über HTTP in einer EXE und auf einem Port bereit.

Referenzen: [Zielbild](../konzept/05-architektur-api-und-mcp.md#zielbild-des-ersten-schritts), [Blazor-interne Aufrufe](../konzept/05-architektur-api-und-mcp.md#blazor-interne-aufrufe), [MCP-Transport](../konzept/05-architektur-api-und-mcp.md#mcp-transport), [Projektstruktur](../konzept/05-architektur-api-und-mcp.md#projekt--und-namespace-struktur)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## M1.1 – ASP.NET-Core-Host

- [x] **M1.1 abschließen**

  - [x] **M1.1-T1 – Serverprojekt auf Webhost umstellen**
    - Projektänderung: `KnowHowToAI.Server.csproj` auf `Microsoft.NET.Sdk.Web` umstellen und nur dadurch redundant gewordene Framework-Paketreferenzen entfernen. Alle weiterhin benötigten Versionen bleiben zentral in `Directory.Packages.props`; das Lizenzinventar wird im selben Commit aktualisiert.
    - Composition Root: `Program` verwendet `WebApplication.CreateBuilder(args)` und liefert für Integrationstests weiterhin eine interne, deterministisch aufrufbare Builder-/Factory-Grenze. Bestehende Options-, Serilog-, Storage-, Application-, Migration- und vorläufige STDIO-MCP-Registrierungen werden unverändert in den Webhost übernommen; STDIO wird erst in M1.4 entfernt.
    - Prozessverhalten: vor dem Listen erfolgen Optionsvalidierung und aktivierte Migrationen. Konfigurations-/Migrationsfehler führen weiterhin zu `ServerExitCodes.StartupFailure`; Cancellation und geordneter Shutdown zu `Success`. Es entsteht keine zweite Hostklasse neben dem tatsächlich gestarteten Host.
    - Erhalten: bestehende `DatabaseConnection`-Sektion einschließlich SQL-Authentifizierung aus der einzigen `appsettings.json`, Konfigurationsvalidierung, SQL-Migrationen, Logging, Exitcodes und Application-/Repository-Registrierungen.
    - Nicht enthalten: Windows-Servicekonto, gMSA, Secret Provider oder andere produktive Credential-Infrastruktur; diese Betriebswahl gehört in M6.0.
    - Tests: reale Kestrel-Bindung auf dynamischem Loopback-Port mit deaktivierter Migration; Optionsfehler vor Bindung; bestehende Hosted-Service-Tests für Migrationserfolg/-fehler; Start plus Cancellation plus belegte Portfreigabe. Testkonfiguration erfolgt per In-Memory-/Environment-Override, nie über eine zweite Appsettings-Datei.
    - Dokumentation: `docs/Architektur.md` und `docs/Konfiguration-und-Betrieb.md` nur auf den bereits erreichten Webhost-Ist-Stand umstellen; STDIO bleibt dort bis M1.4 als aktueller Transport dokumentiert.
    - Abnahme: bestehende fachliche Tests bleiben grün; derselbe Serverprozess ist ein funktionsfähiger Webhost, ohne Blazor- oder HTTP-MCP-Funktionsausbau.

  - [x] **M1.1-T2 – Endpunktrouting und Hostkonfiguration absichern**
    - Struktur: `Web/WebEndpointRegistration.cs` als einzige zentrale Mapping-Erweiterung anlegen. In diesem Task reserviert sie `/api` und `/api/{**reservedPath}` für die üblichen HTTP-Methoden mit leerer `404`-Antwort; es wird keine REST-/OpenAPI-Infrastruktur angelegt. `/mcp` wird noch nicht gemappt und bleibt bis M1.3 ebenfalls `404`.
    - Hostkonfiguration: genau ein normaler ASP.NET-Core-Origin; Scheme, Adresse und Port kommen aus Standard-Hostkonfiguration/Command Line. Keine eigene Portoption, kein zweiter Listener, kein Proxy, kein CORS und keine vorgezogene Authentifizierung.
    - Mappingregel: technische Endpunkte werden vor der Blazor-Komponentenroute gemappt. Reservierte `/api`- und `/mcp`-Pfade dürfen nie HTML der UI oder eine Blazor-Not-Found-Seite liefern.
    - Tests: echte HTTP-Smokes gegen Kestrel für `/`, `/mcp`, `/api`, `/api/test` und eine unbekannte Route; zu diesem Zwischenstand sind `404` für alle fünf zulässig, aber `/api...` muss durch die explizite Reservierung beantwortet werden. Ein zweiter Start mit explizitem dynamischem URL-Override belegt, dass keine konkurrierende Portkonfiguration existiert.
    - Nicht enthalten: eigene Request-Limits, Timeouts, Compression oder Reverse-Proxy-Einstellungen; diese werden erst bei einem konkreten Endpunkt-/Betriebsbedarf eingeführt.
    - Abnahme: Mapping-Reihenfolge und reservierte Präfixe sind strukturell und automatisiert belegt; `WebEndpointRegistration` enthält keine Fachlogik.

## M1.2 – Blazor-Grundhost

- [x] **M1.2 abschließen**

  - [x] **M1.2-T1 – Blazor-Interactive-Server-Shell bereitstellen**
    - Dateien: `WebServiceRegistration`, `WebEndpointRegistration`, `Web/Components/App.razor`, `Routes.razor`, `_Imports.razor`, ein minimales Layout und genau eine Root-Seite gemäß Zielstruktur anlegen. Services mit `AddRazorComponents().AddInteractiveServerComponents()` registrieren und Komponenten mit Interactive Server unter `/` mappen.
    - Testprojekte: `KnowHowToAI.Web.Tests` mit bUnit und xUnit v3 sowie `KnowHowToAI.BrowserTests` mit Microsoft.Playwright .NET anlegen. Beide werden in `KnowHowToAI.slnx` und das jeweils zuständige Testskript aufgenommen. `Web.Tests` referenziert ausschließlich Server/Core; `BrowserTests` referenziert kein Produktionsprojekt und startet die veröffentlichte Server-EXE als Black Box.
    - Inhalt: semantische Überschrift „KnowHowToAI“, technischer Status der Shell, ein rein lokaler Button „Interaktivität prüfen“ mit `aria-live`-Status und ein realer read-only Aufruf von `NavigationService.ListRolesAsync(new ListRolesQuery(new ReadContext(), Limit: 1), cancellationToken)`. Es wird nur Erfolg/leer/Fehler dargestellt, keine Rollenliste, Dashboardlogik oder fachliche Navigation vorgezogen.
    - Fehlergrenze: unerwartete Renderfehler werden durch eine zentrale Error Boundary mit neutralem deutschen Text und Korrelationshinweis abgefangen; Exceptiondetails oder Credentials gelangen nicht ins Markup. Erwartete `Result`-Fehler werden als normaler Seitenzustand dargestellt.
    - Direkte DI: die Razor-Komponente verwendet den Application Service direkt; kein `HttpClient`, kein Loopback, kein Repository und kein SQL-Typ im Webnamespace.
    - Nicht enthalten: Designsystem, finale Navigation oder Fachseiten.
    - Tests: schneller bUnit-Render-/Erfolg-/Leer-/Fehler-Smoke mit Test Double an der vorhandenen Core-Portgrenze. Der Browser-Smoke veröffentlicht den Server in einen frischen Temp-Ordner, verwendet diesen als Content Root, startet genau einen realen Circuit in Google Chrome Stable (installierte aktuelle Version) mit `Channel = "chrome"` und `Headless = true` und wartet ausschließlich auf beobachtbare HTTP-/Circuitzustände. Er zeichnet ausgehende HTTP-Verbindungen auf und belegt, dass kein Server-Loopback stattfindet; Hostprozess und Port sind auch bei Fehlschlag freizugeben.
    - Abnahme: `/` liefert `200 text/html`, Framework-/Static-Assets liefern den passenden Erfolgscode, der lokale Statuswechsel belegt den Circuit, der read-only Application-Aufruf wird beim Initialisieren genau einmal ausgeführt und `/api...` sowie `/mcp` bleiben außerhalb der UI. Beide neuen Testprojekte laufen zweimal hintereinander grün.

## M1.3 – MCP Streamable HTTP

- [x] **M1.3 abschließen**

  - [x] **M1.3-T1 – MCP-HTTP-Transport produktiv integrieren**
    - Paket: die beim Umsetzen aktuelle stabile, kompatible Version von `ModelContextProtocol.AspNetCore` zentral aufnehmen; direkte Basispakete nur behalten, wenn sie danach noch direkt verwendet werden. `THIRD-PARTY-NOTICES.md` im selben Commit aus dem tatsächlichen Restore aktualisieren.
    - Registrierung: vorhandene `.WithToolsFromAssembly()`-Discovery und Toolklassen weiterverwenden, `WithHttpTransport` ausdrücklich mit `SessionMode = Stateless` konfigurieren und `EnableLegacySse = false` beibehalten. `/api` bleibt explizit reserviert; Static Assets und Razor-Komponenten werden gemäß [bestätigter Mappingreihenfolge](../konzept/05-architektur-api-und-mcp.md#mcp-transport) registriert, `MapMcp("/mcp")` beantwortet ausschließlich den MCP-Pfad. Legacy-SSE, stateful/hybride Sessions, CORS und zusätzlicher MCP-Port bleiben deaktiviert.
    - Erhalten: Tool-Namen, Schemas, Envelopes, Fehler- und Warnverträge.
    - Integrationsfixture: realen Kestrel-Host auf dynamischem Loopback-Port starten und ausschließlich `McpClient` plus `HttpClientTransport` des offiziellen SDK mit `TransportMode = StreamableHttp` verwenden. Keine direkten Endpointaufrufe und kein selbstgebautes JSON-RPC im Vertragstest.
    - Tests: SDK-Discovery, vollständige Tool Discovery mit Namen und Inputschema, `list_roles` als Read, kompletter `begin_transaction`/repräsentative Mutation/`discard_transaction`-Writepfad, strukturierter Parameter-/Fachfehler, zwei parallele Requests, beobachteter erster Response-Byte als Streamingnachweis und gezielter Clientabbruch bis zum Tool-`CancellationToken`. DI wird über kontrollierte Test Doubles an vorhandenen Ports isoliert; die HTTP-Grenze bleibt real.
    - Abnahme: alle bestehenden MCP-Kategorien sind über `/mcp` erreichbar; `GET /mcp` und `/mcp/sse` liefern keinen MCP-Erfolg, der Transport setzt keinen fachlichen Sessionzustand voraus, der abgebrochene Aufruf beendet nicht den parallelen Erfolgsaufruf und `/`, `/api...` bleiben unverändert.

  - [x] **M1.3-T2 – MCP-Vertragsregression vollständig abdecken**
    - Umbau: die Assertions der bestehenden `StdioProtocolTests` in HTTP-Vertragstests gegen das gemeinsame M1.3-T1-Fixture überführen. Tests senden keine Hand-JSON-Nachrichten mehr, starten keinen STDIO-Prozess und prüfen keine Zeilenrahmung; fachliche Mapper-/Tool-Unit-Tests bleiben unverändert.
    - Vertragsmatrix: Toolmenge und Schemas; Current/Snapshot/Transaction/Release-Read-Context; Paging und ungültige Cursor; Root/Node/Children/Rollen; Suche und Markdownexport; Begin/Commit/Discard; Node-, Rollen- und Contentmutationen; Historie/Diff/Release; Warnungen; repräsentative stabile Fehlercodes für Parameter-, Not-found-, Zustands- und Konfliktfehler sowie Cancellation.
    - Assertions: mindestens Toolname, relevante Schema-Pflichtfelder, Envelopeform, `data`, `warnings`, stabiler Fehlercode und relevante strukturierte Details prüfen. Freie Meldungstexte, Property-Reihenfolge und SDK-interne HTTP-Frames werden nicht festgezurrt.
    - Duplikationsregel: fachliche Varianten verbleiben in bestehenden Tool-/Core-Tests. Pro HTTP-Boundary genügt je Vertragsform ein repräsentativer Erfolgs-/Fehlerfall; die Matrix verweist auf vorhandene Detailtests, statt sie zu kopieren.
    - Abnahme: HTTP weist vollständige fachliche Parität zum bisherigen MCP-Vertrag nach; kein neuer Test setzt STDIO voraus und alle Kategorien sind in einer lesbaren Matrix im Testcode oder der Ist-Dokumentation nachvollziehbar.

## M1.4 – Hard Cut von STDIO

- [x] **M1.4 abschließen**

  - [x] **M1.4-T1 – STDIO-Transport entfernen und Ist-Dokumentation umstellen**
    - Voraussetzung: M1.3 vollständig abgenommen.
    - Umfang: `StdioHostRunner`, `.WithStdioServerTransport()`, STDIN/STDOUT-Protokollpfade, STDIO-Prozesstests, ausschließlich transportgebundene Helfer und alle Start-/Deploymentanweisungen dafür entfernen. Generisches Prozess-Exitcode-, Logging-, Options- und Shutdownverhalten bleibt erhalten und wird nötigenfalls transportneutral benannt.
    - Pakete: nicht mehr direkt benötigte STDIO-/Basispaketreferenzen entfernen, zentralen Paketgraph und `THIRD-PARTY-NOTICES.md` aktualisieren. Das ASP.NET-Core-MCP-Paket und der offizielle Client für Tests bleiben.
    - Erhalten: transportneutrale MCP-Contracts, Mapper, Tools und Application Services.
    - Negativsuche: Repositoryweit `Stdio`, `STDIO`, `stdin`, `stdout`, `WithStdioServerTransport` sowie alte CLI-/Pipe-Startbeispiele prüfen. Historische Planungsbegründungen dürfen STDIO erwähnen; ausführbarer Code, Konfiguration und Ist-Dokumentation nicht.
    - Dokumentation: `README.md` nur in diesem ausdrücklich dafür vorgesehenen Task sowie `docs/Architektur.md`, `docs/McpApi.md`, `docs/Konfiguration-und-Betrieb.md`, `docs/Entscheidungen.md` und betroffene Lese-Matrix-Einträge vollständig auf eine EXE, einen konfigurierten Origin und `/mcp` umstellen.
    - Abschlussprüfung: Server als veröffentlichtes Artefakt starten; `/` und `/mcp` prüfen; vollständige Fast- und Integrationstests sowie Linter-Gate ausführen. Optional vorhandenes Hermes darf nur manuell zusätzlich geprüft werden und ist kein Abnahmekriterium.
    - Abnahme: Repository und ausgeliefertes Produkt enthalten keinerlei STDIO-Unterstützung, Fallback oder Kompatibilitätsmodus; Streamable HTTP ist der einzige MCP-Transport und alle Quality-Gates sind grün.

## Milestone-Abnahme

- Eine EXE bedient auf einem Port Blazor-Shell, notwendige Web-Endpunkte und stateless MCP HTTP.
- Bestehende MCP-Funktionen sind fachlich erhalten und automatisiert über den offiziellen SDK-Client gegen den realen HTTP-Host geprüft.
- Streamable HTTP ist der einzige MCP-Transport; STDIO ist vollständig und ohne Fallback entfernt.
