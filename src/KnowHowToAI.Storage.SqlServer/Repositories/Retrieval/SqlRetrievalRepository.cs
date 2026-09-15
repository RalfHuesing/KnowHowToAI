using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Retrieval.Search;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Retrieval;

internal sealed class SqlRetrievalRepository : IRetrievalRepository
{
    /// <inheritdoc />
    /// <remarks>SQL-Implementierung folgt in M5.3.</remarks>
    public Task<IReadOnlyList<SearchHit>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException("SearchAsync wird in M5.3 implementiert.");
}
