# Architektur, API und MCP

## Zielbild des ersten Schritts

```text
Browser
  → Blazor Web UI (/)
      → Application Services
          → Domain
              → Repository
                  → SQL Server

MCP-Clients
  → MCP Streamable HTTP (/mcp)
      → dieselben Application Services
```

Verbindlicher initialer Umfang:

- Blazor-Weboberfläche für Menschen.
- MCP über Streamable HTTP für externe Agenten.
- Kontrollierte HTTP-Endpunkte, die das Webfrontend technisch benötigt, beispielsweise Assets, Uploads oder Downloads.
- Keine allgemeine REST-/JSON-API und kein OpenAPI-Vertrag für n8n im ersten Schritt.

## Blazor-interne Aufrufe

Blazor Interactive Server läuft serverseitig. Komponenten und UI-nahe Services rufen Application Services deshalb direkt per Dependency Injection auf.

- Kein HTTP-Loopback vom Server zur eigenen REST-API.
- Keine REST-Endpunkte nur deshalb, weil eine UI-Funktion existiert.
- Kein direkter SQL-Zugriff aus Komponenten oder UI-Services.
- UI-Modelle bleiben von Domain-Typen getrennt und werden an der Web-Grenze gemappt.
- Fachliche Validierung bleibt serverseitig in Domain und Application; UI-Prüfungen verbessern nur die Bedienung.
- Paging, opake Cursors und `ChangeVersion` gelten bis in die UI.
- Browserseitig notwendige Dateiübertragungen dürfen schmale, zweckgebundene HTTP-Endpunkte verwenden. Sie bilden keine allgemeine Integrations-API.

Diese Struktur spart im ersten Schritt eine zusätzliche Netzwerkschicht, ohne spätere externe APIs zu erschweren.

## Spätere REST-/n8n-Option

n8n ist eine mögliche spätere Integration, keine aktuelle Mindestanforderung.

```text
n8n / sonstige Systeme
  → optionale REST API (/api/v1, OpenAPI)
      → dieselben Application Services
```

Vorbereitung ohne Vorratsimplementierung:

- Use Cases liegen in Application Services und enthalten keine Blazor-, MCP- oder HTTP-Abhängigkeiten.
- Eingaben, Ergebnisse und Fehler der Application-Schicht sind transportneutral.
- Web und MCP enthalten nur Mapping, Transportlogik und Darstellung.
- Fachliche Regeln werden weder in Blazor-Komponenten noch in MCP-Tools dupliziert.
- Explizite Arbeitskontexte wie `TransactionId`, `SnapshotId`, Rolle und Cursor bleiben Teil der Use-Case-Aufrufe.
- Eine spätere API erhält eigene versionierte Contracts und Mapper; Domain-Typen werden nicht direkt serialisiert.
- `/api` bleibt als Routingkonvention für diesen späteren Adapter reserviert.

Erst ein konkreter Automationsfall entscheidet, welche REST-Endpunkte benötigt werden. Dann werden Minimal APIs und OpenAPI gezielt ergänzt. Es wird weder ein leerer API-Layer noch eine vorsorgliche Spiegelung aller UI- oder MCP-Funktionen gebaut.

## Ein Prozess und ein Port

Empfehlung: eine EXE, ein Kestrel-Host, ein konfigurierter HTTP(S)-Port.

```text
https://knowhowtoai.intern/
https://knowhowtoai.intern/mcp
https://knowhowtoai.intern/assets/...
```

ASP.NET Core unterscheidet diese Oberflächen per Endpoint Routing; mehrere Ports sind nicht erforderlich. Kestrel kann bei begründetem Bedarf mehrere Endpoints oder Ports bedienen, das ist hier aber kein Ziel.

Leitplanken gegen reale Konflikte:

- MCP immer explizit auf `/mcp` mappen; nicht auf die Root-Route.
- Blazor-Komponenten und Fallbacks dürfen `/mcp`, `/assets` und reserviertes `/api` nicht verschlucken.
- Statische Assets und Upload-/Download-Routen eindeutig trennen.
- Reverse Proxy muss Blazor-SignalR/WebSockets und lang laufende MCP-HTTP-Antworten unterstützen.
- Request-Limits, Timeouts, Response Compression und Streaming pro Endpunktgruppe bewusst konfigurieren.

Eine spätere REST-API kann im selben Prozess und Port unter `/api/v1` ergänzt werden. Dafür ist weder eine zweite EXE noch ein zweiter Port erforderlich.

## MCP-Transport

Streamable HTTP ersetzt STDIO vollständig. Das transportneutrale Domain-/Application-Design bleibt beim Transportwechsel erhalten.

Ziel für den zentralen Betrieb:

- Offizielles `ModelContextProtocol.AspNetCore`-Paket.
- Streamable HTTP auf `/mcp`.
- Stateless Transport, weil `TransactionId`, `SnapshotId`, Cursor und Rolle fachlichen Zustand explizit adressieren.
- Kein Transport-Sessionzustand als fachliche Quelle.
- Bestehende Tool-Namen und Verträge werden beim Transportwechsel fachlich beibehalten.
- Nach erfolgreicher HTTP-Client-Abnahme wird STDIO per Hard Cut vollständig entfernt.
- Kein STDIO-Runner, Startmodus, Konfigurationsschlüssel, Paket, Deploymentpfad oder transportgebundener Test bleibt bestehen.
- Kein Doppelbetrieb, Kompatibilitätsmodus oder STDIO-Fallback; Streamable HTTP ist anschließend der einzige unterstützte MCP-Transport.
- Eine vorübergehende Koexistenz ist ausschließlich innerhalb der noch nicht abgeschlossenen Umstellung zulässig und wird nicht als Produktstand freigegeben.

## DI- und Zustandsgrenzen

Eine gemeinsame EXE ist unkritisch, wenn Lebensdauern korrekt behandelt werden:

- Stateless MCP und zweckgebundene HTTP-Endpunkte besitzen einen Request-Scope.
- Blazor Interactive Server besitzt einen Circuit-Scope, der länger als ein HTTP-Request lebt.
- Application Services und Repositories speichern deshalb keinen ausgewählten Node, keine Rolle und keine aktive Transaction als impliziten Mutable State.
- Arbeitskontext wird als `TransactionId`, `SnapshotId`, `RoleId` und `ChangeVersion` explizit übergeben.
- SQL-Verbindungen bleiben kurzlebig und operationsbezogen.
- Blazor-Circuit-State enthält nur flüchtigen Darstellungszustand; nach Reconnect ist fachlicher Zustand rekonstruierbar.

## Projekt- und Namespace-Struktur

```text
src/KnowHowToAI.Server/
├─ Program.cs
├─ appsettings.json
├─ Configuration/
├─ Hosting/
├─ Mcp/
│  ├─ Contracts/
│  ├─ Mapping/
│  └─ Tools/
├─ Web/
│  ├─ Components/Layout/
│  ├─ Components/Pages/
│  ├─ Components/Shared/
│  ├─ Endpoints/
│  ├─ Features/<Feature>/
│  └─ State/
└─ wwwroot/
```

- Namespaces: zunächst `KnowHowToAI.Server.Mcp` und `.Web`.
- Bestehende Schreibweise `Mcp` bleibt; keine parallele `MCP`-Struktur.
- `Web/Endpoints` enthält ausschließlich technisch notwendige Browser-Endpunkte wie Uploads oder Downloads.
- Ein Namespace und Ordner `KnowHowToAI.Server.Api` wird erst mit einer echten externen REST-API angelegt.
- `KnowHowToAI.Server` wird ASP.NET-Core-Webhost und Composition Root.
- Projekt-SDK wird `Microsoft.NET.Sdk.Web`.
- Eine `appsettings.json` enthält die gemeinsame Basiskonfiguration. Umgebungsoverrides sind Betriebsmechanik, keine zweite Fachkonfiguration.
- Features werden innerhalb `Mcp` und `Web` weiter unterteilt; keine God Components oder Sammelordner.
- Domain, Application und Storage verbleiben in den vorhandenen Projekten.
- Ein separates Clientprojekt wird erst bei bewusster Wahl von Interactive WebAssembly erforderlich.
