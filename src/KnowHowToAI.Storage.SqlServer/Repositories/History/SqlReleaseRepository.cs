using Dapper;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.Storage.SqlServer.Repositories.History;

internal sealed class SqlReleaseRepository : SqlRepository, IReleaseRepository, IReleaseMutationRepository
{
    private const string FindSql = """
        SELECT ReleaseId, SnapshotId, Name, Description, ReleasedAtUtc
        FROM dbo.KnowHowToAI_Release
        WHERE ReleaseId = @releaseId;
        """;

    private const string InsertSql = """
        INSERT INTO dbo.KnowHowToAI_Release (SnapshotId, Name, Description, ReleasedAtUtc)
        OUTPUT INSERTED.ReleaseId, INSERTED.SnapshotId, INSERTED.Name, INSERTED.Description, INSERTED.ReleasedAtUtc
        VALUES (@snapshotId, @name, @description, @releasedAtUtc);
        """;

    private const string ListSql = """
        SELECT TOP (@limit) ReleaseId, SnapshotId, Name, Description, ReleasedAtUtc
        FROM dbo.KnowHowToAI_Release
        WHERE (@afterReleaseId IS NULL OR ReleaseId > @afterReleaseId)
        ORDER BY ReleaseId ASC;
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

    public async Task<Result<Release>> CreateAsync(
        CreateReleaseRecord request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var row = await connection.QuerySingleAsync<ReleaseRow>(
                CreateCommand(InsertSql, new
                {
                    snapshotId = request.SnapshotId.Value,
                    name = request.Name,
                    description = request.Description,
                    releasedAtUtc = request.CreatedAtUtc.UtcDateTime
                }, cancellationToken)).ConfigureAwait(false);

            return Result<Release>.Success(SqlRowMapper.ToRelease(row));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return Result<Release>.Failure(new DomainError(
                ReleaseErrorCodes.ReleaseNameConflict,
                $"Ein Release mit dem Namen '{request.Name}' existiert bereits.",
                new Dictionary<string, string> { [ReleaseErrorCodes.ReleaseNameDetail] = request.Name }));
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            return Result<Release>.Failure(new DomainError(
                ReleaseErrorCodes.SnapshotNotFound,
                "Der referenzierte Snapshot existiert nicht.",
                new Dictionary<string, string> { [ReleaseErrorCodes.SnapshotIdDetail] = request.SnapshotId.ToString() }));
        }
    }

    public async Task<IReadOnlyList<Release>> ListAsync(
        int limit,
        long? afterReleaseId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<ReleaseRow>(
            CreateCommand(ListSql, new { limit, afterReleaseId }, cancellationToken)).ConfigureAwait(false);

        return rows.Select(SqlRowMapper.ToRelease).ToArray();
    }
}
