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
- **Planungs- und Ausführungspattern für Roadmaps**: [tasks/README.md](tasks/README.md)

## Modell- und Delegationsstrategie

- Der Hauptchat läuft grundsätzlich mit Sol Max. Der Hauptagent verantwortet Benutzerintention, Planung, Produkt- und Architekturentscheidungen, Taskzuschnitt, Integration und die abschließende Kommunikation.
- Substantielle, klar abgrenzbare Repository-Arbeit wird standardmäßig an Subagenten mit `gpt-5.6-luna` und Reasoning `high` delegiert. Dazu zählen insbesondere umfangreiche Exploration, Audit-Zuarbeit, ausführungsreife Leaf-Tasks, mechanische Änderungen sowie deren risikogerechte Prüfung.
- Nicht delegiert werden reine Gesprächs- und Entscheidungsfragen sowie kleine lokale Aktionen, wenn Agentenstart, Kontextübergabe und erneute Kontrolle voraussichtlich mehr Aufwand verursachen als die direkte Ausführung.
- Standardmäßig arbeitet genau ein schreibender Subagent pro Slice. Parallele Agenten sind auf unabhängige, vorwiegend lesende Analysen oder ausdrücklich disjunkte Schreibbereiche beschränkt; Builds, Tests und Commits im gemeinsamen Worktree laufen seriell.
- Subagenten erhalten kurze, informationsdichte Aufträge mit Ziel, Scope, Nicht-Zielen, maßgeblichen Referenzpfaden, Abnahmekriterien und Prüfauftrag. Bestehende Regeln und Pläne werden verlinkt statt in Prompts kopiert; es wird nur der erforderliche Gesprächskontext weitergegeben.
- Der schreibende Subagent besitzt den vollständigen Slice einschließlich Dokumentationsfolgen, Prüfungen und – wenn vom Benutzer beauftragt – explizitem Staging und atomarem Commit. Der Hauptagent prüft Übergabe, Diff und Status risikobasiert, ohne die vollständige Detailarbeit zu wiederholen.
- Der Hauptagent übernimmt die Ausführung selbst oder eskaliert auf ein stärkeres Modell, wenn echte Produkt-/Architekturentscheidungen, widersprüchliche Regeln, unklarer Scope oder wiederholtes Scheitern von Luna dies erfordern.
- Maximale Token-Effizienz ist verbindlich: keine unnötigen Agenten, keine doppelten Analysen oder Prüfungen, keine ungekürzten Logwiedergaben und keine vorsorgliche Kontextflut. Entscheidungsrelevante Informationen, Nachweise und Sicherheitsgrenzen dürfen dabei nicht verloren gehen.
