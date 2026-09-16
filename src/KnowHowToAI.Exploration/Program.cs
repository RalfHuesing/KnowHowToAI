using KnowHowToAI.Exploration.Areas;
using KnowHowToAI.Exploration.Hosting;

namespace KnowHowToAI.Exploration;

/// <summary>
/// Explorations-Harness für KnowHowTo AI: ruft die originalen MCP-Tool-Handler
/// in-process auf (gleicher DI-Host wie der Server, ohne stdio-Transport), damit
/// ein Agent agentische Aufgaben end-to-end durchspielen, Befunde sammeln und
/// den produktiven Code direkt fixen kann.
/// </summary>
internal static class Program
{
    private static readonly IReadOnlyList<IExplorationArea> AreaCatalog =
    [
        new NavigationReadArea(),
        new SelfDocumentationArea()
    ];

    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        try
        {
            return args.Length == 0
                ? PrintUsage()
                : args[0].ToLowerInvariant() switch
                {
                    "help" or "--help" or "-h" => PrintUsage(),
                    "areas" => PrintAreas(),
                    "tools" => PrintTools(),
                    "run" => await RunSelectedAsync(args).ConfigureAwait(false),
                    "run-all" => await RunAllAsync().ConfigureAwait(false),
                    _ => UnknownCommand(args[0])
                };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Unerwarteter Fehler: {exception}");
            return 2;
        }
    }

    private static int PrintUsage()
    {
        Console.WriteLine(
            """
            KnowHowToAI.Exploration – In-Process-Harness für die MCP-Tools (ohne stdio-Transport)

            Befehle:
              areas                           Verfügbare Bereiche mit Szenarien auflisten
              tools                           Vom Server-Projekt entdeckte MCP-Tool-Namen
              run <areaId> [--scenario <id>]  Einen Bereich (oder ein einzelnes Szenario) ausführen
              run-all                         Alle Bereiche ausführen
            """);
        return 0;
    }

    private static int PrintAreas()
    {
        using var host = ExplorationHostFactory.CreateHost();
        var context = new ExplorationContext(host.Services, new ExplorationJournal("catalog"));
        foreach (var area in AreaCatalog)
        {
            Console.WriteLine($"{area.Id} – {area.Description}");
            foreach (var scenario in area.CreateScenarios(context))
                Console.WriteLine($"    {scenario.Id}: {scenario.Description}");
        }

        return 0;
    }

    private static int PrintTools()
    {
        foreach (var name in ExplorationHostFactory.DiscoverToolNames())
            Console.WriteLine(name);
        return 0;
    }

    private static async Task<int> RunSelectedAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Verwendung: run <areaId> [--scenario <id>]");
            return 2;
        }

        var area = AreaCatalog.FirstOrDefault(candidate =>
            candidate.Id.Equals(args[1], StringComparison.OrdinalIgnoreCase));
        if (area is null)
        {
            Console.Error.WriteLine($"Unbekannter Bereich '{args[1]}'. Verfügbare: {string.Join(", ", AreaCatalog.Select(a => a.Id))}");
            return 2;
        }

        string? scenarioId = null;
        for (var index = 2; index < args.Length; index++)
        {
            if (args[index] is "--scenario" or "-s")
            {
                if (index + 1 >= args.Length)
                {
                    Console.Error.WriteLine("Nach --scenario fehlt die Szenario-Id.");
                    return 2;
                }

                scenarioId = args[index + 1];
                index++;
            }
            else
            {
                Console.Error.WriteLine($"Unbekanntes Argument '{args[index]}'.");
                return 2;
            }
        }

        return await RunAreaAsync(area, scenarioId).ConfigureAwait(false);
    }

    private static async Task<int> RunAllAsync()
    {
        var exitCode = 0;
        foreach (var area in AreaCatalog)
            exitCode = Math.Max(exitCode, await RunAreaAsync(area, scenarioId: null).ConfigureAwait(false));

        return exitCode;
    }

    private static async Task<int> RunAreaAsync(IExplorationArea area, string? scenarioId)
    {
        using var host = ExplorationHostFactory.CreateHost();
        Console.WriteLine("Host wird gestartet (Schema-Migrationen laufen wie beim Server-Start) ...");
        await host.StartAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            var journal = new ExplorationJournal(area.Id);
            var context = new ExplorationContext(host.Services, journal);
            IEnumerable<ExplorationScenario> scenarios = area.CreateScenarios(context);
            if (scenarioId is not null)
            {
                scenarios = scenarios
                    .Where(scenario => scenario.Id.Equals(scenarioId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (!scenarios.Any())
                {
                    Console.Error.WriteLine($"Unbekanntes Szenario '{scenarioId}' in Bereich '{area.Id}'.");
                    return 2;
                }
            }

            foreach (var scenario in scenarios)
            {
                context.CurrentScenario = scenario.Id;
                Console.WriteLine();
                Console.WriteLine($"=== Szenario {area.Id}/{scenario.Id}: {scenario.Description} ===");
                try
                {
                    await scenario.Run(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    journal.Bug(scenario.Id, $"Szenario abgebrochen: {exception}");
                    Console.WriteLine($"!! Szenario fehlgeschlagen: {exception.Message}");
                }
            }

            journal.PrintSummary();
            return journal.HasBugs ? 1 : 0;
        }
        finally
        {
            await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unbekannter Befehl '{command}'. Nutztes siehe 'help'.");
        return 2;
    }
}
