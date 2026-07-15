# KnowHowToAI

Eine hierarchische Markdown-Wissensdatenbank mit MCP-Zugriff für Claude, Cursor & Co.

Du schreibst Doku als Markdown-Dateien mit YAML-Front-Matter (`title`, `tags`, `synonyms`) in einer Ordnerhierarchie. Ein CLI-Tool validiert die Struktur und synchronisiert sie per Wipe-and-Dump in MS SQL Server. Ein MCP-stdio-Server macht die Bibliothek darüber für LLM-Agenten durchsuchbar — strukturiert (`list_children`), per Stichwortsuche (`search_docs`) und im Detail (`get_doc`) — statt sie als unstrukturierte Textwüste in den Kontext zu laden.

**Konzept & Architektur:** [docs/00-Overview.md](docs/00-Overview.md) — dort auch die Begründung für jede Grundsatzentscheidung.

## Tech-Stack

.NET 10 · MS SQL Server · Dapper · `ModelContextProtocol`-SDK · `System.CommandLine` · Serilog · YamlDotNet · xUnit v3

## Schnellstart

```powershell
dotnet build
dotnet test tests/KnowHowToAI.Core.Tests/KnowHowToAI.Core.Tests.csproj

dotnet run --project src/KnowHowToAI.Cli -- validate
dotnet run --project src/KnowHowToAI.Cli -- import
dotnet run --project src/KnowHowToAI.Cli -- server
```

Konfiguration liegt in [`src/KnowHowToAI.Cli/appsettings.json`](src/KnowHowToAI.Cli/appsettings.json) (Docs-Root-Pfad, SQL-Connection-String, Tabellenname, Details in [docs/03](docs/03-Projektstruktur-und-Konfiguration.md#2-konfiguration-appsettingsjson)). Eine kleine Beispiel-Bibliothek liegt unter [`demo-docs/`](demo-docs/).

### Single-File-Build

```powershell
scripts\publish.ps1
```

Erzeugt eine self-contained `publish\KnowHowToAI.Cli.exe`, die z.B. in der MCP-Server-Konfiguration von Cursor/Claude Desktop referenziert werden kann (Beispiel in [docs/03, Abschnitt 2](docs/03-Projektstruktur-und-Konfiguration.md#mcp-launch-konfiguration-beispiel-für-claude-desktopcursor)).

## Dokumentation

| Dokument | Inhalt |
| --- | --- |
| [docs/00-Overview.md](docs/00-Overview.md) | Elevator Pitch, alle Grundsatzentscheidungen im Überblick |
| [docs/01-Konzept-und-Workflow.md](docs/01-Konzept-und-Workflow.md) | Intention, Akteure, der tägliche Doku-Loop |
| [docs/02-Architektur-und-Techstack.md](docs/02-Architektur-und-Techstack.md) | Tech-Stack, Datenstruktur, MCP-Tools |
| [docs/03-Projektstruktur-und-Konfiguration.md](docs/03-Projektstruktur-und-Konfiguration.md) | Solution-Layout, Konfiguration, CLI, Deployment |
| [docs/04-Datenmodell-Validierung-Edgecases.md](docs/04-Datenmodell-Validierung-Edgecases.md) | SQL-Skripte, Slug-Regeln, Edge Cases |
| [docs/05-Roadmap.md](docs/05-Roadmap.md) | Aktueller Umsetzungsstand, offene Punkte, Backlog |

Agenten-Regeln für die Weiterentwicklung dieses Projekts liegen in [`.agents/rules/`](.agents/rules/).

## Status

Kern-Loop (validate/import/export/server) ist implementiert — aktueller Stand und offene Punkte in [docs/05-Roadmap.md](docs/05-Roadmap.md).

## Lizenz

[MIT](LICENSE)
