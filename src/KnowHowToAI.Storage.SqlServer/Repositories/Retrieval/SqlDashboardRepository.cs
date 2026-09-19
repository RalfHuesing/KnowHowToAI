using Dapper;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Retrieval;

/// <summary>
/// SQL-Server-Implementierung des Dashboard-Read-Ports.
/// </summary>
internal sealed class SqlDashboardRepository : SqlRepository, IDashboardRepository
{
    private readonly ITransactionRepository _transactionRepository;

    private const string LatestReleaseSql = """
        SELECT TOP (1) ReleaseId, SnapshotId, Name, Description, ReleasedAtUtc
        FROM dbo.KnowHowToAI_Release
        ORDER BY ReleasedAtUtc DESC, ReleaseId DESC;
        """;

    public SqlDashboardRepository(
        SqlConnectionFactory connectionFactory,
        SqlStoragePolicy storagePolicy,
        ITransactionRepository transactionRepository)
        : base(connectionFactory, storagePolicy)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
    }

    public async Task<Release?> GetLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<ReleaseRow>(
            CreateCommand(LatestReleaseSql, null, cancellationToken)).ConfigureAwait(false);
        return row is null ? null : SqlRowMapper.ToRelease(row);
    }

    public Task<IReadOnlyList<KnowledgeTransaction>> ListOpenTransactionsAsync(
        int limit,
        CancellationToken cancellationToken = default) =>
        _transactionRepository.ListOpenAsync(limit, cancellationToken);
}
