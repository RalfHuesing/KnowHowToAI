using KnowHowToAI.Core.Application.Abstractions.Persistence;

namespace KnowHowToAI.Core.Application.History;

/// <summary>
/// Faesst alle Read-Ports zusammen, die HistoryService und MarkdownExportService benoetigen.
/// Reduziert die Konstruktor-Parameteranzahl gemaess MaxConstructorDependencies-Regel.
/// </summary>
public sealed record HistoryRepositories(
    ISnapshotRepository Snapshots,
    ITransactionRepository Transactions,
    IHierarchyRepository Hierarchy,
    IContentRepository Contents,
    IRoleRepository Roles,
    IDependencyRepository Dependencies);
