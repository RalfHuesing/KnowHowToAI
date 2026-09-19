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
    private const string LatestReleaseSql = """
        SELECT TOP (1) ReleaseId, SnapshotId, Name, Description, ReleasedAtUtc
        FROM dbo.KnowHowToAI_Release
        ORDER BY ReleasedAtUtc DESC, ReleaseId DESC;
        """;

    private const string ListOpenTransactionsSql = """
        SELECT TransactionId, BaseSnapshotId, WorkingSnapshotId, State, ChangeVersion,
               CreatedAtUtc, CommittedAtUtc, Purpose, Actor, Client, CommitMessage
        FROM dbo.KnowHowToAI_Transaction
        WHERE State = 'Open'
        ORDER BY CreatedAtUtc DESC;
        """;

    public SqlDashboardRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
        : base(connectionFactory, storagePolicy)
    {
    }

    public async Task<Release?> GetLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<ReleaseRow>(
            CreateCommand(LatestReleaseSql, null, cancellationToken)).ConfigureAwait(false);
        return row is null ? null : SqlRowMapper.ToRelease(row);
    }

    public async Task<IReadOnlyList<KnowledgeTransaction>> ListOpenTransactionsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<TransactionRow>(
            CreateCommand(ListOpenTransactionsSql, null, cancellationToken)).ConfigureAwait(false);
        return rows.Select(SqlRowMapper.ToTransaction).ToArray();
    }
}
