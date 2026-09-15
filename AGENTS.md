# KnowHowTo AI

Hierarchische, versionierte und rollenabhängige Wissensbasis für Agenten und Menschen (.NET / C#, MS SQL Server, MCP STDIO).

## Grundprinzip: Greenfield (Hard Cut)

- Vollständiger Neustart bei Stand 0. Keine Legacy-Migrationspfade, keine Abwärtskompatibilität, kein Rückgriff auf alte Branches.
- Verbindliche fachliche und architektonische Quelle sind ausschließlich der [Konzeptindex](docs/Konzept.md) und die dort verlinkten Konzeptmodule.

## Verbindliche Dokumentation & Regeln

- **Fachkonzept, Architektur & Invarianten**: [docs/Konzept.md](docs/Konzept.md) einschließlich der dort vorgeschriebenen Pflichtmodule
- **Implementierungs-Roadmap & Status**: [docs/Roadmap.md](docs/Roadmap.md)
- **Architektur-, Coding-, MCP- & Git-Regeln**: [.agents/rules/Richtlinien.mdc](.agents/rules/Richtlinien.mdc)
- **Teststrategie & Testebenen**: [.agents/rules/TestRichtlinien.mdc](.agents/rules/TestRichtlinien.mdc)
- **C#-Codeanalyse & Linter-Workflow**: [.agents/rules/AiNetLinter-McpWorkflow.mdc](.agents/rules/AiNetLinter-McpWorkflow.mdc)
