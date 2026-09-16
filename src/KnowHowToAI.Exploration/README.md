# KnowHowToAI.Exploration

In-Process-Explorations-Harness für die MCP-Tools des Servers. Zweck: agentische
Aufgaben (z. B. „importiere diese Wikipedia-Seite in die Wissensbasis“) direkt gegen
die **originalen MCP-Handler** durchspielen – ohne stdio-Server, ohne MCP-Client –
und dabei Fehler, Parameterprobleme und Contract-Schwächen finden und im
produktiven Code fixen zu können.

## Prinzip

- **Original-Code testen:** Projekt-Referenzen auf Core, Storage.SqlServer und Server
  (nicht kopiert). Der DI-Host wird identisch zum Server aufgebaut
  (`AddKnowHowToAIOptions` → `AddSqlStorage` → `AddApplicationServices`), nur der
  stdio-Transport entfällt. Tool-Klassen werden per Reflection über
  `[McpServerToolType]` registriert – neue Tools im Server werden automatisch
  gefunden.
- **In-process statt stdio:** Szenarien rufen die Tool-Methoden typisiert auf.
  Der einzige Unterschied zum echten Betrieb ist der Transport; Parameter-Handling,
  Mapping, Envelope-Verträge, Fehlerkatalog und JSON-Serialisierbarkeit sind
  identisch geprüft. Jede Antwort wird zudem als JSON serialisiert, geparst und
  auf ihre kompakte UTF-8-Größe (Token-Kosten-Näherung) vermessen.
- **Befunde statt Crashes:** Szenarien brechen nicht ab, sondern dokumentieren
  Befunde (Info / Improvement / Bug) im Journal. Bugs → Exitcode 1.
- **Keine Lint-Regeln:** Das Projekt ist in `ainetlinter-rules.json` unter
  `FileFilters.ExcludeDirectoryPatterns` ausgeschlossen – Explorationscode ist
  bewusst frei von Produktions-Regeln.

## Verwendung

```bash
dotnet run --project src/KnowHowToAI.Exploration -- help
dotnet run --project src/KnowHowToAI.Exploration -- areas        # Bereiche + Szenarien
dotnet run --project src/KnowHowToAI.Exploration -- tools        # entdeckte MCP-Tool-Namen
dotnet run --project src/KnowHowToAI.Exploration -- run navigation-read
dotnet run --project src/KnowHowToAI.Exploration -- run navigation-read --scenario children-paging
dotnet run --project src/KnowHowToAI.Exploration -- run-all
```

Hinweis: `run` startet den Host inkl. Schema-Migration gegen die DB aus
`appsettings.json` (Kopie der Server-Defaults) – Explorationsdaten sind Wegwerf.
Achtung: Jeder `dotnet test`-Lauf der IntegrationTests leert die DB
(`SqlTestDatabase` verwirft alle `KnowHowToAI_*`-Tabellen) – Harness-Daten
überleben Testläufe nicht.

## Neuen Bereich anlegen

1. Klasse in `Areas/` anlegen, `IExplorationArea` implementieren (Id in kebab-case,
   Szenarien als `ExplorationScenario`-Records mit async-Delegate).
2. Im `AreaCatalog` in `Program.cs` registrieren.

Szenarien nutzen `ExplorationContext`: `Tools<T>()` für typisierte Handler-Zugriffe,
`ReportEnvelope(...)` für JSON-Ausgabe mit Größenreport, `Info/Improvement/Bug` für
Befunde.
