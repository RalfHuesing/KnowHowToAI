using Dapper;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using KnowHowToAI.Storage.SqlServer.Repositories;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;

internal sealed class SqlAudienceRepository : SqlRepository, IAudienceRepository
{
    internal const string ListAudiencesSql = """
        SELECT SnapshotId, AudienceId, Name, Description, IsDeleted
        FROM dbo.KnowHowToAI_Audience
        WHERE SnapshotId = @snapshotId
        ORDER BY AudienceId;
        """;

    internal const string ListResolutionsSql = """
        SELECT SnapshotId, RequestedAudienceId, CandidateAudienceId, Priority
        FROM dbo.KnowHowToAI_AudienceResolution
        WHERE SnapshotId = @snapshotId
        ORDER BY RequestedAudienceId, Priority;
        """;

    public SqlAudienceRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
        : base(connectionFactory, storagePolicy)
    {
    }

    public async Task<IReadOnlyList<Audience>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<AudienceRow>(
            CreateCommand(ListAudiencesSql, new { snapshotId = snapshotId.Value }, cancellationToken)).ConfigureAwait(false);
        return rows.Select(SqlRowMapper.ToAudience).ToArray();
    }

    public async Task<IReadOnlyList<AudienceResolution>> ListResolutionsBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<AudienceResolutionRow>(
            CreateCommand(ListResolutionsSql, new { snapshotId = snapshotId.Value }, cancellationToken)).ConfigureAwait(false);
        return rows.Select(SqlRowMapper.ToAudienceResolution).ToArray();
    }
}
