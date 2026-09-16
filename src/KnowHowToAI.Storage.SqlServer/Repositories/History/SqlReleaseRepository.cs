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

    private const string LockReleaseGuardSql = """
        SELECT snapshotRow.State AS SnapshotState,
               existingRelease.ReleaseId AS ExistingReleaseId
        FROM dbo.KnowHowToAI_Snapshot AS snapshotRow WITH (UPDLOCK, HOLDLOCK)
        LEFT JOIN dbo.KnowHowToAI_Release AS existingRelease WITH (UPDLOCK, HOLDLOCK)
            ON existingRelease.Name = @name
        WHERE snapshotRow.SnapshotId = @snapshotId;
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

    /// <summary>
    /// Registriert den Release-Verweis in einem kurzen SQL-Vorgang: Existenz und Zustand
    /// <c>Committed</c> des referenzierten Snapshots werden unter Zeilensperre zusammen
    /// mit dem Insert geprüft, damit kein Release auf einen zwischen Service-Vorprüfung
    /// und Registrierung verworfenen Snapshot entstehen kann.
    /// </summary>
    public async Task<Result<Release>> CreateAsync(
        CreateReleaseRecord request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var databaseTransaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            var guard = await ReadCommittedSnapshotGuardAsync(connection, databaseTransaction, request, cancellationToken)
                .ConfigureAwait(false);
            if (guard.Error is not null)
                return await RollbackAsync(databaseTransaction, Result<Release>.Failure(guard.Error)).ConfigureAwait(false);

            var row = await connection.QuerySingleAsync<ReleaseRow>(
                CreateCommand(InsertSql, new
                {
                    snapshotId = request.SnapshotId.Value,
                    name = request.Name,
                    description = request.Description,
                    releasedAtUtc = request.CreatedAtUtc.UtcDateTime
                }, cancellationToken, databaseTransaction)).ConfigureAwait(false);
            await databaseTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Result<Release>.Success(SqlRowMapper.ToRelease(row));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return await RollbackAsync(
                databaseTransaction,
                Result<Release>.Failure(CreateNameConflictError(request))).ConfigureAwait(false);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            return await RollbackAsync(
                databaseTransaction,
                Result<Release>.Failure(CreateSnapshotNotFoundError(request))).ConfigureAwait(false);
        }
        catch
        {
            await databaseTransaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
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

    private async Task<ReleaseGuardResult> ReadCommittedSnapshotGuardAsync(
        SqlConnection connection,
        SqlTransaction databaseTransaction,
        CreateReleaseRecord request,
        CancellationToken cancellationToken)
    {
        var guard = await connection.QuerySingleOrDefaultAsync<ReleaseGuardRow>(
            CreateCommand(
                LockReleaseGuardSql,
                new { snapshotId = request.SnapshotId.Value, name = request.Name },
                cancellationToken,
                databaseTransaction)).ConfigureAwait(false);

        if (guard is null)
            return new ReleaseGuardResult(CreateSnapshotNotFoundError(request));
        if (!string.Equals(guard.SnapshotState, SqlPersistedValues.SnapshotCommitted, StringComparison.Ordinal))
            return new ReleaseGuardResult(CreateSnapshotNotCommittedError(request));
        if (guard.ExistingReleaseId is not null)
            return new ReleaseGuardResult(CreateNameConflictError(request));
        return new ReleaseGuardResult(null);
    }

    private static async Task<T> RollbackAsync<T>(SqlTransaction databaseTransaction, T result)
    {
        await databaseTransaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        return result;
    }

    private static DomainError CreateSnapshotNotFoundError(CreateReleaseRecord request) =>
        new(
            ReleaseErrorCodes.SnapshotNotFound,
            "Der referenzierte Snapshot existiert nicht.",
            new Dictionary<string, string> { [ReleaseErrorCodes.SnapshotIdDetail] = request.SnapshotId.ToString() });

    private static DomainError CreateSnapshotNotCommittedError(CreateReleaseRecord request) =>
        new(
            ReleaseErrorCodes.SnapshotNotCommitted,
            "Releases können nur auf committed Snapshots angelegt werden.",
            new Dictionary<string, string> { [ReleaseErrorCodes.SnapshotIdDetail] = request.SnapshotId.ToString() });

    private static DomainError CreateNameConflictError(CreateReleaseRecord request) =>
        new(
            ReleaseErrorCodes.ReleaseNameConflict,
            $"Ein Release mit dem Namen '{request.Name}' existiert bereits.",
            new Dictionary<string, string> { [ReleaseErrorCodes.ReleaseNameDetail] = request.Name });

    private sealed record ReleaseGuardResult(DomainError? Error);

    private sealed class ReleaseGuardRow
    {
        public string SnapshotState { get; init; } = string.Empty;

        public long? ExistingReleaseId { get; init; }
    }
}
