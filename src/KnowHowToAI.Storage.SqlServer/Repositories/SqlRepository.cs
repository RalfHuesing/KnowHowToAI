using Dapper;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.Storage.SqlServer.Repositories;

/// <summary>Gemeinsame technische Basis für kurzlebige, parametrisierte SQL-Zugriffe.</summary>
internal abstract class SqlRepository
{
    private const string LockTransactionSql = """
        SELECT transactionRow.WorkingSnapshotId,
               transactionRow.State AS TransactionState,
               snapshotRow.State AS SnapshotState,
               transactionRow.ChangeVersion
        FROM dbo.KnowHowToAI_Transaction AS transactionRow WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.KnowHowToAI_Snapshot AS snapshotRow WITH (UPDLOCK, HOLDLOCK)
            ON snapshotRow.SnapshotId = transactionRow.WorkingSnapshotId
        WHERE transactionRow.TransactionId = @transactionId;
        """;

    private const string IncrementChangeVersionSql = """
        UPDATE dbo.KnowHowToAI_Transaction
        SET ChangeVersion = ChangeVersion + 1
        OUTPUT inserted.ChangeVersion
        WHERE TransactionId = @transactionId
          AND State = 'Open';
        """;

    private readonly SqlConnectionFactory _connectionFactory;
    private readonly SqlStoragePolicy _storagePolicy;

    protected SqlRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
    {
        _connectionFactory = connectionFactory;
        _storagePolicy = storagePolicy;
    }

    protected Task<SqlConnection> OpenAsync(CancellationToken cancellationToken) =>
        _connectionFactory.OpenAsync(cancellationToken);

    protected CommandDefinition CreateCommand(
        string commandText,
        object? parameters,
        CancellationToken cancellationToken,
        SqlTransaction? transaction = null) =>
        SqlCommandFactory.Create(commandText, parameters, _storagePolicy, cancellationToken, transaction);

    /// <summary>
    /// Führt genau eine Zustandsänderung eines Working Snapshots atomar aus. Die
    /// Transaction-Zeile bleibt bis zum Commit gesperrt, damit parallele Mutationen
    /// derselben fachlichen Transaction nicht ineinander laufen.
    /// </summary>
    protected async Task<SqlWorkingSnapshotMutationExecution<TResult>> ExecuteWorkingSnapshotMutationAsync<TResult>(
        TransactionId transactionId,
        Func<SqlWorkingSnapshotMutationContext, CancellationToken, Task<SqlWorkingSnapshotMutationResult<TResult>>> mutateAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutateAsync);

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var databaseTransaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var guard = await ReadWorkingSnapshotGuardAsync(connection, databaseTransaction, transactionId, cancellationToken)
                .ConfigureAwait(false);
            ValidateWorkingSnapshotMutationGuard(guard, transactionId);

            var context = new SqlWorkingSnapshotMutationContext(
                connection,
                databaseTransaction,
                _storagePolicy,
                new SnapshotId(guard!.WorkingSnapshotId));
            var mutation = await mutateAsync(context, cancellationToken).ConfigureAwait(false);
            var changeVersion = guard.ChangeVersion;

            if (mutation.StateChanged)
            {
                changeVersion = await connection.QuerySingleAsync<long>(
                    CreateCommand(
                        IncrementChangeVersionSql,
                        new { transactionId = transactionId.Value },
                        cancellationToken,
                        databaseTransaction)).ConfigureAwait(false);
            }

            await databaseTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new SqlWorkingSnapshotMutationExecution<TResult>(mutation.Value, changeVersion);
        }
        catch
        {
            await databaseTransaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Liest unter derselben Sperre wie Mutationen den offenen Working-Snapshot-Zustand.</summary>
    protected async Task<WorkingSnapshotGuard?> ReadWorkingSnapshotGuardAsync(
        SqlConnection connection,
        SqlTransaction databaseTransaction,
        TransactionId transactionId,
        CancellationToken cancellationToken)
    {
        var guard = await connection.QuerySingleOrDefaultAsync<WorkingSnapshotMutationGuardRow>(
            CreateCommand(
                LockTransactionSql,
                new { transactionId = transactionId.Value },
                cancellationToken,
                databaseTransaction)).ConfigureAwait(false);
        return guard is null
            ? null
            : new WorkingSnapshotGuard(
                guard.WorkingSnapshotId,
                guard.TransactionState,
                guard.SnapshotState,
                guard.ChangeVersion);
    }

    protected static void ValidateWorkingSnapshotMutationGuard(
        WorkingSnapshotGuard? guard,
        TransactionId transactionId)
    {
        if (guard is null)
            throw new WorkingSnapshotMutationRejectedException(
                "TransactionNotFound",
                $"Die Transaction '{transactionId.Value}' existiert nicht.");

        if (!string.Equals(guard.TransactionState, "Open", StringComparison.Ordinal))
            throw new WorkingSnapshotMutationRejectedException(
                "TransactionClosed",
                $"Die Transaction '{transactionId.Value}' ist nicht offen.");

        if (!string.Equals(guard.SnapshotState, "Working", StringComparison.Ordinal))
            throw new WorkingSnapshotMutationRejectedException(
                "WorkingSnapshotNotOpen",
                $"Der Working Snapshot der Transaction '{transactionId.Value}' ist nicht bearbeitbar.");
    }

    protected sealed record WorkingSnapshotGuard(
        long WorkingSnapshotId,
        string TransactionState,
        string SnapshotState,
        long ChangeVersion);

    private sealed class WorkingSnapshotMutationGuardRow
    {
        public long WorkingSnapshotId { get; init; }

        public string TransactionState { get; init; } = string.Empty;

        public string SnapshotState { get; init; } = string.Empty;

        public long ChangeVersion { get; init; }
    }
}
