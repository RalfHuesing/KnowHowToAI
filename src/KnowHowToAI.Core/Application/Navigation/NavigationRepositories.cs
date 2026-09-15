using KnowHowToAI.Core.Application.Abstractions.Persistence;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Faesst alle Read-Ports zusammen, die der NavigationService benoetigt.
/// Reduziert die Konstruktor-Parameteranzahl gemaess MaxConstructorDependencies-Regel.
/// </summary>
public sealed record NavigationRepositories(
    ISnapshotRepository Snapshots,
    ITransactionRepository Transactions,
    IHierarchyRepository Hierarchy,
    IRoleRepository Roles,
    IContentRepository Contents,
    IDependencyRepository Dependencies);
