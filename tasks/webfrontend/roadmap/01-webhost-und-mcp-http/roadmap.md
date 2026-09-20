# M1 – Gemeinsamer Webhost und MCP HTTP

[Roadmap-Index](../../Roadmap.md)

- [x] **M1 abschließen**

Abhängigkeit: [M0](../00-komponenten-und-architektur/roadmap.md)

Verbindliche M0-Basis: das offizielle Paket `ModelContextProtocol.AspNetCore`, ein direkter Kestrel-Origin, `/mcp` stateless, kein Legacy-SSE, kein zweiter Port und kein Reverse Proxy. Für Tests gelten bUnit mit xUnit v3, Microsoft.Playwright .NET und die installierte aktuelle Google-Chrome-Stable-Version ausschließlich mit `Channel = "chrome"` und `Headless = true`. Die konkrete Abhängigkeitsauswahl folgt der Regel im [Strukturkonzept](../../konzept/08-projektstruktur-und-codekonventionen.md). Die Referenz-Fixtures unter `temp/webfrontend-spikes/` bleiben uncommittet und werden nicht in Produktions- oder Testprojekte übernommen.

Ziel: `KnowHowToAI.Server` stellt Blazor und die bestehenden MCP-Funktionen zentral über HTTP in einer EXE und auf einem Port bereit.

Referenzen: [Zielbild](../../konzept/05-architektur-api-und-mcp.md#zielbild-des-ersten-schritts), [Blazor-interne Aufrufe](../../konzept/05-architektur-api-und-mcp.md#blazor-interne-aufrufe), [MCP-Transport](../../konzept/05-architektur-api-und-mcp.md#mcp-transport), [Projektstruktur](../../konzept/05-architektur-api-und-mcp.md#projekt--und-namespace-struktur)

Verbindliche Zielstruktur: [Projektstruktur und Codekonventionen](../../konzept/08-projektstruktur-und-codekonventionen.md)

## M1.1 – ASP.NET-Core-Host

- [x] **M1.1 abschließen**

  - [x] **M1.1-T1 – [Serverprojekt auf Webhost umstellen](tasks/M1.1-T1.md)**
  - [x] **M1.1-T2 – [Endpunktrouting und Hostkonfiguration absichern](tasks/M1.1-T2.md)**
## M1.2 – Blazor-Grundhost

- [x] **M1.2 abschließen**

  - [x] **M1.2-T1 – [Blazor-Interactive-Server-Shell bereitstellen](tasks/M1.2-T1.md)**
## M1.3 – MCP Streamable HTTP

- [x] **M1.3 abschließen**

  - [x] **M1.3-T1 – [MCP-HTTP-Transport produktiv integrieren](tasks/M1.3-T1.md)**
  - [x] **M1.3-T2 – [MCP-Vertragsregression vollständig abdecken](tasks/M1.3-T2.md)**
## M1.4 – Hard Cut von STDIO

- [x] **M1.4 abschließen**

  - [x] **M1.4-T1 – [STDIO-Transport entfernen und Ist-Dokumentation umstellen](tasks/M1.4-T1.md)**
## Milestone-Abnahme

- Eine EXE bedient auf einem Port Blazor-Shell, notwendige Web-Endpunkte und stateless MCP HTTP.
- Bestehende MCP-Funktionen sind fachlich erhalten und automatisiert über den offiziellen SDK-Client gegen den realen HTTP-Host geprüft.
- Streamable HTTP ist der einzige MCP-Transport; STDIO ist vollständig und ohne Fallback entfernt.
