using Dapper;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using KnowHowToAI.Storage.SqlServer.Repositories;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Snapshots;

internal sealed class SqlSnapshotRepository : SqlRepository, ISnapshotRepository
{
    private const string SelectColumns = """
        SELECT SnapshotId, BaseSnapshotId, State, CreatedAtUtc, CommittedAtUtc
        FROM dbo.KnowHowToAI_Snapshot

        """;

    private const string FindSql = SelectColumns + "WHERE SnapshotId = @snapshotId;";

    private const string CurrentSql = SelectColumns + """
        WHERE SnapshotId = (
            SELECT CurrentSnapshotId
            FROM dbo.KnowHowToAI_SystemState
            WHERE Id = 1
        );
        """;

    private const string ListCommittedSql = SelectColumns + """
        WHERE State = @committedState
          AND (@beforeSnapshotId IS NULL OR SnapshotId < @beforeSnapshotId)
        ORDER BY SnapshotId DESC
        OFFSET 0 ROWS FETCH NEXT @limit ROWS ONLY;
        """;

    public SqlSnapshotRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
        : base(connectionFactory, storagePolicy)
    {
    }

    public async Task<Snapshot?> FindAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<SnapshotRow>(
            CreateCommand(FindSql, new { snapshotId = snapshotId.Value }, cancellationToken)).ConfigureAwait(false);
        return row is null ? null : SqlRowMapper.ToSnapshot(row);
    }

    public async Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<SnapshotRow>(
            CreateCommand(CurrentSql, parameters: null, cancellationToken)).ConfigureAwait(false);
        return row is null
            ? throw new InvalidOperationException("KnowHowToAI_SystemState enthält keinen aktuellen Snapshot.")
            : SqlRowMapper.ToSnapshot(row);
    }

    public async Task<IReadOnlyList<Snapshot>> ListCommittedAsync(
        int limit,
        SnapshotId? beforeSnapshotId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<SnapshotRow>(CreateCommand(
            ListCommittedSql,
            new
            {
                committedState = SnapshotState.Committed.ToString(),
                beforeSnapshotId = beforeSnapshotId?.Value,
                limit
            },
            cancellationToken)).ConfigureAwait(false);
        return rows.Select(SqlRowMapper.ToSnapshot).ToArray();
    }
}
