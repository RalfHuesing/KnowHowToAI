# Architektur, API und MCP

## Zielbild

```text
Browser
  → Blazor Web UI (/)
      → Application Services
          → Domain
              → Repository
                  → SQL Server

n8n / sonstige Systeme
  → REST API (/api/v1, OpenAPI)
      → dieselben Application Services

MCP-Clients
  → MCP Streamable HTTP (/mcp)
      → dieselben Application Services
```

- Kein direkter SQL-Zugriff aus UI, REST oder MCP.
- Blazor Interactive Server verwendet Application Services direkt; kein interner HTTP-Loopback zur eigenen REST-API.
- REST und MCP sind eigenständige Adapter für externe Clients.
- UI-, REST- und MCP-Modelle bleiben von Domain-Typen getrennt.
- Fachliche Validierung ist serverseitig; clientseitige Prüfungen verbessern nur die Bedienung.
- Paging, opake Cursors und `ChangeVersion` gelten bis in die UI.

## Ein Prozess und ein Port

Empfehlung: eine EXE, ein Kestrel-Host, ein konfigurierter HTTP(S)-Port.

```text
https://knowhowtoai.intern/
https://knowhowtoai.intern/api/v1/...
https://knowhowtoai.intern/openapi/v1.json
https://knowhowtoai.intern/mcp
https://knowhowtoai.intern/assets/...
```

ASP.NET Core unterscheidet diese Oberflächen per Endpoint Routing; mehrere Ports sind nicht erforderlich. Kestrel kann bei begründetem Bedarf mehrere Endpoints/Ports bedienen, das ist hier aber kein Ziel.

Leitplanken gegen reale Konflikte:

- MCP immer explizit auf `/mcp` mappen; nicht auf die Root-Route.
- REST ausschließlich unter `/api/v1` gruppieren.
- Blazor-Komponenten und Fallbacks dürfen `/api`, `/mcp`, `/assets` und `/openapi` nicht verschlucken.
- Statische Assets und Upload-/Download-Routen eindeutig trennen.
- Reverse Proxy muss Blazor-SignalR/WebSockets und lang laufende MCP-HTTP-Antworten unterstützen.
- Request-Limits, Timeouts, Response Compression und Streaming pro Endpunktgruppe bewusst konfigurieren.

## HTTP-Endpunkte und n8n

Blazor erzeugt keine fachliche API automatisch. Blazor, Minimal APIs, OpenAPI und MCP verwenden aber dasselbe ASP.NET-Core-Endpoint-Routing.

```text
/                  Blazor Web App
/api/v1/...        REST/JSON für n8n und andere Integrationen
/openapi/v1.json   generierter OpenAPI-Vertrag
/mcp               MCP Streamable HTTP
/assets/...        kontrollierte Asset-Auslieferung
```

- REST wird als versionierte Minimal API nach Features organisiert.
- OpenAPI wird aus expliziten REST-Endpunkten generiert.
- n8n verwendet REST per HTTP Request; MCP ersetzt keine stabile Integrations-API.
- Vom Server ausgelöste n8n-Workflows können später konfigurierbare Webhooks verwenden.
- HTTP-Handler mappen und delegieren nur an Application Services.
- Schreibendpunkte erwarten explizite `TransactionId`; HTTP-Requests werden nicht zu lang laufenden SQL-Transactions.

## MCP-Transport

STDIO war für V1 fachlich korrekt: Der Client startet den Server lokal als Child Process. Das transportneutrale Domain-/Application-Design bleibt gültig.

Ziel für den zentralen Betrieb:

- Offizielles `ModelContextProtocol.AspNetCore`-Paket.
- Streamable HTTP auf `/mcp`.
- Stateless Transport, weil `TransactionId`, `SnapshotId`, Cursor und Rolle fachlichen Zustand explizit adressieren.
- Kein Transport-Sessionzustand als fachliche Quelle.
- Bestehende Tool-Namen und Verträge werden beim Transportwechsel fachlich beibehalten.
- STDIO wird nach Client-Abnahme in einem eigenen Hard-Cut-Schnitt entfernt; kein dauerhafter Doppelbetrieb.

## DI- und Zustandsgrenzen

Eine gemeinsame EXE ist unkritisch, wenn Lebensdauern korrekt behandelt werden:

- REST und stateless MCP besitzen einen Request-Scope.
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
├─ Api/
│  ├─ Contracts/
│  ├─ Mapping/
│  └─ Endpoints/<Feature>/
├─ Web/
│  ├─ Components/Layout/
│  ├─ Components/Pages/
│  ├─ Components/Shared/
│  ├─ Features/<Feature>/
│  └─ State/
└─ wwwroot/
```

- Namespaces: `KnowHowToAI.Server.Mcp`, `.Api`, `.Web`.
- Bestehende Schreibweise `Mcp` bleibt; keine parallele `MCP`-Struktur.
- `KnowHowToAI.Server` wird ASP.NET-Core-Webhost und Composition Root.
- Projekt-SDK wird `Microsoft.NET.Sdk.Web`.
- Eine `appsettings.json` enthält die gemeinsame Basiskonfiguration. Umgebungsoverrides sind Betriebsmechanik, keine zweite Fachkonfiguration.
- Features werden innerhalb `Api` und `Web` weiter unterteilt; keine God Components oder Sammelordner.
- Domain, Application und Storage verbleiben in den vorhandenen Projekten.
- Ein separates Clientprojekt wird erst bei bewusster Wahl von Interactive WebAssembly erforderlich.
