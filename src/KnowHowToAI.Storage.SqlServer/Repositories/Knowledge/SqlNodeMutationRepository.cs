using Dapper;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using KnowHowToAI.Storage.SqlServer.Repositories;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;

/// <summary>Persistiert Node-Mutationen vollständig innerhalb der Working-Snapshot-Sperre.</summary>
internal sealed class SqlNodeMutationRepository : SqlRepository, INodeMutationRepository
{
    private const string ListNodesSql = """
        SELECT SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted
        FROM dbo.KnowHowToAI_Node WHERE SnapshotId = @snapshotId;
        """;
    private const string ListContentsSql = """
        SELECT SnapshotId, NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd, IsDeleted
        FROM dbo.KnowHowToAI_NodeContent WHERE SnapshotId = @snapshotId;
        """;
    private const string ListDependenciesSql = """
        SELECT SnapshotId, TargetNodeId, TargetAudienceId, SourceNodeId, SourceAudienceId, SourceContentRevisionId
        FROM dbo.KnowHowToAI_ContentDependency WHERE SnapshotId = @snapshotId;
        """;
    private const string ListKnownNodeIdsSql = "SELECT DISTINCT NodeId FROM dbo.KnowHowToAI_Node;";
    private const string InsertNodeSql = """
        INSERT INTO dbo.KnowHowToAI_Node (SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
        VALUES (@snapshotId, @nodeId, @parentNodeId, @title, @description, @sortOrder, @isDeleted);
        """;
    private const string UpdateNodeSql = """
        UPDATE dbo.KnowHowToAI_Node
        SET ParentNodeId = @parentNodeId, Title = @title, Description = @description,
            SortOrder = @sortOrder, IsDeleted = @isDeleted
        WHERE SnapshotId = @snapshotId AND NodeId = @nodeId;
        """;
    private const string InsertContentSql = """
        INSERT INTO dbo.KnowHowToAI_NodeContent
            (SnapshotId, NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
        VALUES (@snapshotId, @nodeId, @audienceId, @contentRevisionId, @contentMode, @contentMd, @isDeleted);
        """;
    private const string UpdateContentSql = """
        UPDATE dbo.KnowHowToAI_NodeContent
        SET ContentRevisionId = @contentRevisionId, ContentMode = @contentMode,
            ContentMd = @contentMd, IsDeleted = @isDeleted
        WHERE SnapshotId = @snapshotId AND NodeId = @nodeId AND AudienceId = @audienceId;
        """;
    private const string DeleteDependenciesSql =
        "DELETE FROM dbo.KnowHowToAI_ContentDependency WHERE SnapshotId = @snapshotId;";
    private const string InsertDependencySql = """
        INSERT INTO dbo.KnowHowToAI_ContentDependency
            (SnapshotId, TargetNodeId, TargetAudienceId, SourceNodeId, SourceAudienceId, SourceContentRevisionId)
        VALUES (@snapshotId, @targetNodeId, @targetAudienceId, @sourceNodeId, @sourceAudienceId, @sourceContentRevisionId);
        """;

    public SqlNodeMutationRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
        : base(connectionFactory, storagePolicy)
    {
    }

    public async Task<Result<WorkingNodeMutationExecution<T>>> ExecuteAsync<T>(
        TransactionId transactionId,
        Func<WorkingNodeMutationState, Result<WorkingNodeMutationDecision<T>>> mutate,
        CancellationToken cancellationToken = default,
        long? expectedChangeVersion = null)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        WorkingNodeMutationState? previousState = null;

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
                        return new SqlWorkingSnapshotMutationResult<Result<WorkingNodeMutationDecision<T>>>(
                            Result<WorkingNodeMutationDecision<T>>.Failure(decisionResult.Error!),
                            StateChanged: false);
                    }

                    var decision = decisionResult.Value!;
                    var stateChanged = !StateEquals(previousState, decision.State);
                    if (stateChanged)
                        await SaveStateAsync(context, previousState, decision.State, token).ConfigureAwait(false);

                    return new SqlWorkingSnapshotMutationResult<Result<WorkingNodeMutationDecision<T>>>(
                        Result<WorkingNodeMutationDecision<T>>.Success(decision),
                        stateChanged);
                },
                cancellationToken,
                expectedChangeVersion).ConfigureAwait(false);

            if (!execution.Value.IsSuccess)
                return Result<WorkingNodeMutationExecution<T>>.Failure(execution.Value.Error!);

            var decision = execution.Value.Value!;
            return Result<WorkingNodeMutationExecution<T>>.Success(new WorkingNodeMutationExecution<T>(
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

            return Result<WorkingNodeMutationExecution<T>>.Failure(new DomainError(
                exception.Code,
                exception.Message,
                details));
        }
    }

    private static async Task<WorkingNodeMutationState> ReadStateAsync(
        SqlWorkingSnapshotMutationContext context,
        CancellationToken cancellationToken)
    {
        var parameters = new { snapshotId = context.WorkingSnapshotId.Value };
        var nodeRows = await context.QueryAsync<NodeRow>(ListNodesSql, parameters, cancellationToken).ConfigureAwait(false);
        var contentRows = await context.QueryAsync<NodeContentRow>(ListContentsSql, parameters, cancellationToken).ConfigureAwait(false);
        var dependencyRows = await context.QueryAsync<ContentDependencyRow>(ListDependenciesSql, parameters, cancellationToken).ConfigureAwait(false);
        var knownNodeIds = await context.QueryAsync<Guid>(ListKnownNodeIdsSql, parameters: null, cancellationToken).ConfigureAwait(false);
        return new WorkingNodeMutationState(
            context.WorkingSnapshotId,
            nodeRows.Select(SqlRowMapper.ToNode).ToArray(),
            contentRows.Select(SqlRowMapper.ToNodeContent).ToArray(),
            dependencyRows.Select(SqlRowMapper.ToContentDependency).ToArray(),
            knownNodeIds.Select(id => new NodeId(id)).ToArray());
    }

    private static bool StateEquals(WorkingNodeMutationState left, WorkingNodeMutationState right) =>
        SetEquals(left.Nodes, right.Nodes)
        && SetEquals(left.Contents, right.Contents)
        && SetEquals(left.Dependencies, right.Dependencies);

    private static bool SetEquals<T>(IReadOnlyList<T> left, IReadOnlyList<T> right) where T : notnull =>
        left.Count == right.Count && new HashSet<T>(left).SetEquals(right);

    private static async Task SaveStateAsync(
        SqlWorkingSnapshotMutationContext context,
        WorkingNodeMutationState previousState,
        WorkingNodeMutationState currentState,
        CancellationToken cancellationToken)
    {
        await SaveNodesAsync(context, previousState.Nodes, currentState.Nodes, cancellationToken).ConfigureAwait(false);
        await SaveContentsAsync(context, previousState.Contents, currentState.Contents, cancellationToken).ConfigureAwait(false);
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

    private static async Task SaveNodesAsync(
        SqlWorkingSnapshotMutationContext context,
        IReadOnlyList<Node> previousNodes,
        IReadOnlyList<Node> currentNodes,
        CancellationToken cancellationToken)
    {
        var previousById = previousNodes.ToDictionary(node => node.NodeId);
        var changingNodes = currentNodes
            .Where(node => previousById.TryGetValue(node.NodeId, out var previous) && previous != node)
            .ToArray();
        var inserts = currentNodes
            .Where(node => !previousById.ContainsKey(node.NodeId))
            .Select(node => ToNodeParameters(node))
            .ToArray();

        if (changingNodes.Length > 0)
        {
            await WriteChangedNodesAsync(context, changingNodes, cancellationToken).ConfigureAwait(false);
        }

        if (inserts.Length > 0)
            await context.ExecuteAsync(InsertNodeSql, inserts, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Schreibt geänderte Node-Zeilen zweiphasig: der gefilterte Unique-Index
    /// UQ_KnowHowToAI_Node_ActiveSiblingSortOrder verbietet beim zeilenweisen
    /// Schreiben vorübergehende Doppelte — eine Renummerierung (neue Node vorn,
    /// Verschieben, Rotieren) kollidiert sonst mit dem noch nicht verschobenen
    /// Nachbarn. Alle geänderten Zeilen weichen deshalb zunächst auf eindeutige
    /// Park-SortOrders außerhalb des gültigen Bereichs aus, bevor die Zielwerte
    /// geschrieben werden; danach folgen die neuen Zeilen.
    /// </summary>
    private static async Task WriteChangedNodesAsync(
        SqlWorkingSnapshotMutationContext context,
        IReadOnlyList<Node> changingNodes,
        CancellationToken cancellationToken)
    {
        var parkedRows = changingNodes
            .Select((node, index) => ToNodeParameters(node, ParkedSortOrderBase + index))
            .ToArray();
        var finalRows = changingNodes.Select(node => ToNodeParameters(node)).ToArray();

        await context.ExecuteAsync(UpdateNodeSql, parkedRows, cancellationToken).ConfigureAwait(false);
        await context.ExecuteAsync(UpdateNodeSql, finalRows, cancellationToken).ConfigureAwait(false);
    }

    private static async Task SaveContentsAsync(
        SqlWorkingSnapshotMutationContext context,
        IReadOnlyList<NodeContent> previousContents,
        IReadOnlyList<NodeContent> currentContents,
        CancellationToken cancellationToken)
    {
        var previousByKey = previousContents.ToDictionary(content => (content.NodeId, content.AudienceId));
        var inserts = currentContents.Where(content => !previousByKey.ContainsKey((content.NodeId, content.AudienceId)))
            .Select(SqlMutationParameterMapper.ToContentParameters).ToArray();
        var updates = currentContents.Where(content => previousByKey.TryGetValue((content.NodeId, content.AudienceId), out var previous) && previous != content)
            .Select(SqlMutationParameterMapper.ToContentParameters).ToArray();
        if (inserts.Length > 0)
            await context.ExecuteAsync(InsertContentSql, inserts, cancellationToken).ConfigureAwait(false);
        if (updates.Length > 0)
            await context.ExecuteAsync(UpdateContentSql, updates, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Basis der Park-SortOrders im Zweiphasen-Schreiben; liegt oberhalb jedes
    /// realen Geschwisterfensters und wird in Phase 2 stets überschrieben.</summary>
    private const int ParkedSortOrderBase = 1_000_000_000;

    private static object ToNodeParameters(Node node) => ToNodeParameters(node, node.SortOrder);

    private static object ToNodeParameters(Node node, int sortOrder) => new
    {
        snapshotId = node.SnapshotId.Value,
        nodeId = node.NodeId.Value,
        parentNodeId = node.ParentNodeId?.Value,
        node.Title,
        node.Description,
        sortOrder,
        node.IsDeleted
    };

}
