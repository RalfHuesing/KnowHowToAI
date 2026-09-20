using Dapper;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using KnowHowToAI.Storage.SqlServer.Repositories;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;

internal sealed class SqlContentRepository : SqlRepository, IContentRepository
{
    internal const string ListSql = """
        SELECT SnapshotId, NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd, IsDeleted
        FROM dbo.KnowHowToAI_NodeContent
        WHERE SnapshotId = @snapshotId
        ORDER BY NodeId, AudienceId;
        """;

    public SqlContentRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
        : base(connectionFactory, storagePolicy)
    {
    }

    public async Task<IReadOnlyList<NodeContent>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<NodeContentRow>(
            CreateCommand(ListSql, new { snapshotId = snapshotId.Value }, cancellationToken)).ConfigureAwait(false);
        return rows.Select(SqlRowMapper.ToNodeContent).ToArray();
    }
}
