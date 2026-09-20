using KnowHowToAI.Core.Application.Abstractions.Persistence;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Bündelt alle Read-Ports der Snapshot-Sichten (Navigation, History, Export)
/// für den gemeinsamen Snapshot-Lader. Löst die identischen Records
/// NavigationRepositories und HistoryRepositories ab und reduziert die
/// Konstruktor-Parameteranzahl gemaess MaxConstructorDependencies-Regel.
/// </summary>
public sealed record SnapshotReadRepositories(
    ISnapshotRepository Snapshots,
    ITransactionRepository Transactions,
    IHierarchyRepository Hierarchy,
    IContentRepository Contents,
    IAudienceRepository Audiences,
    IDependencyRepository Dependencies,
    IWorkingSnapshotReadRepository? WorkingSnapshots = null);
