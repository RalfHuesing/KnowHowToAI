using KnowHowToAI.Core.Application.Abstractions.Persistence;

namespace KnowHowToAI.Core.Application.Retrieval.Search;

/// <summary>
/// Bündelt Repositories für den transportneutralen Search-Use-Case gemäß Constructor-Dependency-Regeln.
/// </summary>
public sealed record SearchRepositories(
    ISnapshotRepository Snapshots,
    ITransactionRepository Transactions,
    IRetrievalRepository Retrieval);
