using Dapper;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using KnowHowToAI.Storage.SqlServer.Repositories;

namespace KnowHowToAI.Storage.SqlServer.Repositories.History;

internal sealed class SqlReleaseRepository : SqlRepository, IReleaseRepository
{
    private const string FindSql = """
        SELECT ReleaseId, SnapshotId, Name, Description, ReleasedAtUtc
        FROM dbo.KnowHowToAI_Release
        WHERE ReleaseId = @releaseId;
        """;

    public SqlReleaseRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
        : base(connectionFactory, storagePolicy)
    {
    }

    public async Task<Release?> FindAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<ReleaseRow>(
            CreateCommand(FindSql, new { releaseId = releaseId.Value }, cancellationToken)).ConfigureAwait(false);
        return row is null ? null : SqlRowMapper.ToRelease(row);
    }
}
