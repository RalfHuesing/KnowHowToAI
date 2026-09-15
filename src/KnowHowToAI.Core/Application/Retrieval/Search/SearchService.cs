using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Retrieval.Search;

/// <summary>
/// Transportneutraler Search-Use-Case (search): Textsuche über Titel, Description und Content.
/// V1 bietet keine semantische oder linguistisch vollständige Suche (ADR-V1-006).
/// Die eigentliche SQL-Suche wird in M5.3 implementiert.
/// </summary>
public sealed class SearchService
{
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IRetrievalRepository _retrievalRepository;
    private readonly RetrievalPolicy _retrievalPolicy;

    public SearchService(
        ISnapshotRepository snapshotRepository,
        ITransactionRepository transactionRepository,
        IRetrievalRepository retrievalRepository,
        RetrievalPolicy retrievalPolicy)
    {
        _snapshotRepository = snapshotRepository ?? throw new ArgumentNullException(nameof(snapshotRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _retrievalRepository = retrievalRepository ?? throw new ArgumentNullException(nameof(retrievalRepository));
        _retrievalPolicy = retrievalPolicy ?? throw new ArgumentNullException(nameof(retrievalPolicy));
    }

    /// <summary>
    /// Sucht nach Nodes, die <paramref name="query"/> im Titel, in der Description
    /// oder in aktivem auflösbarem Content enthalten.
    /// </summary>
    public Task<Result<SearchResultPage>> SearchAsync(
        SearchQuery query,
        ReadContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        // Implementierung folgt in M5.3
        throw new NotImplementedException("SearchAsync wird in M5.3 implementiert.");
    }
}
