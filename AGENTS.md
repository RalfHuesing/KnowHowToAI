# KnowHowToAI

Hierarchische, versionierte und rollenabhängige Wissensbasis für Agenten und Menschen (.NET / C#, MS SQL Server, MCP Streamable HTTP).

## Grundprinzip: Greenfield (Hard Cut)

- Vollständiger Neustart bei Stand 0. Keine Legacy-Migrationspfade, keine Abwärtskompatibilität, kein Rückgriff auf alte Branches.

## Verbindliche Dokumentation & Regeln

- **Ist-Dokumentation (fachlich & technisch)**: [docs/README.md](docs/README.md) — Einstieg, Verbindlichkeit und Lese-Matrix je Aufgabe
- **Architektur-, Coding-, MCP- & Git-Regeln**: [.agents/rules/Richtlinien.mdc](.agents/rules/Richtlinien.mdc)
- **Build-Ausführung**: [scripts/build.ps1](scripts/build.ps1) — killt blockierende Prozesse, baut die Solution, Output statisch in `temp/build.log` (bei Exitcode != 0 lesen). Details: [.agents/rules/BuildWorkflow.mdc](.agents/rules/BuildWorkflow.mdc)
- **Teststrategie & Testebenen**: [.agents/rules/TestRichtlinien.mdc](.agents/rules/TestRichtlinien.mdc)
- **C#-Codeanalyse & Linter-Workflow**: [.agents/rules/AiNetLinter-McpWorkflow.mdc](.agents/rules/AiNetLinter-McpWorkflow.mdc)
- **Doku-Pflege**: [.agents/rules/DokuRichtlinien.mdc](.agents/rules/DokuRichtlinien.mdc)
