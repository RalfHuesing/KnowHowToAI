namespace KnowHowToAI.Exploration;

/// <summary>
/// Ein Bereich ("Bereich" im Sinne des Explorations-Harness) bündelt Szenarien zu einem
/// fachlichen Themenfeld. Neue Bereiche implementieren dieses Interface und werden im
/// AreaCatalog des Programms registriert.
/// </summary>
public interface IExplorationArea
{
    /// <summary>Stabiler CLI-Bezeichner (kebab-case), z. B. <c>navigation-read</c>.</summary>
    string Id { get; }

    /// <summary>Kurzbeschreibung, was der Bereich erkundet.</summary>
    string Description { get; }

    /// <summary>
    /// Erzeugt die Szenarien des Bereichs. Wird erst unmittelbar vor der Ausführung
    /// aufgerufen; die Szenario-Delegates greifen erst zur Laufzeit auf Tools zu.
    /// </summary>
    IReadOnlyList<ExplorationScenario> CreateScenarios(ExplorationContext context);
}

/// <summary>
/// Ein einzelner Explorationslauf: ruft Original-MCP-Handler in-process auf und
/// dokumentiert Befunde über den <see cref="ExplorationContext"/>.
/// </summary>
public sealed record ExplorationScenario(string Id, string Description, Func<CancellationToken, Task> Run);
