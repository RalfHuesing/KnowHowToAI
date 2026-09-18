# M0 – Komponenten- und Architekturentscheidungen

[Roadmap-Index](../Roadmap.md)

- [x] **M0 abschließen**

Ziel: Technische Risiken und produktprägende Fremdkomponenten sind vor der eigentlichen Webimplementierung anhand realistischer Anforderungen entschieden.

Referenzen: [Komponentenstrategie](../konzept/02-bedienkonzept-und-ui.md#komponentenstrategie), [Offene Fragen](../konzept/07-entscheidungen-und-offene-fragen.md), [Ein Prozess und ein Port](../konzept/05-architektur-api-und-mcp.md#ein-prozess-und-ein-port)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../konzept/08-projektstruktur-und-codekonventionen.md)

## Verbindliches Spikeverfahren

Für M0.2 und M0.3 gilt zusätzlich:

1. Wegwerfcode liegt ausschließlich unter `temp/webfrontend-spikes/<Task-ID>/`. Das Verzeichnis ist bereits per `.gitignore` ausgeschlossen. Der Agent verändert dafür weder die Solution noch zentrale Paketdateien oder Produktionsprojekte.
2. Bei Fremdkomponenten wird die am Ausführungstag aktuelle stabile Version verwendet und mit Prüfdatum, Paketquelle, Quellrepository und Lizenz festgehalten. Preview-, Beta- und Release-Candidate-Versionen sind ausgeschlossen; eine ausdrücklich als Beta bezeichnete Teilfunktion wird als Produktrisiko bewertet.
3. Der Agent verwendet nur die im Task genannte Kandidatenmenge. Er beginnt keine allgemeine Marktanalyse und ergänzt keine weiteren Bibliotheken. Ein Kandidat darf nach einem belegten Knock-out abgebrochen werden; die übrigen Knock-out-Kriterien werden dann als „nicht mehr geprüft“ markiert.
4. Jeder Spike verwendet dieselben taskinternen Fixtures für alle Kandidaten. Befehle, Fixture, aufgelöste Versionen, Messergebnisse, erfüllte und nicht erfüllte Kriterien sowie verworfene Kandidaten werden im fachlich zuständigen Konzept dokumentiert. Die Fassung der Roadmap schreibt diese Versionsnummern nicht für Folgeumsetzungen vor.
5. Direkte und transitive Lizenzen werden gegen M0.1-T2 geprüft. Cloud-, Telemetrie- und CDN-Zwang, kostenpflichtige Funktionen, nicht redistribuierbare Bestandteile sowie ein notwendiger Paketfork sind Knock-outs.
6. Aus Spike-Code wird kein Produktionscode übernommen. Nach dem dokumentierten Ergebnis wird nur das konkrete Spikeverzeichnis entfernt. Produktive Pakete werden erst im zuständigen Umsetzungstask aufgenommen.
7. Erfüllt kein zulässiger Kandidat alle Musskriterien, bleibt der Task offen. Der Agent dokumentiert die genaue Lücke und fragt den Benutzer, statt Kriterien abzuschwächen oder selbst einen neuen Kandidaten einzuführen.

## M0.1 – Ausgangslage

- [x] **M0.1 abschließen**

  - [x] **M0.1-T1 – Build-, Test- und Linter-Baseline nachweisen**
    - Vorbedingung: Working Tree mit `git status --short` prüfen. Fremde Änderungen weder aufnehmen noch bereinigen; bei Überschneidung den Benutzer fragen.
    - Befehle in dieser Reihenfolge: `dotnet --info`, `dotnet restore KnowHowToAI.slnx`, `dotnet build KnowHowToAI.slnx --no-restore`, `pwsh scripts/test-fast.ps1`, `pwsh scripts/test-integration.ps1` und AiNetLinter `verify` für `KnowHowToAI.slnx` mit `scope: "solution"`.
    - Aufzeichnung: Datum, .NET-SDK-/Runtimeversion, SQL-Preflight-Ergebnis, Exitcode und Laufzeit jedes Befehls sowie beim Linter `verdict`, `score` und `violationCount` direkt als kurzer `Nachweis`-Block unter diesem Task ergänzen. Keine Credentials, Connection Strings oder vollständigen Umgebungsvariablen protokollieren.
    - Abnahme: alle Befehle sind grün; der Linter meldet ausschließlich `verdict=pass`, `score=10.0` und `violationCount=0`. Eine vorbestehende Abweichung wird nicht in diesem Task repariert: Befund dokumentieren, Task offen lassen und den Benutzer auf einen separaten Fix-Task verweisen.
    - Nicht enthalten: Paketupdates, Refactoring oder Webänderungen.
    - Abschluss: betroffene Ist-Dokumentation nur bei tatsächlicher Änderung aktualisieren; Task und dadurch tatsächlich erfüllte Parentstatus committen.

    **Nachweis (2026-09-18):** .NET SDK, Host-Runtime und `Microsoft.NETCore.App` waren verfügbar. SQL-Preflight erfolgreich (Verbindung zur manuell bereitgestellten Konfiguration).

    | Befehl | Exitcode | Laufzeit | Ergebnis |
    |---|---:|---:|---|
    | `dotnet --info` | 0 | 0,276 s | grün |
    | `dotnet restore KnowHowToAI.slnx` | 0 | 0,911 s | grün |
    | `dotnet build KnowHowToAI.slnx --no-restore` | 0 | 4,947 s | grün, 0 Warnungen, 0 Fehler |
    | `pwsh scripts/test-fast.ps1` | 0 | 6,575 s | grün, 628 Tests |
    | `pwsh scripts/test-integration.ps1` | 0 | 13,693 s | grün, SQL-Preflight und 31 Tests |
    | AiNetLinter `verify` (`scope: solution`) | n/a (MCP) | 2,831 s | `verdict=pass`, `score=10.0`, `violationCount=0` |

  - [x] **M0.1-T2 – MIT-Lizenz und Abhängigkeitsbaseline herstellen**
    - Ausgangslage: Die MIT-`LICENSE` mit `Copyright (c) 2026 Ralf Hüsing` ist im Repository-Root vorhanden.
    - Ermittlung: nach einem Restore für jedes Projekt direkte und transitive NuGet-Pakete ausgeben; zentrale Versionen und tatsächlich aufgelöste Versionen gegeneinander prüfen. Runtime-/Shared-Framework, nur zur Entwicklung verwendete Test-/Buildpakete und mit der Anwendung ausgelieferte Pakete getrennt kennzeichnen.
    - Lizenzprüfung: Paketmetadaten und beigefügte Lizenz-/NOTICE-Dateien aus der tatsächlich restaurierten Paketversion verwenden; bei Unklarheit das jeweilige offizielle Quellrepository hinzuziehen. Suchmaschinen-Snippets oder der Lizenztyp eines übergeordneten Projekts genügen nicht als Nachweis für transitive Pakete.
    - Ergebnis: `THIRD-PARTY-NOTICES.md` enthält je Abhängigkeit Name, aufgelöste Version, direkte/transitive Verwendung, Produktions-/Entwicklungsumfang, Quelle, SPDX-Ausdruck oder ausgeschriebenen Lizenztyp und konkret einzuhaltende Copyright-/Lizenz-/NOTICE-Pflichten. Identische transitive Pakete werden einmal inventarisiert; erforderliche Originaltexte werden eindeutig referenziert oder beigefügt.
    - Dauerregel: am Dokumentanfang die reproduzierbaren Inventarbefehle und die Pflicht festhalten, das Inventar bei jeder Abhängigkeitsänderung im selben Commit zu aktualisieren.
    - Abnahme: `LICENSE` ist unveränderter MIT-Text mit dem genannten Copyright; kein Paket ist kostenpflichtig, lizenzseitig ungeklärt oder mit der MIT-Distribution unvereinbar. Nicht permissive oder unklare Lizenzen werden nicht eigenmächtig akzeptiert: Task offen lassen und Benutzerentscheidung anfordern.
    - Abschluss: ausschließlich Lizenz-/Inventardateien, Roadmapstatus und zwingende Dokumentationsverweise atomar committen; keine Paketversion allein zur Vereinfachung des Inventars ändern.

    **Nachweis (2026-09-18):** `LICENSE` ist unverändert der MIT-Text
    mit `Copyright (c) 2026 Ralf Hüsing`. Ein Restore und die Inventarisierung
    aller fünf `project.assets.json` sind in
    [`THIRD-PARTY-NOTICES.md`](../../../THIRD-PARTY-NOTICES.md) dokumentiert;
    alle zentralen Versionen entsprechen der tatsächlichen Auflösung. Der
    restaurierte Transitiv `Microsoft.Data.SqlClient.SNI.runtime`
    enthält jedoch Microsoft Software License Terms statt einer permissiven
    SPDX-Lizenz und verpflichtet unter anderem zu besonderen
    Weitergabe-/Endnutzerbedingungen, Freistellung und Exportbeachtung. Der
    Der Benutzer hat diese konkreten Microsoft Software License Terms am
    2026-09-18 ausdrücklich angenommen. Die Lizenz wird dabei nicht als
    permissiv eingeordnet; ihre Weitergabe-, Freistellungs- und Exportpflichten
    bleiben verbindlich und sind in `THIRD-PARTY-NOTICES.md` festgehalten. Es
    wurden keine Pakete geändert.

## M0.2 – Host- und Routing-Spike

- [x] **M0.2 abschließen**

  - [x] **M0.2-T1 – Blazor und MCP HTTP in einem Host validieren**
    - Fixture: isoliertes `net10.0`-Webprojekt mit `WebApplication`, einer minimalen Interactive-Server-Komponente unter `/` und dem aktuellen stabilen offiziellen Paket `ModelContextProtocol.AspNetCore`; ein zustandsloses Echo-Tool liegt unter `/mcp`. Kestrel bindet einen dynamischen Loopback-Port, beide Oberflächen verwenden exakt denselben Origin.
    - Prüfen: Shell per HTTP laden und genau einen echten Blazor-Circuit ausschließlich mit Headless Chrome öffnen; parallel über den offiziellen C#-SDK-Client mit ausdrücklich gewähltem Streamable-HTTP-Transport initialisieren, Tools auflisten und das Echo-Tool aufrufen. MCP wird explizit stateless konfiguriert; Legacy-SSE, zustandsbehaftete Sessions und zusätzliche Ports bleiben aus.
    - DI-/Lebenszyklusnachweis: instrumentierte scoped und singleton Services belegen erwartete Scopes ohne Zustandsübertragung zwischen zwei MCP-Requests oder zwischen MCP und Circuit. Requestabbruch erreicht den Tool-`CancellationToken`; der Host startet und stoppt dreimal ohne verbleibenden Prozess oder belegten Port.
    - Headless-Regel: kein sichtbarer oder interaktiver Browserstart. Die temporäre Browserautomation entscheidet kein Testframework vor M0.3-T4 und wird nicht in Produktionsprojekte übernommen.
    - Nicht enthalten: produktive Hostmigration, REST/OpenAPI, UI-Design oder STDIO-Entfernung.
    - Ergebnis: Paket-/Protokollversion, vollständige Mapping-Reihenfolge, relevante DI-Lifetimes, Start-/Stop-Befehle und Messung im [Architekturkonzept](../konzept/05-architektur-api-und-mcp.md) festhalten.
    - Abnahme: Circuit und MCP-Aufruf funktionieren gleichzeitig auf demselben Port; stateless Verhalten, Abbruch und saubere Beendigung sind automatisiert belegt.
    - Abschluss: Spike-Code wird nicht committed; produktive Umsetzung beginnt erst in M1.

    **Nachweis (2026-09-18):** Der isolierte Wegwerf-Spike unter
    `temp/webfrontend-spikes/M0.2-T1` verwendete `net10.0` und ausschließlich
    das am Prüftag aktuelle stabile `ModelContextProtocol.AspNetCore`-Paket
    von NuGet.org laut
    `https://api.nuget.org/v3-flatcontainer/modelcontextprotocol.aspnetcore/index.json`).
    Das Paket referenziert `ModelContextProtocol` und
    `ModelContextProtocol.Core` als kompatible SDK-Abhängigkeit; die Paketmetadaten weisen
    Apache-2.0 und das offizielle Quellrepository
    `https://github.com/modelcontextprotocol/csharp-sdk` (Paketcommit
    `6fa3825973949a9c4f0cd8af344e15a8db09dc35`) aus. Apache-2.0 ist im
    M0.1-T2-Inventar als kompatible, bereits dokumentierte Lizenz enthalten;
    weder der Spike noch sein transitive Paketgraph führen eine neue
    Lizenzentscheidung ein. Es wurden keine Preview-, Beta- oder RC-Pakete
    und keine weitere Kandidatenbibliothek verwendet.

    | Nachweis | Ergebnis |
    |---|---|
    | Host und Origin | Der veröffentlichte Kestrel-Host band dreimal dynamisch ausschließlich an `127.0.0.1` (`52649`, `55853`, `55877`). `GET /` lieferte jeweils `200`; Blazor und `/mcp` nutzten denselben Origin, ohne Zusatzport oder Proxy. |
    | Mapping-Reihenfolge | `AddRazorComponents().AddInteractiveServerComponents()`; Instrumentierungsdienste; `AddMcpServer().WithHttpTransport(...)`; `MapStaticAssets()`; `MapRazorComponents<App>().AddInteractiveServerRenderMode()`; `MapMcp("/mcp")`. Der Transport setzt `SessionMode = Stateless`; `EnableLegacySse` bleibt beim SDK-Default `false`. |
    | MCP-Protokoll | Streamable HTTP nach der vom Paket referenzierten Spezifikation `2025-11-25`; der SDK-Client `HttpClientTransport` setzte `TransportMode = StreamableHttp` ausdrücklich. Die aktuelle SDK-Initialisierung erfolgte über `server/discover` (statt des in neueren Revisionen entfernten `initialize`-Handshakes), danach `tools/list` und `tools/call` für `echo`. Ergebnis: `echo=same-origin`; die Tools `echo` und `wait_for_cancellation` wurden entdeckt. |
    | Headless-Circuit | Die am Prüftag installierte aktuelle Google-Chrome-Stable-Version mit `--headless=new` öffnete `/`. Ein instrumentierter `CircuitHandler` meldete exakt einen geöffneten Circuit; kein sichtbares Browserfenster und kein Testframework wurden verwendet. |
    | DI und Statelesness | `SingletonProbe` blieb in zwei getrennten MCP-Aufrufen identisch (`7c11ea1d-492d-4be3-b62e-dff6c9363658`). `ScopedProbe` war pro MCP-Request verschieden (`d2a9f2d3-a055-4d80-b40d-b420d72a2723`, `346b5958-3c65-4253-b55f-a37a9719dad3`) und unterschied sich zudem vom Circuit-Scope (`eab0ea3d-ea81-4f28-8847-cba21b55c3d8`). Damit fand keine Zustandsübertragung Request-zu-Request oder MCP-zu-Circuit statt. |
    | Kein Legacy-SSE | `GET /mcp/sse` ergab `404`; es gibt keine Session-, SSE- oder zusätzliche Portkonfiguration. |
    | Requestabbruch | Ein offizieller Streamable-HTTP-Client startete `wait_for_cancellation`; nach der beobachteten Startmarke brach er genau diesen Request per `CancellationToken` ab. Das Tool beobachtete denselben abgebrochenen Token automatisiert (`CANCELLATION=observed`). |
    | Lifecycle | Jeder der drei gestarteten Hosts wurde kontrolliert beendet; unmittelbar danach war der konkret gebundene Port nicht mehr im Listen-Zustand. Es blieb kein Hostprozess oder Port-Leak zurück. |

    Verwendete Befehle: `dotnet build`, `dotnet publish -c Release -o .\\publish`,
    `dotnet M0.2-T1.dll --urls http://127.0.0.1:0`, der offizielle Clientmodus
    `--client http://127.0.0.1:<Port>/mcp` sowie der abbruchprüfende Clientmodus
    `--cancel http://127.0.0.1:<Port>/mcp`. Alle Wartebedingungen beruhten auf
    beobachtbaren Host-, Circuit- oder Requestzuständen, nicht auf festen Sleeps.
    Der vollständige Spike wurde nach diesem Nachweis entfernt und nicht committed.

  - [x] **M0.2-T2 – Routing- und Transportmatrix verifizieren**
    - Fixture: den Spike aus M0.2-T1 verwenden; keine zweite Hostvariante und keinen Reverse Proxy hinzufügen.
    - Matrix: `/` liefert die Blazor-Shell; Framework-/Static-Asset-Routen bleiben erreichbar; `/mcp` akzeptiert ausschließlich die vom offiziellen SDK erzeugten zulässigen Streamable-HTTP-Aufrufe; `/api` und `/api/...` werden ausdrücklich reserviert und niemals von UI-Fallback oder MCP beantwortet; eine unbekannte UI-Route folgt allein der dokumentierten Blazor-Not-Found-Regel.
    - Parallelfall: während eines aktiven Circuits zwei parallele MCP-Aufrufe ausführen und einen davon abbrechen. Antworten, Content-Types, Streaming/Flush, Cancellation und fehlende Route-Kollision werden protokolliert; es werden keine festen Wartezeiten verwendet.
    - Konfiguration: Scheme, Adresse und Port stammen aus der normalen ASP.NET-Core-Hostkonfiguration. Es entsteht keine zweite Web-/MCP-Portoption und keine neue Proxykonfiguration.
    - Abgrenzung: Reverse-Proxy-Produktwahl, TLS-Terminierung, öffentliche Erreichbarkeit und betriebliche Proxy-Timeouts sind für M0–M2 nicht erforderlich und werden erst im manuellen M6.0-Gate entschieden. M0 belegt nur, dass WebSocket/Circuit und MCP-Streaming auf einem direkten Kestrel-Origin koexistieren.
    - Ergebnis: konkrete Routenmatrix, Mapping-Reihenfolge, reservierte Präfixe und nach M6 verschobene Betriebsannahmen im Architekturkonzept dokumentieren.
    - Abnahme: alle Matrixfälle sind automatisiert grün und der gemeinsame Kestrel-Port ist bestätigt. Ist das nicht möglich, bleibt der Task offen und der Agent fragt den Benutzer, statt auf mehrere Ports oder einen Proxy auszuweichen.

    **Nachweis (2026-09-18):** Der isolierte Wegwerf-Spike unter
    `temp/webfrontend-spikes/M0.2-T2` rekonstruierte ausschließlich den in
    M0.2-T1 nachgewiesenen `net10.0`-`WebApplication`-Host mit
    dem am Prüftag aktuellen stabilen `ModelContextProtocol.AspNetCore`-Paket, Interactive Server und
    stateless Streamable HTTP. Es gab keinen zweiten Host, keinen Proxy und
    keine neue Abhängigkeit oder Lizenzentscheidung. Der veröffentlichte
    Host band über die normale ASP.NET-Core-Hostkonfiguration
    `ASPNETCORE_URLS=http://127.0.0.1:0` dynamisch an genau einen
    Loopback-Origin `http://127.0.0.1:61594`. Google Chrome
    lief ausschließlich mit `--headless=new`; Circuit- und
    Tool-Startmarken steuerten alle Wartebedingungen ohne feste Sleeps.

    | Matrixfall | Automatisiertes Ergebnis |
    |---|---|
    | Mapping-Reihenfolge und Reservierungen | `AddRazorComponents().AddInteractiveServerComponents()`; Instrumentierung; `AddMcpServer().WithHttpTransport(SessionMode = Stateless).WithToolsFromAssembly()`; `UseAntiforgery()`; `/api` und `/api/{**reservedPath}` für alle üblichen HTTP-Methoden mit `404`; `/mcp` für Nicht-POST mit `405`; `MapStaticAssets()`; Razor-Komponenten einschließlich der Blazor-Catch-all-Not-Found-Seite; `MapMcp("/mcp")`. Damit können weder UI noch MCP das API-Präfix beantworten. |
    | Blazor und Assets | `GET /` ergab `200 text/html` mit `blazor-shell`; `GET /marker.txt` ergab `200 text/plain`; `GET /_framework/blazor.web.js` ergab `200 text/javascript`. Während der anschließenden MCP-Prüfung blieb der beobachtete Headless-Circuit aktiv. |
    | Reservierte und unbekannte Routen | `GET /api` und `GET /api/future` ergaben jeweils `404` ohne HTML-Shell oder MCP-Antwort. `GET /unknown-ui-route` ergab ausschließlich die dokumentierte Blazor-Not-Found-Seite (`200 text/html`, `route-not-found`). |
    | MCP-Zulässigkeit und Content-Type | Ein direkter `GET /mcp` ergab `405`; ein absichtlich ungültiger JSON-POST ergab `406 application/json`, nie die UI. Zwei offizielle C#-`HttpClientTransport`-Clients mit explizitem `TransportMode = StreamableHttp` führten `server/discover`, `tools/list` sowie `tools/call` aus; alle zulässigen SDK-POSTs ergaben `200 text/event-stream`. Die Beobachtung des ersten Response-Bytes belegte Flush/Streaming. |
    | Parallelität und Abbruch | Bei aktivem Circuit liefen `echo=same-origin` und `wait_for_cancellation` parallel. Nach der beobachteten Tool-Startmarke brach ausschließlich der zweite Clientrequest per `CancellationToken` ab; der Tool-Token meldete den Abbruch, der Server schloss diesen Stream mit `499 text/event-stream`, während `echo` erfolgreich mit `200 text/event-stream` endete. Es gab keine Routen- oder Portkollision. |

    Reverse Proxy, TLS-Terminierung, öffentliche Erreichbarkeit und
    betriebliche Proxy-Timeouts bleiben unverändert für das manuelle
    M6.0-Gate zurückgestellt. Der Spike und seine temporären Browserprofile
    wurden nach dem Nachweis entfernt und nicht committed.

## M0.3 – UI-Komponenten

- [x] **M0.3 abschließen**

  - [x] **M0.3-T1 – UI-Komponentenbasis auswählen**
    - Kandidaten und Reihenfolge: zuerst native Blazor-/HTML-/CSS-Basis. Erfüllt sie alle Musskriterien ohne wiederholte komplexe Eigenimplementierung, wird „keine allgemeine Komponentenbibliothek“ gewählt und der Paketvergleich endet. Nur andernfalls werden zusätzlich die aktuellen stabilen Versionen von `Microsoft.FluentUI.AspNetCore.Components` und `MudBlazor` mit demselben Fixture geprüft; andere Suites sind ausgeschlossen.
    - Fixture: eine kleine Interactive-Server-Seite mit Shellnavigation, beschriftetem Formular samt Validierung, modalem Bestätigungsdialog, kleiner Tabelle, Inlinehinweis und Toast sowie zentral überschreibbaren Farb-, Abstands- und Fokus-Tokens. Der Dialog muss per Tastatur geöffnet und geschlossen werden, Fokus einfangen und zum Auslöser zurückgeben.
    - Musskriterien: .NET 10 und Interactive Server; vollständig self-hosted ohne Cloud/CDN; O-021 für Semantik, Tastatur, sichtbaren Fokus, Kontrast und 200-%-Zoom; deterministisch per bUnit-artigem Komponentenfixture und Headless Chrome testbar; Light Theme mit den M2-Tokens anpassbar; keine erzwungene JavaScript-Buildtoolchain.
    - Messung: direkter und transitiver Paketgraph samt Lizenzpflichten, erforderliche Services/Assets/JS-Interop, Release-Publishgröße von `wwwroot` und Gesamtpublish jeweils gegen das native Fixture sowie notwendige Initialisierungs- und Themeeingriffe. Rohwerte werden dokumentiert; es gibt keinen erfundenen Maximalwert.
    - Auswahlregel: Nach Musskriterien gewinnt die native Basis. Eine Bibliothek wird nur gewählt, wenn sie einen im Fixture belegten, in M2 mehrfach benötigten Nutzen liefert und ihre zusätzliche Abhängigkeit sowie Betriebsoberfläche kleiner sind als die dadurch vermiedene Eigenimplementierung. Bei Gleichstand gilt: weniger Produktionsabhängigkeiten, dann weniger JS/Assets, dann kleinere Publishdifferenz.
    - Nicht enthalten: Knowledge Tree und Rich-Text-Editor; diese werden separat entschieden.
    - Ergebnisort: Auswahl und Integrationsleitplanken in `konzept/02-bedienkonzept-und-ui.md`; Lizenzbefund in `THIRD-PARTY-NOTICES.md`; O-001 aus `konzept/07-entscheidungen-und-offene-fragen.md` entfernen.
    - Abnahme: Entscheidung, Messwerte, Lizenzprüfung und verworfene Alternativen sind dokumentiert; Browserunterstützung erfüllt O-013.

    **Nachweis und Entscheidung (2026-09-18):** Gewählt ist **keine
    allgemeine Komponentenbibliothek**. Das ausschließlich native
    `net10.0`-Interactive-Server-Fixture unter
    `temp/webfrontend-spikes/M0.3-T1` erfüllt alle Musskriterien ohne
    wiederholte komplexe Eigenimplementierung: Shell-Navigation, gelabeltes
    `EditForm` mit Validierung, semantische Tabelle, Hinweis und Toast sind
    Standard-Blazor/HTML/CSS. Der native HTML-`dialog` wird mit einem kleinen
    eigenen Wrapper (`native-dialog.js`, 879 B) per `showModal()` geöffnet;
    dessen einmaliger Tastatur-Fokuskreislauf umfasst nur die lokalen
    Dialogbuttons und gibt beim nativen Schließen den Fokus an den Auslöser
    zurück. Es gibt keine Buildtoolchain, kein CDN, keine Cloudfunktion und
    keine aus dem Spike übernommene Produktdatei.

    | Prüfaspekt | Nachweis |
    |---|---|
    | .NET, Interaktivität und Self-Hosting | `dotnet build -c Release` für das Fixture: 0 Warnungen, 0 Fehler. `AddRazorComponents().AddInteractiveServerComponents()` und `@rendermode InteractiveServer`; ein dynamischer Loopback-Host lief vollständig aus dem frisch veröffentlichten Temp-Ordner. |
    | Komponentenfixture und O-021 | Die am Prüftag aktuelle stabile bUnit-Version prüfte zweimal (2/2) native Landmarks, Label, Tabellenkopfdaten, Dialogstruktur, Statusmeldung sowie den schmalen JS-Interop-Aufruf. Die zentralen überschreibbaren CSS-Tokens decken Oberfläche, Text, Primär-, Hinweis-, Abstand- und Fokusring ab. Berechnete Kontrastverhältnisse gegen Weiß: Text 17,74:1, Primärbutton 5,17:1, Fokus 6,70:1, Hinweis 10,31:1, Fehler 6,47:1, Toast 7,68:1. |
    | Headless-Chrome und O-013 | Zwei Läufe mit der am Prüftag installierten aktuellen Google-Chrome-Stable-Version, ausschließlich `Headless=true`, auf 1280×720: Playwright öffnete den Dialog per `Enter`, prüfte sichtbaren Fokus, `Tab` und `Shift+Tab` in der Fokusfalle, schloss per `Escape` und verifizierte die Fokusrückgabe. Der 1024×720-Smoke sowie `Emulation.setPageScaleFactor(2)` bestätigten ohne horizontalen Dokumentüberlauf die 200-%-Zoom-Anforderung. Semantik-Smoke prüfte `header`/benannte `nav`, `main`, Label, Tabellenkopf und Dialog. Kein sichtbarer Browser und keine Browsermatrix wurden gestartet. |
    | Temporäre Browserautomatisierung | Der erste selbstgeschriebene CDP-Harness erhielt bei `Input.dispatchKeyEvent` keinen Blazor-Click. Die Ursache der nachfolgenden 500er war der falsche Content Root (Release-DLL mit Quellordner statt Publish-Ordner; gehashte Static-Web-Assets nicht auflösbar). Der finale Nachweis verwendete deshalb ausschließlich im ignorierten Fixture das am Prüftag aktuelle stabile `Microsoft.Playwright`-Paket (NuGet.org), Upstream `https://github.com/microsoft/playwright-dotnet`, MIT. Seine Transitiven sind ebenfalls MIT. Der Treiber ist weder Produkt- noch Testprojektabhängigkeit und legt M0.3-T4 nicht vorweg; `Channel = "chrome"` und `Headless = true` waren explizit gesetzt. |
    | Paket-, Asset- und Publishmessung | Das native App-Projekt löste außer dem automatischen `Microsoft.AspNetCore.App.Internal.Assets` keine direkte oder transitive NuGet-Abhängigkeit auf. Damit entsteht keine neue Lizenz-/NOTICE-Pflicht und `THIRD-PARTY-NOTICES.md` bleibt unverändert. Services: Razor Components plus Interactive Server; Assets: lokale CSS-Tokens, 879-B-Dialogwrapper und Blazor-Frameworkassets, ohne Fremd-JS/CSS. Release-Publish des nativen Fixtures: `wwwroot` 562.439 B, gesamt 1.210.101 B. Eine Bibliotheksdifferenz existiert regelgemäß nicht. |
    | Verworfene Alternativen | `Microsoft.FluentUI.AspNetCore.Components` und `MudBlazor` wurden nicht installiert oder geprüft: Die native Basis erfüllt bereits alle Musskriterien; gemäß verbindlicher Kandidatenreihenfolge endet der Paketvergleich hier. |

    Der Spike einschließlich bUnit-/Playwright-Hilfsprojekten und
    Publishmessungen bleibt auf ausdrücklichen Benutzerwunsch vollständig und
    uncommittet unter `temp/webfrontend-spikes/M0.3-T1` beziehungsweise dessen
    benachbarten Temp-Ergebnisordnern erhalten; dies überschreibt für diesen
    Nachweis die Cleanupregel. **O-001 ist mit dieser Auswahl in der Roadmap
    geschlossen.** Die dafür vorgesehene Konzeptdatei bleibt auf ausdrückliche
    Benutzeranweisung unverändert readonly; auch `THIRD-PARTY-NOTICES.md`
    erhält keine Änderung, da keine Fixture-Abhängigkeit in das Repository
    aufgenommen wurde.

  - [x] **M0.3-T2 – Knowledge-Tree-Komponente auswählen**
    - Kandidaten: (1) eine schmale native Blazor-/HTML-Lösung, (2) genau die Tree-Komponente der in M0.3-T1 gewählten allgemeinen UI-Bibliothek, falls dort eine enthalten ist, und (3) die aktuelle stabile `Radzen.Blazor`-Tree-Komponente. Kandidat 2 entfällt bei nativer UI-Basis oder fehlender Tree-Komponente; weitere Tree-Pakete werden nicht gesucht.
    - Gemeinsamer Datenadapter: opake Cursor, Root- und Children-Seiten zu je 100 Einträgen, stabile `NodeId`, `HasChildren`, kontrollierter Expand-/Selection-State und Instrumentierung jedes Datenabrufs. Die synthetische Quelle bildet 100.000 Nodes, mindestens zehn Ebenen und einen Parent mit mehr als 1.000 direkten Children ab, hält aber nie den Gesamtbaum im UI-Speicher.
    - Musskriterien: echtes Load-on-expand; serverseitiges Root-/Children-Paging ohne interne Web-API; begrenzte DOM- und Speichernutzung durch Paging plus Virtualisierung oder gleichwertig begrenztes Rendering; stabiler Zustand nach Seitenwechsel und Re-render; zugängliche Tree-Semantik, Pfeil-/Home-/End-/Enter-/Leertastenbedienung und sichtbarer Fokus; Drag-and-drop-Zielvorschau für `Parent`, `Before` und `After`; dieselben drei Moves vollständig ohne Drag-and-drop per Tastatur/Aktionsmenü; Interactive Server und Headless-Chrome-Testbarkeit.
    - Knock-outs: Vorabladen des Gesamtbaums; nur clientseitiges Paging; Verlust von Auswahl/Expand-State beim Nachladen; keine eindeutige Move-Zielposition; notwendiger Fork, DOM-Patch oder Zugriff auf nichtöffentliche Komponenteninternas. Ein kleiner öffentlicher Adapter und eigene Darstellung der Tastaturalternative sind zulässig und werden im Aufwand ausgewiesen.
    - Auswahlregel: Nach Musskriterien gewinnt der Kandidat mit dem kleinsten produktiven Paket-/JS-/CSS-Footprint und der geringsten komponentenspezifischen Adapterfläche. Eine allgemeine Suite wird nicht allein für den Tree gewählt, wenn die native Lösung gleichwertig ist.
    - Ergebnisort: Entscheidung, Datenadaptervertrag, Zustandsbesitz, bekannte Grenzen und spätere Integrationsschritte in `konzept/02-bedienkonzept-und-ui.md`; Lizenzbefund in `THIRD-PARTY-NOTICES.md`; O-003 entfernen.
    - Abnahme: Instrumentierte Tests belegen, dass Expand und Paging nur die angeforderte Seite laden; alle Musskriterien und die Grenzen der gewählten Lösung sind dokumentiert.

    **Nachweis und Entscheidung (2026-09-18):** Gewählt ist die
    **schmale native Blazor-/HTML-/CSS-Tree-Implementierung**. Sie ist die
    einzige zulässige Lösung, die den vollständigen Vertrag ohne zusätzliche
    Produktionsabhängigkeit erfüllt. Kandidat 2 entfällt, weil M0.3-T1 die
    native UI-Basis gewählt hat. `Radzen.Blazor` wurde als am
    Prüftag aktuelle stabile Version aus NuGet.org anhand des ausgelieferten
    Pakets und seiner öffentlichen XML-API geprüft, aber nicht eingebunden:
    `RadzenTree` bietet `Expand` und `Reload`, jedoch weder eine öffentliche
    Tree-Virtualisierungs- noch eine serverseitige Cursor-Paging-API. Seine
    Datenbindung erwartet `ChildrenProperty`; damit würden beim Parent mit
    2.000 Children alle Kindobjekte und DOM-Elemente bereitgestellt. Das ist
    ein Knock-out für serverseitiges Paging und begrenztes Rendering. Ein
    eigener Adapter, DOM-Patch, Fork oder Zugriff auf nichtöffentliche
    Internas wurde nicht verwendet. Das NuGet-Paket ist 9.660.960 B groß,
    verlangt für `net10.0` `Microsoft.AspNetCore.Components` und
    `.Web`, liefert JavaScript/CSS-Assets und trägt MIT
    (Upstream `https://github.com/radzenhq/radzen-blazor`, Paketcommit
    `8d114b7016a97cabb836dae33a08ab52d93a58dc`). MIT wäre mit M0.1-T2
    vereinbar, ist wegen des Knock-outs aber keine Produktabhängigkeit und
    erzeugt keinen Eintrag in `THIRD-PARTY-NOTICES.md`.

    | Aspekt | Nachweis |
    |---|---|
    | Gemeinsamer öffentlicher Datenadapter | `SyntheticTreeSource` modelliert 100.000 Knoten rein synthetisch, mindestens zehn Ebenen (die Kette über die Root-Knoten 1 bis 10) und 2.000 direkte Children von Node 1. `GetRoots(cursor)` und `GetChildren(parentId, cursor)` geben ausschließlich `TreeResultPage(Items, NextCursor, TotalCount)` mit opakem Base64-Cursor, stabiler `Guid`-`NodeId`, `HasChildren` und exakt höchstens 100 Einträgen zurück. Jede tatsächliche Quellabfrage schreibt `scope`, `parentId`, Cursor und Anzahl in die sichtbare Instrumentierung; ein Ein-Seiten-Cache verhindert nur den zweiten identischen Prerender-Read und hält nie einen Gesamtbaum vor. |
    | On-demand, Paging und Speichergrenze | Die native Komponente lädt zunächst nur 100 Roots. Erst `ArrowRight` oder der öffentliche Expand-Button lädt die erste Children-Seite; die nächste Seite fordert mit dem vom Adapter erhaltenen Cursor genau weitere 100 Children an. Root- und Child-Paging sind reine Interactive-Server-Aufrufe, keine interne Web-API. Der DOM enthält 100 Nodes ohne Expand und 200 Nodes mit genau einer offenen 100er-Childseite – gleichwertig begrenzt durch serverseitiges Paging statt Vollbaum- oder Client-Paging. |
    | Zustand und Semantik | Expand- und Auswahlzustand besitzen die Featurekomponente als `HashSet<NodeId>` beziehungsweise `selectedId`; sie bleiben bei Seitenwechsel und Re-render erhalten und sind keine fachliche Wahrheit. Der Tree hat `role=tree`, Knoten haben `treeitem`, Ebene, Auswahl- und Expand-Attribute; roving `tabindex`, sichtbarer 3-px-Fokusring und `ArrowUp/Down/Left/Right`, `Home`, `End`, `Enter` und Leertaste sind implementiert. |
    | Verschieben ohne Ausschluss | Native HTML-Drag-Ereignisse (`dragstart`, `dragover`, `drop`) bieten die klar beschriftete Zielvorschau `Before`, `Parent`, `After`. Dieselben drei eindeutigen Zielpositionen sind als fokussierbare Aktionsmenü-Buttons verfügbar und lösen denselben öffentlichen `Move(source, target, position)`-Vertrag aus; der Spike nutzt keinen privaten Komponentenmechanismus. Die endgültliche persistente Move-Mutation bleibt absichtlich M4 vorbehalten. |
    | Automatisierter Nachweis | `dotnet build -c Release` und `dotnet publish -c Release -o publish`: jeweils 0 Warnungen/0 Fehler. Der ausschließlich headless gestartete Chrome-Lauf (am Prüftag aktuelles stabiles `Microsoft.Playwright`, `Channel=chrome`) startet den veröffentlichten Interactive-Server-Host auf dynamischem Loopback-Port und endet ohne verbliebenen Prozess. Er prüft Tree-Semantik, 100/200 DOM-Grenze, die Fetchfolge `roots:null:100`, `children:node1:null:100`, `children:node1:cursor(100):100`, `roots:cursor(100):100`, Auswahl/Expand nach Re-render, alle geforderten Tasten sowie `Before`, `Parent`, `After` über das Aktionsmenü. |
    | Messung | Release-Publish: `wwwroot` 553.904 B, gesamt 802.737 B. Die gewählte Tree-Fixture hat keine direkte oder transitive Produkt-NuGet-Abhängigkeit, keine Fremd-JS-/CSS-Assets, keinen CDN-, Cloud- oder Telemetriezwang und keine JavaScript-Buildtoolchain. |

    **Integrationsvertrag und Grenzen:** In M2/M3 wird der Fixture-Adapter durch
    einen transportneutralen Navigation-Read-Port ersetzt, der genau die
    genannten Cursor-Seiten liefert; `NodeId`, `HasChildren`, Titel und
    Read-Kontext bleiben explizit. Der UI-Circuit besitzt nur Auswahl,
    sichtbare Seiten und Expand-Cache; Route und Query bleiben die
    rekonstruierbare Quelle für Node, Rolle und Read-Kontext. M4 verbindet
    die drei `Move`-Positionen mit der fachlichen Mutation einschließlich
    TransactionId, Validierung, Berechtigungsprüfung und Konfliktanzeige.
    Mehrfach gleichzeitig offene Parents sowie echte Viewport-Virtualisierung
    sind bewusst keine Behauptung des Spikes: Die produktive Komponente muss
    weiterhin eine begrenzte Zahl offener Seiten halten; andernfalls ist vor
    Erweiterung der gewählte Paging-Renderer zu ergänzen. **O-003 ist mit
    dieser Auswahl geschlossen.**

    Die vollständige nichtproduktive Fixture bleibt auf ausdrücklichen
    Benutzerwunsch uncommittet unter `temp/webfrontend-spikes/M0.3-T2`
    erhalten. Sie teilt weder Projektdateien noch Code mit der Solution;
    besonders der temporäre Playwright-Treiber ist keine Vorentscheidung für
    M0.3-T4.

  - [x] **M0.3-T3 – Markdown-fähigen Rich-Text-Editor auswählen**
    - Kandidaten: aktuelle stabile Versionen von Milkdown und Tiptap mit dessen offizieller Markdown-Erweiterung; beide werden mit demselben Fixture geprüft, Milkdown zuerst. Die weiterhin als Beta dokumentierte Tiptap-Markdown-Erweiterung ist als Produktrisiko auszuweisen. TOAST UI Editor ist ausgeschlossen, weil das Upstream-Repository seit 2026-09-02 archiviert und schreibgeschützt ist; weitere Editoren werden nicht gesucht.
    - Golden-Master-Fixture: Absätze; fett/kursiv/durchgestrichen; Links; geordnete, ungeordnete und verschachtelte Listen; Tabellen; Inline-Code; fenced Code mit Sprachkennung; Blockquotes; Unicode; absichtlich enthaltenes Raw HTML; erlaubte und verbotene Linkziele; externe Bildsyntax; Text-, Browser-HTML- und Office-HTML-Paste; Inhalt nahe der fachlichen 4-KiB-Warngrenze.
    - Roundtrip: jeden zulässigen Golden Master fünfmal `Markdown -> Editor -> Markdown` durchlaufen lassen und anschließend mit Markdig als semantische Struktur vergleichen. Unterschiedliche, aber semantisch gleichwertige Markdownschreibweisen sind zulässig; verlorene oder neu erzeugte Struktur ist ein Knock-out. Das serverseitig verbotene Raw HTML und Headings müssen als klarer Validierungsfehler erhalten beziehungsweise abgelehnt werden und dürfen nicht stillschweigend verschwinden.
    - Sicherheits- und Integrationsprüfung: Heading-Befehle sind nicht verfügbar; Links folgen exakt O-020; externe Bilder/Assets lösen im Browser keinen Request aus; Paste-Reduktionen erzeugen einen sichtbaren Hinweis; der ursprüngliche Editorinhalt bleibt bei Serverablehnung erhalten; Image-Upload ist deaktiviert, besitzt aber einen später anschließbaren internen Hook; Dirty-State, Fokus, Dispose, Reconnect und Nodewechsel sind kontrollierbar.
    - Musskriterien: Markdown bleibt einziges kanonisches Speicherformat; kein HTML-/Editor-JSON als verstecktes Zweitformat; self-hosted Assets; schmale JS-Isolation für Blazor Interactive Server; automatisierbar in Headless Chrome; keine Cloudfunktion und kein Pro-/Bezahlmodul für Mussfunktionen.
    - Auswahlregel: Ein stabiler, vollständig bestehender Markdownpfad gewinnt vor einer Beta-API. Danach gelten geringere semantische Anpassung, kleinere transitive JavaScript-Lieferkette und weniger eigener zustandsbehafteter JS-Code. Besteht kein Kandidat den Roundtrip, wird nichts ausgewählt.
    - Ergebnisort: Entscheidung, erlaubte Editorfunktionen, Interop-/Lifecycle-Vertrag, Golden Master und bekannte Grenzen in `konzept/03-content-und-assets.md`; Lizenzbefund in `THIRD-PARTY-NOTICES.md`; O-002 entfernen.
    - Abnahme: alle erlaubten Strukturen bestehen den fünffachen semantischen Roundtrip und sämtliche Sicherheits-/Lifecycle-Kriterien sind automatisiert belegt.

    **Nachweis und Entscheidung (2026-09-18):** Gewählt ist
    **Milkdown `@milkdown/crepe`**. Die am Prüftag aktuelle
    stabile npm-Version wurde aus `https://registry.npmjs.org/@milkdown/crepe`
    (Dist-Tag `latest`) und dem ausgelieferten Paket geprüft; Upstream ist
    `https://github.com/Milkdown/milkdown`, Lizenz MIT. Die Referenzintegration
    nutzt den vorhandenen Markdown-Import und `getMarkdown()` ohne eigene
    Markdown-Umwandlung. Damit bleibt Markdown der einzige Wert an der
    Blazor-/Servergrenze und das einzige persistierbare Format; die interne,
    flüchtige ProseMirror-Struktur wird weder gespeichert noch als zweites
    Transportformat geführt. Es gibt keinen CDN-, Cloud-, Telemetrie-, Pro- oder
    Bezahlzwang.

    Beide strikt vorgegebenen Kandidaten liefen mit demselben lokalen,
    nichtproduktiven Fixture unter `temp/webfrontend-spikes/M0.3-T3`:
    `dotnet build EditorSpike.csproj --no-restore` war mit 0 Warnungen/0 Fehlern
    erfolgreich; der lokale Markdig-Endpunkt verwendete die
    Advanced-Extensions-Pipeline für die semantische AST-Strukturprüfung. Der
    Browsernachweis lief ausschließlich mit der am Prüftag installierten aktuellen Google-Chrome-Stable-Version und dem aktuellen stabilen
    `Microsoft.Playwright`-Paket, `Channel = "chrome"`, `Headless = true`.
    Er wartet auf die beobachtbare Fixture-API und Browserzustände, nicht auf
    feste Sleeps. Playwright, Vite und der Minihost sind reine, uncommittete
    Spikewerkzeuge und keine Vorentscheidung für M0.3-T4.

    | Gemeinsamer Golden Master und Ergebnis | Milkdown | Tiptap + `@tiptap/markdown` |
    |---|---|---|
    | Absätze, fett/kursiv/durchgestrichen, erlaubte `https`-/`mailto`-/Fragment-/root-relative-/relative Links, geordnete/ungeordnete/verschachtelte Listen, Tabellen, Inline- und `csharp`-Fenced-Code, Zitat, Unicode | 5/5 Markdown → Editor → Markdown, jeweils Markdig-äquivalente Struktur | 5/5, jeweils Markdig-äquivalente Struktur |
    | Größennahe Variante | final 4.012 UTF-8-Bytes | final 4.014 UTF-8-Bytes |
    | Separate Listen-/Tabellen- sowie Link-Master | jeweils 5/5 Markdig-äquivalent | jeweils 5/5 Markdig-äquivalent |
    | Raw HTML und Markdown-/HTML-Headings | unverändert an den Server übergeben und klar abgelehnt: `RawHtmlNotAllowed`, `HeadingNotAllowed`, `RawHtmlNotAllowed`; kein stilles Entfernen | gleiches Ergebnis |
    | O-020-Links und externe Bildsyntax | `http`, `javascript`, `data`, `file`, Netzwerkpfad `//` und externe Bilder vor dem Editorzugriff abgelehnt (`LinkTargetNotAllowed` bzw. `ExternalImageNotAllowed`); ein Request-Observer sah 0 Requests auf die externe Bild-URL | gleiches Ergebnis |
    | Text-, Browser-HTML- und Office-HTML-Paste | Text blieb Text; unsichere Links, Bilder, Styles und Headingsemantik wurden reduziert; der sichtbare `role=status`-Hinweis erschien | gleiches Ergebnis |
    | Serverablehnung und Lifecycle | Ausgangsinhalt blieb nach jeder Ablehnung erhalten; Dirty-State, Fokus, Reconnect mit Inhaltserhalt, Dispose und Nodewechsel mit Verwerfungsentscheidung automatisiert positiv | gleiches Ergebnis |

    Die sichtbare Milkdown-Crepe-Toolbar enthielt im Fixture ausschließlich
    Bold, Italic, Strikethrough, Inline-Code und Link; die öffentlichen
    Featureflags deaktivierten Latex und ImageBlock, und es gab keinen Heading-
    oder Bild-Upload-Befehl. In der späteren Blazor-Komponente
    wird diese reduzierte Commandmenge über genau ein featurelokales,
    dynamisch importiertes `ContentEditor.razor.js` gebunden: `mount` erhält
    Markdown und Change-/Focus-Callbacks, `readMarkdown`, `focus` und `dispose`
    sind die einzigen weiteren Aufrufe. `dispose` wird bei Reconnect und
    Nodewechsel vor erneutem `mount` ausgeführt; Dirty-Content verbleibt im
    Circuit und erfordert explizit `Bleiben` oder `Ungespeicherte Eingabe
    verwerfen`. Ein späterer M8-Hook darf ausschließlich eine bereits
    serverseitig erzeugte interne Assetreferenz in den Editor einsetzen; er
    aktiviert weder Browserupload noch externe Bild-URLs. Der Server validiert
    weiterhin vor jeder Mutation, zeigt den konkreten Befund und überschreibt
    bei Fehler niemals den unpersistierten Editorwert.

    **Verworfener Kandidat:** Tiptap mit der offiziellen
    Markdown-Erweiterung bestand technisch dieselben Nachweise und
    benötigt keine kostenpflichtige Funktion. Die offizielle Tiptap-Dokumentation
    bezeichnet die Erweiterung jedoch weiterhin ausdrücklich als **Beta/early
    release** und weist auf mögliche nicht unterstützte Randfälle hin; sie
    übersetzt Markdown zudem über Marked in internes Tiptap-JSON und zurück.
    Das ist kein persistiertes Zweitformat im Fixture, aber eine Beta-API an der
    kanonischen Speichergrenze. Nach der Auswahlregel verliert sie deshalb gegen
    Milkdowns stabilen, vollständigen Markdownpfad.

    | Lizenz- und Lieferkettenmessung aus den tatsächlich installierten Paketen | Milkdown | Tiptap |
    |---|---:|---:|
    | Laufzeitabhängigkeiten im vollständigen Closure | 209 | 48 |
    | Lizenzbefund | 205 MIT, 1 BSD-2-Clause, 1 BSD-3-Clause, 1 ISC, `dompurify` `MPL-2.0 OR Apache-2.0` (für die Distribution ist die permissive Apache-2.0-Alternative zu erfüllen) | 48 MIT |
    | Closure-Dateigröße | 55.591.064 B | 9.971.519 B |
    | Eigener zustandsbehafteter JS-Code für den Markdownpfad | keiner | keiner |

    Alle vorgenannten aufgelösten Paketstände wurden gegen die Paketmetadaten und ihre
    vollständigen direkten/transitiven Closure-Manifeste geprüft. Copyright-,
    Lizenz- und gegebenenfalls NOTICE-Texte werden bei der späteren tatsächlichen
    Produktaufnahme in `THIRD-PARTY-NOTICES.md` vollständig inventarisiert;
    dieser uncommittete Spike fügt keine Produktabhängigkeit hinzu und ändert
    das Inventar deshalb nicht. Die größere Milkdown-Lieferkette ist nachrangig,
    weil der stabile existierende Markdownpfad vor der Tiptap-Beta gewinnt.

    Die Fixture wird auf ausdrücklichen Benutzerwunsch erhalten, nicht gelöscht
    und nicht committed. Sie ist ausschließlich unter
    `temp/webfrontend-spikes/M0.3-T3` abgelegt, wird als nichtproduktive
    Wiederverwendung für nachfolgende Spikes behalten und teilt keine Solution-,
    zentrale Paket-, Produktions- oder Testprojektdatei. **O-002 ist mit dieser
    Auswahl in der Roadmap geschlossen.** M0.3 und M0 bleiben bis M0.3-T4 offen.

  - [x] **M0.3-T4 – Web-Komponenten- und Browser-Testwerkzeuge auswählen**
    - Feste Werkzeuge: aktuelle stabile bUnit-Version mit xUnit v3 für Razor-Komponenten und aktuelle stabile Microsoft-Playwright-.NET-Version mit dem installierten Google-Chrome-Stable-Kanal für Browserabläufe. Es werden keine alternativen Testframeworks verglichen.
    - Fixture bUnit: eine zustandsbehaftete Razor-Komponente rendert, DI beziehen, Parameter ändern, Event auslösen, Fehlerzustand prüfen und dünnes JS-Interop mocken. Der Test läuft mit `dotnet test` unter `net10.0` und xUnit v3.
    - Fixture Playwright: einen realen temporären ASP.NET-Core-Host auf dynamischem Loopback-Port starten, Interactive-Server-Verbindung abwarten, über Rollen/Labels/Test-IDs bedienen, Web-first Assertions und einen stabil maskierten Screenshot ausführen sowie sauber beenden. Start ausschließlich mit `Channel = "chrome"` und `Headless = true`; kein Chromium-Fallback, keine Browsermatrix und kein sichtbarer Browserstart.
    - Chrome-Voraussetzung: Version protokollieren und einen nichtinteraktiven Installations-/Prüfbefehl für lokale und CI-Ausführung dokumentieren. Fehlt Chrome, wird nicht still auf einen anderen Browser gewechselt. Die Browserinstallation ist keine Anwendungslaufzeitabhängigkeit.
    - Vitest-Entscheidung: nur ergänzen, wenn die in M0.3-T2/T3 gewählte Integration eigenen zustandsbehafteten JavaScript-/TypeScript-Code mit Verzweigungen, Transformationen oder Retry-/Lifecyclelogik erfordert. Bei ausschließlich dünnen Interop-Aufrufen wird ausdrücklich „nicht erforderlich“ dokumentiert und keine Node-Testtoolchain angelegt. Falls erforderlich, muss ein Fixture die reine JS-Logik ohne Browser testen und der feste CI-Befehl dokumentiert werden.
    - Prüfen: Interaktion, JS-Interop, Screenshots, Parallel-/Wiederholungslauf, keine festen Wartezeiten, lokale/CI-Befehle, Lizenzgraph und Wartungsstatus. Browser-E2E bleibt auf Integrationsrisiken begrenzt; fachliche Varianten gehören in Core- und Komponententests.
    - Nicht enthalten: Testprojekte oder produktive Testfälle; diese folgen in M2.
    - Ergebnisort: Projektzuordnung, feste Befehle, Locator-/Screenshot-Regeln und Vitest-Entscheidung in `konzept/08-projektstruktur-und-codekonventionen.md`; die aufgelösten Versionen stehen bei Produktaufnahme in Paketverwaltung, Lockfile und Lizenzinventar; O-015 entfernen.
    - Abnahme: beide Pflichtfixtures laufen zweimal hintereinander grün und hinterlassen keinen Hostprozess; O-015 ist geschlossen.

    **Nachweis und Entscheidung (2026-09-18):** Das isolierte, uncommittete Fixture
    unter `temp/webfrontend-spikes/M0.3-T4` prüfte die am Prüftag stabilen
    `bunit` (MIT, NuGet.org, `https://github.com/bUnit-dev/bUnit`) und
    `xunit.v3` (Apache-2.0, NuGet.org, `https://github.com/xunit/xunit`)
    mit dem zugehörigen Runner. Der feste Befehl
    `dotnet test ComponentTests/M03T4.ComponentTests.csproj` lief zweimal
    hintereinander grün (je ein Test): Rendering, Scoped-DI, Parameterwechsel,
    Event, Fehlerzustand und das gemockte dünne JS-Interop sind belegt.

    Die Browserpflichtfixture verwendet das am Prüftag aktuelle stabile `Microsoft.Playwright`-Paket
    (MIT, NuGet.org, `https://github.com/microsoft/playwright-dotnet`) und
    ausschließlich die installierte aktuelle Google-Chrome-Stable-Version
    mit `Channel = "chrome"` und `Headless = true`. Der reale Host startete
    auf dynamischem Loopback und die Shell war erreichbar. Der entscheidende
    Startweg ist der frisch mit `dotnet publish -c Release -o Publish`
    erzeugte Temp-Publish-Ordner als Content Root der Release-DLL; damit
    lösen Static-Web-Assets korrekt auf und der ausschließlich nach realem
    `OnAfterRender` gesetzte Circuitmarker erscheint. In zwei unmittelbaren
    Wiederholungsläufen bediente Playwright den Button per Rolle, das Feld per
    Label und das Ergebnis per Test-ID, wartete Web-first auf
    `DI verfügbar: Browserwert` und erzeugte einen stabil maskierten
    Screenshot. Es gab keinen Chromium-Fallback, keinen sichtbaren Browser
    und keine feste Wartezeit: Host und Circuit werden ausschließlich über
    beobachtbare Zustände abgewartet. Der Host wird im `finally` beendet; die
    konkrete Prozessprüfung ergab keinen verbliebenen `M0.3-T4`-Hostprozess.

    Lokale und CI-Prüfung vor Browser-E2E: `Get-ItemProperty
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Google
    Chrome' | Select-Object -Expand DisplayVersion`; nichtinteraktive
    Windows-Installation: `winget install --id Google.Chrome --exact
    --silent --accept-package-agreements --accept-source-agreements`. Fehlt
    Chrome danach, schlägt der Lauf fehl statt auf einen anderen Browser zu
    wechseln. Chrome ist keine Anwendungslaufzeitabhängigkeit.

    **Vitest-Entscheidung:** nicht erforderlich. Die gewählte Native-Tree- und
    Milkdown-Integration besitzen nach M0.3-T2/T3 keinen eigenen
    zustandsbehafteten JS-/TS-Pfad mit Verzweigungen, Transformationen oder
    Retry-/Lifecyclelogik; vorgesehen sind ausschließlich schmale
    featurelokale Interop-Aufrufe. Es wird keine Node-Testtoolchain angelegt.
    `THIRD-PARTY-NOTICES.md` bleibt unverändert, weil keines dieser Pakete
    produktiv oder dauerhaft aufgenommen wurde. Auf ausdrücklichen
    Benutzerwunsch bleibt das Fixture als ignorierte, nichtproduktive Referenz
    erhalten und wird nicht gelöscht oder committed. **O-015 ist durch diese
    Werkzeugentscheidung in der Roadmap geschlossen; M0.3 und M0 sind
    abgeschlossen.**

## Milestone-Abnahme

- O-001 bis O-003 und O-015 sind geschlossen; die niedrig priorisierte Assetentscheidung O-004 bleibt bis M8 offen.
- Jede gewählte direkte und transitive Abhängigkeit ist kostenlos nutzbar, mit der MIT-Distribution vereinbar und besitzt eine dokumentierte Lizenz-, Pflicht- und Wartungsbewertung.
- Kein Kernmilestone M1 bis M6 hängt von einer unbekannten Kernkomponente oder unbestätigten Hostannahme ab.
