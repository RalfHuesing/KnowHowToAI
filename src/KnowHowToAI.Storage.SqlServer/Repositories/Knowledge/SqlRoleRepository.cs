using Dapper;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using KnowHowToAI.Storage.SqlServer.Repositories;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;

internal sealed class SqlRoleRepository : SqlRepository, IAudienceRepository
{
    internal const string ListRolesSql = """
        SELECT SnapshotId, RoleId, Name, Description, IsDeleted
        FROM dbo.KnowHowToAI_Role
        WHERE SnapshotId = @snapshotId
        ORDER BY RoleId;
        """;

    internal const string ListResolutionsSql = """
        SELECT SnapshotId, RequestedRoleId, CandidateRoleId, Priority
        FROM dbo.KnowHowToAI_RoleResolution
        WHERE SnapshotId = @snapshotId
        ORDER BY RequestedRoleId, Priority;
        """;

    public SqlRoleRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
        : base(connectionFactory, storagePolicy)
    {
    }

    public async Task<IReadOnlyList<Audience>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<RoleRow>(
            CreateCommand(ListRolesSql, new { snapshotId = snapshotId.Value }, cancellationToken)).ConfigureAwait(false);
        return rows.Select(SqlRowMapper.ToRole).ToArray();
    }

    public async Task<IReadOnlyList<AudienceResolution>> ListResolutionsBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<RoleResolutionRow>(
            CreateCommand(ListResolutionsSql, new { snapshotId = snapshotId.Value }, cancellationToken)).ConfigureAwait(false);
        return rows.Select(SqlRowMapper.ToRoleResolution).ToArray();
    }
}
