using Dapper;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using KnowHowToAI.Storage.SqlServer.Repositories;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Transactions;

internal sealed class SqlTransactionRepository : SqlRepository, ITransactionRepository
{
    private const string BeginSql = """
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;

        BEGIN TRY
            DECLARE @baseSnapshotId BIGINT;
            DECLARE @workingSnapshotId BIGINT;
            DECLARE @workingSnapshot TABLE (SnapshotId BIGINT NOT NULL);

            SELECT @baseSnapshotId = CurrentSnapshotId
            FROM dbo.KnowHowToAI_SystemState WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = 1;

            IF @baseSnapshotId IS NULL
                THROW 50000, 'KnowHowToAI_SystemState enthält keinen aktuellen Snapshot.', 1;

            INSERT INTO dbo.KnowHowToAI_Snapshot (BaseSnapshotId, State)
            OUTPUT inserted.SnapshotId INTO @workingSnapshot (SnapshotId)
            VALUES (@baseSnapshotId, 'Working');

            SELECT @workingSnapshotId = SnapshotId
            FROM @workingSnapshot;

            INSERT INTO dbo.KnowHowToAI_Audience (SnapshotId, AudienceId, Name, Description, IsDeleted)
            SELECT @workingSnapshotId, AudienceId, Name, Description, IsDeleted
            FROM dbo.KnowHowToAI_Audience
            WHERE SnapshotId = @baseSnapshotId;

            INSERT INTO dbo.KnowHowToAI_AudienceResolution (SnapshotId, RequestedAudienceId, CandidateAudienceId, Priority)
            SELECT @workingSnapshotId, RequestedAudienceId, CandidateAudienceId, Priority
            FROM dbo.KnowHowToAI_AudienceResolution
            WHERE SnapshotId = @baseSnapshotId;

            INSERT INTO dbo.KnowHowToAI_Node (SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
            SELECT @workingSnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted
            FROM dbo.KnowHowToAI_Node
            WHERE SnapshotId = @baseSnapshotId;

            INSERT INTO dbo.KnowHowToAI_NodeContent (
                SnapshotId, NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
            SELECT @workingSnapshotId, NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd, IsDeleted
            FROM dbo.KnowHowToAI_NodeContent
            WHERE SnapshotId = @baseSnapshotId;

            INSERT INTO dbo.KnowHowToAI_ContentDependency (
                SnapshotId, TargetNodeId, TargetAudienceId, SourceNodeId, SourceAudienceId, SourceContentRevisionId)
            SELECT
                @workingSnapshotId,
                TargetNodeId,
                TargetAudienceId,
                SourceNodeId,
                SourceAudienceId,
                SourceContentRevisionId
            FROM dbo.KnowHowToAI_ContentDependency
            WHERE SnapshotId = @baseSnapshotId;

            INSERT INTO dbo.KnowHowToAI_Transaction (
                TransactionId,
                BaseSnapshotId,
                WorkingSnapshotId,
                State,
                Purpose,
                Actor,
                Client)
            VALUES (
                @transactionId,
                @baseSnapshotId,
                @workingSnapshotId,
                'Open',
                @purpose,
                @actor,
                @client);

            COMMIT TRANSACTION;

            SELECT TransactionId, BaseSnapshotId, WorkingSnapshotId, State, ChangeVersion,
                   CreatedAtUtc, CommittedAtUtc, Purpose, Actor, Client, CommitMessage
            FROM dbo.KnowHowToAI_Transaction
            WHERE TransactionId = @transactionId;
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0
                ROLLBACK TRANSACTION;

            THROW;
        END CATCH;
        """;

    private const string FindSql = """
        SELECT TransactionId, BaseSnapshotId, WorkingSnapshotId, State, ChangeVersion,
               CreatedAtUtc, CommittedAtUtc, Purpose, Actor, Client, CommitMessage
        FROM dbo.KnowHowToAI_Transaction
        WHERE TransactionId = @transactionId;
        """;

    private const string LockOpenWorkingGuardSql = """
        SELECT transactionRow.BaseSnapshotId, transactionRow.WorkingSnapshotId,
               transactionRow.State AS TransactionState, snapshotRow.State AS SnapshotState
        FROM dbo.KnowHowToAI_Transaction AS transactionRow WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN dbo.KnowHowToAI_Snapshot AS snapshotRow WITH (UPDLOCK, HOLDLOCK)
            ON snapshotRow.SnapshotId = transactionRow.WorkingSnapshotId
        WHERE transactionRow.TransactionId = @transactionId;
        """;

    private const string LockCurrentSnapshotSql = """
        SELECT CurrentSnapshotId
        FROM dbo.KnowHowToAI_SystemState WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = 1;
        """;

    private const string ListNodesSql = """
        SELECT SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted
        FROM dbo.KnowHowToAI_Node
        WHERE SnapshotId = @snapshotId
        ORDER BY SortOrder, NodeId;
        """;

    private const string ListAudiencesSql = """
        SELECT SnapshotId, AudienceId, Name, Description, IsDeleted
        FROM dbo.KnowHowToAI_Audience
        WHERE SnapshotId = @snapshotId
        ORDER BY AudienceId;
        """;

    private const string ListAudienceResolutionsSql = """
        SELECT SnapshotId, RequestedAudienceId, CandidateAudienceId, Priority
        FROM dbo.KnowHowToAI_AudienceResolution
        WHERE SnapshotId = @snapshotId
        ORDER BY RequestedAudienceId, Priority;
        """;

    private const string ListContentsSql = """
        SELECT SnapshotId, NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd, IsDeleted
        FROM dbo.KnowHowToAI_NodeContent
        WHERE SnapshotId = @snapshotId
        ORDER BY NodeId, AudienceId;
        """;

    private const string ListDependenciesSql = """
        SELECT SnapshotId, TargetNodeId, TargetAudienceId, SourceNodeId, SourceAudienceId, SourceContentRevisionId
        FROM dbo.KnowHowToAI_ContentDependency
        WHERE SnapshotId = @snapshotId
        ORDER BY TargetNodeId, TargetAudienceId, SourceNodeId, SourceAudienceId;
        """;

    private const string CommitSnapshotSql = """
        UPDATE dbo.KnowHowToAI_Snapshot
        SET State = 'Committed', CommittedAtUtc = @committedAtUtc
        WHERE SnapshotId = @workingSnapshotId AND State = 'Working';
        """;

    private const string ActivateCurrentSnapshotSql = """
        UPDATE dbo.KnowHowToAI_SystemState
        SET CurrentSnapshotId = @workingSnapshotId, LastUpdatedUtc = @committedAtUtc
        WHERE Id = 1;
        """;

    private const string CommitTransactionSql = """
        UPDATE dbo.KnowHowToAI_Transaction
        SET State = 'Committed', CommittedAtUtc = @committedAtUtc, CommitMessage = @commitMessage
        WHERE TransactionId = @transactionId AND State = 'Open';
        """;

    private const string DiscardSnapshotSql = """
        UPDATE dbo.KnowHowToAI_Snapshot
        SET State = 'Discarded'
        WHERE SnapshotId = @workingSnapshotId AND State = 'Working';
        """;

    private const string DiscardTransactionSql = """
        UPDATE dbo.KnowHowToAI_Transaction
        SET State = 'Discarded'
        WHERE TransactionId = @transactionId AND State = 'Open';
        """;

    private const string ReadTransactionSql = """
        SELECT TransactionId, BaseSnapshotId, WorkingSnapshotId, State, ChangeVersion,
               CreatedAtUtc, CommittedAtUtc, Purpose, Actor, Client, CommitMessage
        FROM dbo.KnowHowToAI_Transaction
        WHERE TransactionId = @transactionId;
        """;

    private const string ListOpenTransactionsSql = """
        SELECT TransactionId, BaseSnapshotId, WorkingSnapshotId, State, ChangeVersion,
               CreatedAtUtc, CommittedAtUtc, Purpose, Actor, Client, CommitMessage
        FROM dbo.KnowHowToAI_Transaction
        WHERE State = 'Open'
        ORDER BY CreatedAtUtc DESC;
        """;

    public SqlTransactionRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
        : base(connectionFactory, storagePolicy)
    {
    }

    public async Task<KnowledgeTransaction> BeginAsync(
        BeginTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QuerySingleAsync<TransactionRow>(
            CreateCommand(
                BeginSql,
                new
                {
                    transactionId = request.TransactionId.Value,
                    request.Purpose,
                    request.Actor,
                    request.Client
                },
                cancellationToken)).ConfigureAwait(false);
        return SqlRowMapper.ToTransaction(row);
    }

    public async Task<KnowledgeTransaction?> FindAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<TransactionRow>(
            CreateCommand(FindSql, new { transactionId = transactionId.Value }, cancellationToken)).ConfigureAwait(false);
        return row is null ? null : SqlRowMapper.ToTransaction(row);
    }

    public async Task<IReadOnlyList<KnowledgeTransaction>> ListOpenAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<TransactionRow>(
            CreateCommand(ListOpenTransactionsSql, null, cancellationToken)).ConfigureAwait(false);
        return rows.Select(SqlRowMapper.ToTransaction).ToArray();
    }

    public async Task<IReadOnlyList<KnowledgeTransaction>> ListOpenAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var sql = ListOpenTransactionsSql.Replace("SELECT", "SELECT TOP (@Limit)", StringComparison.Ordinal);
        var rows = await connection.QueryAsync<TransactionRow>(
            CreateCommand(sql, new { Limit = limit }, cancellationToken)).ConfigureAwait(false);
        return rows.Select(SqlRowMapper.ToTransaction).ToArray();
    }

    public async Task<CommitTransactionResult> CommitAsync(
        CommitTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.QualityWarningThresholds);

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var databaseTransaction = (Microsoft.Data.SqlClient.SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var guardResult = await ReadOpenWorkingGuardAsync(connection, databaseTransaction, request.TransactionId, cancellationToken)
                .ConfigureAwait(false);
            if (guardResult.Error is not null)
                return await RollbackAndReturnAsync(databaseTransaction, CreateRejectedResult(guardResult.Error)).ConfigureAwait(false);

            var report = await ValidateWorkingSnapshotAsync(
                connection,
                databaseTransaction,
                new SnapshotId(guardResult.Guard!.WorkingSnapshotId),
                request,
                cancellationToken).ConfigureAwait(false);
            if (!report.IsValid)
                return await RollbackAndReturnAsync(
                    databaseTransaction,
                    new CommitTransactionResult(null, report, report.Errors[0])).ConfigureAwait(false);

            var currentSnapshotId = await connection.QuerySingleAsync<long>(
                CreateCommand(
                    LockCurrentSnapshotSql,
                    parameters: null,
                    cancellationToken,
                    databaseTransaction)).ConfigureAwait(false);
            if (guardResult.Guard.BaseSnapshotId != currentSnapshotId)
                return await RollbackAndReturnAsync(
                    databaseTransaction,
                    CreateRejectedResult(CreateConflictError(request.TransactionId, guardResult.Guard.BaseSnapshotId, currentSnapshotId), report))
                    .ConfigureAwait(false);

            var transaction = await ActivateSnapshotAsync(connection, databaseTransaction, request, guardResult.Guard, cancellationToken)
                .ConfigureAwait(false);
            await databaseTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new CommitTransactionResult(transaction, report, null);
        }
        catch
        {
            await databaseTransaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<Result<KnowledgeTransaction>> DiscardAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var databaseTransaction = (Microsoft.Data.SqlClient.SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var guardResult = await ReadOpenWorkingGuardAsync(connection, databaseTransaction, transactionId, cancellationToken)
                .ConfigureAwait(false);
            if (guardResult.Error is not null)
                return await RollbackAndReturnAsync(
                    databaseTransaction,
                    Result<KnowledgeTransaction>.Failure(guardResult.Error)).ConfigureAwait(false);

            var parameters = new
            {
                transactionId = transactionId.Value,
                workingSnapshotId = guardResult.Guard!.WorkingSnapshotId
            };
            await EnsureSingleRowAsync(connection, DiscardSnapshotSql, parameters, cancellationToken, databaseTransaction).ConfigureAwait(false);
            await EnsureSingleRowAsync(connection, DiscardTransactionSql, parameters, cancellationToken, databaseTransaction).ConfigureAwait(false);
            var row = await connection.QuerySingleAsync<TransactionRow>(
                CreateCommand(ReadTransactionSql, new { transactionId = transactionId.Value }, cancellationToken, databaseTransaction))
                .ConfigureAwait(false);
            await databaseTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Result<KnowledgeTransaction>.Success(SqlRowMapper.ToTransaction(row));
        }
        catch
        {
            await databaseTransaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<OpenWorkingGuardResult> ReadOpenWorkingGuardAsync(
        Microsoft.Data.SqlClient.SqlConnection connection,
        Microsoft.Data.SqlClient.SqlTransaction databaseTransaction,
        TransactionId transactionId,
        CancellationToken cancellationToken)
    {
        var guard = await connection.QuerySingleOrDefaultAsync<OpenWorkingGuardRow>(
            CreateCommand(LockOpenWorkingGuardSql, new { transactionId = transactionId.Value }, cancellationToken, databaseTransaction))
            .ConfigureAwait(false);
        if (guard is null)
            return new(null, CreateTransactionError(
                TransactionValidationErrorCodes.TransactionNotFound,
                "Die angefragte Transaction existiert nicht.",
                transactionId));
        if (!string.Equals(guard.TransactionState, SqlPersistedValues.TransactionOpen, StringComparison.Ordinal))
            return new(null, CreateTransactionError(
                TransactionValidationErrorCodes.TransactionClosed,
                "Die angefragte Transaction ist nicht offen.",
                transactionId));
        if (!string.Equals(guard.SnapshotState, SqlPersistedValues.SnapshotWorking, StringComparison.Ordinal))
            return new(null, CreateTransactionError(
                TransactionValidationErrorCodes.WorkingSnapshotNotOpen,
                "Der Working Snapshot der Transaction ist nicht bearbeitbar.",
                transactionId));

        return new(guard, null);
    }

    private async Task<KnowledgeTransaction> ActivateSnapshotAsync(
        Microsoft.Data.SqlClient.SqlConnection connection,
        Microsoft.Data.SqlClient.SqlTransaction databaseTransaction,
        CommitTransactionRequest request,
        OpenWorkingGuardRow guard,
        CancellationToken cancellationToken)
    {
        var committedAtUtc = await connection.QuerySingleAsync<DateTime>(
            CreateCommand("SELECT SYSUTCDATETIME();", parameters: null, cancellationToken, databaseTransaction)).ConfigureAwait(false);
        var parameters = new DynamicParameters();
        parameters.Add("transactionId", request.TransactionId.Value);
        parameters.Add("workingSnapshotId", guard.WorkingSnapshotId);
        parameters.Add("committedAtUtc", committedAtUtc, System.Data.DbType.DateTime2);
        parameters.Add("CommitMessage", request.CommitMessage);
        await EnsureSingleRowAsync(connection, CommitSnapshotSql, parameters, cancellationToken, databaseTransaction).ConfigureAwait(false);
        await EnsureSingleRowAsync(connection, ActivateCurrentSnapshotSql, parameters, cancellationToken, databaseTransaction).ConfigureAwait(false);
        await EnsureSingleRowAsync(connection, CommitTransactionSql, parameters, cancellationToken, databaseTransaction).ConfigureAwait(false);
        var row = await connection.QuerySingleAsync<TransactionRow>(
            CreateCommand(ReadTransactionSql, new { transactionId = request.TransactionId.Value }, cancellationToken, databaseTransaction))
            .ConfigureAwait(false);
        return SqlRowMapper.ToTransaction(row);
    }

    private async Task<TransactionValidationReport> ValidateWorkingSnapshotAsync(
        Microsoft.Data.SqlClient.SqlConnection connection,
        Microsoft.Data.SqlClient.SqlTransaction databaseTransaction,
        SnapshotId snapshotId,
        CommitTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var parameters = new { snapshotId = snapshotId.Value };
        var nodes = (await connection.QueryAsync<NodeRow>(CreateCommand(ListNodesSql, parameters, cancellationToken, databaseTransaction)).ConfigureAwait(false))
            .Select(SqlRowMapper.ToNode)
            .ToArray();
        var audiences = (await connection.QueryAsync<AudienceRow>(CreateCommand(ListAudiencesSql, parameters, cancellationToken, databaseTransaction)).ConfigureAwait(false))
            .Select(SqlRowMapper.ToAudience)
            .ToArray();
        var resolutions = (await connection.QueryAsync<AudienceResolutionRow>(CreateCommand(ListAudienceResolutionsSql, parameters, cancellationToken, databaseTransaction)).ConfigureAwait(false))
            .Select(SqlRowMapper.ToAudienceResolution)
            .ToArray();
        var contents = (await connection.QueryAsync<NodeContentRow>(CreateCommand(ListContentsSql, parameters, cancellationToken, databaseTransaction)).ConfigureAwait(false))
            .Select(SqlRowMapper.ToNodeContent)
            .ToArray();
        var dependencies = (await connection.QueryAsync<ContentDependencyRow>(CreateCommand(ListDependenciesSql, parameters, cancellationToken, databaseTransaction)).ConfigureAwait(false))
            .Select(SqlRowMapper.ToContentDependency)
            .ToArray();

        return TransactionValidator.Validate(new TransactionValidationRequest(
            nodes,
            audiences,
            resolutions,
            contents,
            dependencies,
            request.QualityWarningThresholds,
            request.WarnOnPossibleEmbeddedHeading));
    }

    private async Task EnsureSingleRowAsync(
        Microsoft.Data.SqlClient.SqlConnection connection,
        string commandText,
        object parameters,
        CancellationToken cancellationToken,
        Microsoft.Data.SqlClient.SqlTransaction databaseTransaction)
    {
        var affectedRows = await connection.ExecuteAsync(
            CreateCommand(commandText, parameters, cancellationToken, databaseTransaction)).ConfigureAwait(false);
        if (affectedRows != 1)
            throw new InvalidOperationException("Der atomare Zustandswechsel konnte keinen eindeutigen Zustandswechsel durchführen.");
    }

    private static CommitTransactionResult CreateRejectedResult(
        DomainError error,
        TransactionValidationReport? report = null) =>
        new(null, report, error);

    private static async Task<T> RollbackAndReturnAsync<T>(
        System.Data.Common.DbTransaction databaseTransaction,
        T result)
    {
        await databaseTransaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        return result;
    }

    private static DomainError CreateTransactionError(string code, string message, TransactionId transactionId) =>
        new(code, message, new Dictionary<string, string>
        {
            [TransactionValidationErrorCodes.TransactionIdDetail] = transactionId.ToString()
        });

    private static DomainError CreateConflictError(
        TransactionId transactionId,
        long baseSnapshotId,
        long currentSnapshotId) =>
        new(
            TransactionValidationErrorCodes.SnapshotConflict,
            "Der Current Snapshot hat sich seit dem Öffnen der Transaction geändert.",
            new Dictionary<string, string>
            {
                [TransactionValidationErrorCodes.TransactionIdDetail] = transactionId.ToString(),
                [TransactionValidationErrorCodes.BaseSnapshotIdDetail] = baseSnapshotId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [TransactionValidationErrorCodes.CurrentSnapshotIdDetail] = currentSnapshotId.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });

    private sealed record OpenWorkingGuardResult(OpenWorkingGuardRow? Guard, DomainError? Error);

    private sealed record OpenWorkingGuardRow(
        long BaseSnapshotId,
        long WorkingSnapshotId,
        string TransactionState,
        string SnapshotState);
}
