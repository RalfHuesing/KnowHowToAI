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
- Kontrollierte HTTP-Endpunkte werden erst in dem Milestone ergänzt, der den jeweiligen Browser-Upload oder -Download tatsächlich benötigt.
- Keine allgemeine REST-/JSON-API und kein OpenAPI-Vertrag für n8n im ersten Schritt.

Der M1-Webhost-Umbau verändert den vorhandenen `DatabaseConnection`-Vertrag nicht. SQL-Benutzername und Passwort dürfen für den aktuellen Entwicklungs- und ersten Betriebsstand weiterhin in der einzigen versionierten `appsettings.json` stehen. M1 führt weder Secret Provider noch Windows-Servicekonten, gMSA oder eine zweite Konfigurationsquelle ein. Die konkrete produktive Prozess-/SQL-Identität ist eine Betriebsentscheidung und wird erst im manuellen M6.0-Gate bei bekannter Deploymentumgebung bewertet.

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
- M0 bis M2 belegen den direkten gemeinsamen Kestrel-Origin. Eine Reverse-Proxy-Produktwahl, TLS-Terminierung und betriebliche Proxy-Timeouts werden nicht vorgezogen, sondern im manuellen M6.0-Gate mit der realen Deploymentumgebung entschieden.
- Request-Limits, Timeouts und Response Compression werden erst mit einem konkreten Endpunkt- oder Betriebsbedarf ergänzt. Streaming und Cancellation des MCP-Endpunkts werden bereits in M0/M1 gegen Kestrel verifiziert.

Eine spätere REST-API kann im selben Prozess und Port unter `/api/v1` ergänzt werden. Dafür ist weder eine zweite EXE noch ein zweiter Port erforderlich.

## MCP-Transport

Streamable HTTP ersetzt STDIO vollständig. Das transportneutrale Domain-/Application-Design bleibt beim Transportwechsel erhalten.

Ziel für den zentralen Betrieb:

- Offizielles `ModelContextProtocol.AspNetCore`-Paket.
- Streamable HTTP auf `/mcp`.
- Stateless Transport, weil `TransactionId`, `SnapshotId`, Cursor und Rolle fachlichen Zustand explizit adressieren.
- Kein Transport-Sessionzustand als fachliche Quelle.
- Bestehende Tool-Namen und Verträge werden beim Transportwechsel fachlich beibehalten.
- Im PoC existiert kein verbindlicher externer MCP-Zielclient. Die Transportabnahme verwendet den offiziellen SDK-Client automatisiert gegen den real gestarteten HTTP-Host; Tool Discovery, repräsentative Reads/Writes, strukturierte Fehler, Streaming und der vollständige Transaction-Workflow bilden das Gate.
- Hermes Agent darf bei lokaler Verfügbarkeit zusätzlich für einen manuellen Eval-Smoke verwendet werden. Dieser Smoke ist optional, wird nicht durch den Implementierungsagenten installiert und blockiert weder M1 noch den Hard Cut.
- Nach erfolgreicher automatisierter HTTP-Vertragsabnahme wird STDIO per Hard Cut vollständig entfernt.
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

Die verbindliche Zuordnung aller Projekte, Ordner, Namespaces, Featurebereiche, Klassentypen, Routen und Testprojekte steht ausschließlich in [Projektstruktur und Codekonventionen](08-projektstruktur-und-codekonventionen.md).

Architekturgrenzen:

- `KnowHowToAI.Server` bleibt eine Deployment-Einheit und enthält Composition Root, MCP-, Blazor-, PDF- und technisch notwendige HTTP-Adapter.
- Domain und transportneutrale Application-Use-Cases verbleiben in `KnowHowToAI.Core`.
- SQL-Persistenz verbleibt in `KnowHowToAI.Storage.SqlServer`.
- Ein allgemeiner Ordner oder Namespace `Api` entsteht erst mit einer tatsächlich beschlossenen externen Integrations-API.
- Bestehende Schreibweise `Mcp` bleibt; keine parallele `MCP`-Struktur.
