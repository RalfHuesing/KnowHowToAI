using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using KnowHowToAI.Storage.SqlServer.Repositories;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;

/// <summary>Persistiert Content und Provenienz atomar innerhalb der Working-Snapshot-Sperre.</summary>
internal sealed class SqlContentMutationRepository : SqlRepository, IContentMutationRepository
{
    private const string ListNodesSql = """
        SELECT SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted
        FROM dbo.KnowHowToAI_Node WHERE SnapshotId = @snapshotId;
        """;
    private const string ListRolesSql = """
        SELECT SnapshotId, RoleId, Name, Description, IsDeleted
        FROM dbo.KnowHowToAI_Role WHERE SnapshotId = @snapshotId;
        """;
    private const string ListContentsSql = """
        SELECT SnapshotId, NodeId, RoleId, ContentRevisionId, ContentMode, ContentMd, IsDeleted
        FROM dbo.KnowHowToAI_NodeContent WHERE SnapshotId = @snapshotId;
        """;
    private const string ListDependenciesSql = """
        SELECT SnapshotId, TargetNodeId, TargetRoleId, SourceNodeId, SourceRoleId, SourceContentRevisionId
        FROM dbo.KnowHowToAI_ContentDependency WHERE SnapshotId = @snapshotId;
        """;
    private const string InsertContentSql = """
        INSERT INTO dbo.KnowHowToAI_NodeContent
            (SnapshotId, NodeId, RoleId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
        VALUES (@snapshotId, @nodeId, @roleId, @contentRevisionId, @contentMode, @contentMd, @isDeleted);
        """;
    private const string UpdateContentSql = """
        UPDATE dbo.KnowHowToAI_NodeContent
        SET ContentRevisionId = @contentRevisionId, ContentMode = @contentMode,
            ContentMd = @contentMd, IsDeleted = @isDeleted
        WHERE SnapshotId = @snapshotId AND NodeId = @nodeId AND RoleId = @roleId;
        """;
    private const string DeleteDependenciesSql =
        "DELETE FROM dbo.KnowHowToAI_ContentDependency WHERE SnapshotId = @snapshotId;";
    private const string InsertDependencySql = """
        INSERT INTO dbo.KnowHowToAI_ContentDependency
            (SnapshotId, TargetNodeId, TargetRoleId, SourceNodeId, SourceRoleId, SourceContentRevisionId)
        VALUES (@snapshotId, @targetNodeId, @targetRoleId, @sourceNodeId, @sourceRoleId, @sourceContentRevisionId);
        """;

    public SqlContentMutationRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
        : base(connectionFactory, storagePolicy)
    {
    }

    public async Task<Result<WorkingContentMutationExecution<T>>> ExecuteAsync<T>(
        TransactionId transactionId,
        Func<WorkingContentMutationState, Result<WorkingContentMutationDecision<T>>> mutate,
        long expectedChangeVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        WorkingContentMutationState? previousState = null;

        try
        {
            var execution = await ExecuteWorkingSnapshotMutationAsync(
                transactionId,
                async (context, token) =>
                {
                    previousState = await ReadStateAsync(context, token).ConfigureAwait(false);
                    var decisionResult = mutate(previousState);
                    if (!decisionResult.IsSuccess)
                    {
                        return new SqlWorkingSnapshotMutationResult<Result<WorkingContentMutationDecision<T>>>(
                            Result<WorkingContentMutationDecision<T>>.Failure(decisionResult.Error!, decisionResult.Warnings),
                            StateChanged: false);
                    }

                    var decision = decisionResult.Value!;
                    var stateChanged = !StateEquals(previousState, decision.State);
                    if (stateChanged)
                        await SaveStateAsync(context, previousState, decision.State, token).ConfigureAwait(false);

                    return new SqlWorkingSnapshotMutationResult<Result<WorkingContentMutationDecision<T>>>(
                        Result<WorkingContentMutationDecision<T>>.Success(decision),
                        stateChanged);
                },
                cancellationToken: cancellationToken,
                expectedChangeVersion: expectedChangeVersion).ConfigureAwait(false);

            if (!execution.Value.IsSuccess)
                return Result<WorkingContentMutationExecution<T>>.Failure(execution.Value.Error!, execution.Value.Warnings);

            var decision = execution.Value.Value!;
            return Result<WorkingContentMutationExecution<T>>.Success(new WorkingContentMutationExecution<T>(
                decision.Value,
                decision.State.SnapshotId,
                execution.ChangeVersion,
                previousState!,
                decision.State));
        }
        catch (WorkingSnapshotMutationRejectedException exception)
        {
            var details = new Dictionary<string, string> { ["transactionId"] = transactionId.ToString() };
            if (exception.ExpectedChangeVersion is { } expected)
                details[TransactionValidationErrorCodes.ExpectedChangeVersionDetail] = expected.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (exception.ActualChangeVersion is { } actual)
                details[TransactionValidationErrorCodes.ActualChangeVersionDetail] = actual.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return Result<WorkingContentMutationExecution<T>>.Failure(new DomainError(
                exception.Code,
                exception.Message,
                details));
        }
    }

    private static async Task<WorkingContentMutationState> ReadStateAsync(
        SqlWorkingSnapshotMutationContext context,
        CancellationToken cancellationToken)
    {
        var parameters = new { snapshotId = context.WorkingSnapshotId.Value };
        var nodeRows = await context.QueryAsync<NodeRow>(ListNodesSql, parameters, cancellationToken).ConfigureAwait(false);
        var roleRows = await context.QueryAsync<RoleRow>(ListRolesSql, parameters, cancellationToken).ConfigureAwait(false);
        var contentRows = await context.QueryAsync<NodeContentRow>(ListContentsSql, parameters, cancellationToken).ConfigureAwait(false);
        var dependencyRows = await context.QueryAsync<ContentDependencyRow>(ListDependenciesSql, parameters, cancellationToken).ConfigureAwait(false);
        return new WorkingContentMutationState(
            context.WorkingSnapshotId,
            nodeRows.Select(SqlRowMapper.ToNode).ToArray(),
            roleRows.Select(SqlRowMapper.ToRole).ToArray(),
            contentRows.Select(SqlRowMapper.ToNodeContent).ToArray(),
            dependencyRows.Select(SqlRowMapper.ToContentDependency).ToArray());
    }

    private static bool StateEquals(WorkingContentMutationState left, WorkingContentMutationState right) =>
        SetEquals(left.Contents, right.Contents) && SetEquals(left.Dependencies, right.Dependencies);

    private static bool SetEquals<T>(IReadOnlyList<T> left, IReadOnlyList<T> right) where T : notnull =>
        left.Count == right.Count && new HashSet<T>(left).SetEquals(right);

    private static async Task SaveStateAsync(
        SqlWorkingSnapshotMutationContext context,
        WorkingContentMutationState previousState,
        WorkingContentMutationState currentState,
        CancellationToken cancellationToken)
    {
        var previousByKey = previousState.Contents.ToDictionary(content => (content.NodeId, content.RoleId));
        var inserts = currentState.Contents.Where(content => !previousByKey.ContainsKey((content.NodeId, content.RoleId)))
            .Select(SqlMutationParameterMapper.ToContentParameters).ToArray();
        var updates = currentState.Contents.Where(content => previousByKey.TryGetValue((content.NodeId, content.RoleId), out var previous) && previous != content)
            .Select(SqlMutationParameterMapper.ToContentParameters).ToArray();
        if (inserts.Length > 0)
            await context.ExecuteAsync(InsertContentSql, inserts, cancellationToken).ConfigureAwait(false);
        if (updates.Length > 0)
            await context.ExecuteAsync(UpdateContentSql, updates, cancellationToken).ConfigureAwait(false);

        if (!SetEquals(previousState.Dependencies, currentState.Dependencies))
        {
            await context.ExecuteAsync(DeleteDependenciesSql, new { snapshotId = context.WorkingSnapshotId.Value }, cancellationToken).ConfigureAwait(false);
            if (currentState.Dependencies.Count > 0)
            {
                await context.ExecuteAsync(
                    InsertDependencySql,
                    currentState.Dependencies.Select(SqlMutationParameterMapper.ToDependencyParameters),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

}
