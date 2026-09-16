using Dapper;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using KnowHowToAI.Storage.SqlServer.Repositories;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;

internal sealed class SqlHierarchyRepository : SqlRepository, IHierarchyRepository
{
    internal const string ListSql = """
        SELECT SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted
        FROM dbo.KnowHowToAI_Node
        WHERE SnapshotId = @snapshotId
        ORDER BY SortOrder, NodeId;
        """;

    public SqlHierarchyRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
        : base(connectionFactory, storagePolicy)
    {
    }

    public async Task<IReadOnlyList<Node>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<NodeRow>(
            CreateCommand(ListSql, new { snapshotId = snapshotId.Value }, cancellationToken)).ConfigureAwait(false);
        return rows.Select(SqlRowMapper.ToNode).ToArray();
    }
}
