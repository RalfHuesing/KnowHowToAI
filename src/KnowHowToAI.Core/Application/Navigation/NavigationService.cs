using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Transportneutrale Orchestrierung der Navigation-Use-Cases (get_root, get_node,
/// list_children, list_roles). Liest ausschließlich, verändert keinen Snapshot-Zustand.
/// Working-Snapshot-Reads nutzen die konsistente atomare M5-Read-Sicht.
/// </summary>
public sealed class NavigationService
{
    private readonly NavigationRepositories _repos;
    private readonly RetrievalPolicy _retrievalPolicy;

    public NavigationService(
        NavigationRepositories repositories,
        RetrievalPolicy retrievalPolicy)
    {
        _repos = repositories ?? throw new ArgumentNullException(nameof(repositories));
        _retrievalPolicy = retrievalPolicy ?? throw new ArgumentNullException(nameof(retrievalPolicy));
    }

    /// <summary>
    /// Liefert den aktiven Root-Node mit aufgelöstem Rollen-Content.
    /// Gibt <c>availability = None</c> und <c>Node = null</c> zurück, wenn der Snapshot noch keinen Root besitzt.
    /// </summary>
    public async Task<Result<NodeWithContent>> GetRootAsync(
        ReadContext context,
        RoleId roleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var loadResult = await LoadNavigationSnapshotDataAsync(context, cancellationToken).ConfigureAwait(false);
        if (!loadResult.IsSuccess)
            return Result<NodeWithContent>.Failure(loadResult.Error!);

        var (_, snapshotData) = loadResult.Value!;
        var root = snapshotData.Nodes.FirstOrDefault(node => node.ParentNodeId is null);

        if (root is null)
        {
            var validationResult = NodeContentResolver.Resolve(new NodeContentResolutionRequest(
                null,
                roleId,
                snapshotData.SnapshotId,
                snapshotData.Roles,
                snapshotData.Resolutions,
                Array.Empty<Domain.Content.NodeContent>(),
                Array.Empty<ContentDependency>()));

            if (!validationResult.IsSuccess)
                return Result<NodeWithContent>.Failure(validationResult.Error!);

            return Result<NodeWithContent>.Success(
                new NodeWithContent(
                    Node: null,
                    RequestedRoleId: roleId,
                    ResolvedRoleId: null,
                    Availability: Availability.None,
                    FallbackUsed: false,
                    Content: null,
                    Freshness: Freshness.Unknown));
        }

        return BuildNodeWithContent(root, roleId, snapshotData);
    }

    /// <summary>Liefert eine einzelne Node mit aufgelöstem Rollen-Content.</summary>
    public async Task<Result<NodeWithContent>> GetNodeAsync(
        NodeId nodeId,
        ReadContext context,
        RoleId roleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var loadResult = await LoadNavigationSnapshotDataAsync(context, cancellationToken).ConfigureAwait(false);
        if (!loadResult.IsSuccess)
            return Result<NodeWithContent>.Failure(loadResult.Error!);

        var (_, snapshotData) = loadResult.Value!;
        var node = snapshotData.Nodes.FirstOrDefault(n => n.NodeId == nodeId);

        if (node is null)
            return Result<NodeWithContent>.Failure(new DomainError(
                NavigationErrorCodes.NodeNotFound,
                "Die angefragte Node existiert nicht.",
                new Dictionary<string, string> { [NavigationErrorCodes.NodeIdDetail] = nodeId.ToString() }));

        return BuildNodeWithContent(node, roleId, snapshotData);
    }

    /// <summary>
    /// Paginierte, deterministisch sortierte Kind-Nodes (SortOrder, dann NodeId).
    /// Gibt Metadaten-first zurück – kein vollständiger Content, nur Größe und Availability.
    /// </summary>
    public async Task<Result<ChildrenPage>> ListChildrenAsync(
        ListChildrenQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var loadResult = await LoadNavigationSnapshotDataAsync(query.Context, cancellationToken).ConfigureAwait(false);
        if (!loadResult.IsSuccess)
            return Result<ChildrenPage>.Failure(loadResult.Error!);

        var (resolvedContext, snapshotData) = loadResult.Value!;
        var effectiveLimit = query.Limit is { } limit && limit > 0
            ? Math.Min(limit, _retrievalPolicy.MaximumPageSize)
            : _retrievalPolicy.DefaultPageSize;

        var roleValidation = ValidateRoleInSnapshot(query.RoleId, snapshotData);
        if (!roleValidation.IsSuccess)
            return Result<ChildrenPage>.Failure(roleValidation.Error!);

        var children = snapshotData.Nodes
            .Where(node => node.ParentNodeId == query.ParentNodeId)
            .OrderBy(node => node.SortOrder)
            .ThenBy(node => node.NodeId.Value)
            .ToArray();
        var startIndexResult = ResolveStartIndex(query, resolvedContext, children);
        if (!startIndexResult.IsSuccess)
            return Result<ChildrenPage>.Failure(startIndexResult.Error!);

        var page = children.Skip(startIndexResult.Value).Take(effectiveLimit + 1).ToArray();
        var hasNext = page.Length > effectiveLimit;
        var pageItems = page.Take(effectiveLimit).ToArray();
        var summariesResult = BuildChildSummaries(pageItems, query.RoleId, snapshotData);
        if (!summariesResult.IsSuccess)
            return Result<ChildrenPage>.Failure(summariesResult.Error!);

        var nextCursor = hasNext && pageItems.Length > 0
            ? new NavigationCursor(
                resolvedContext.SnapshotId,
                resolvedContext.ChangeVersion,
                query.ParentNodeId,
                query.RoleId,
                resolvedContext.IncludeDeleted,
                pageItems[^1].NodeId,
                pageItems[^1].SortOrder).Encode()
            : null;

        return Result<ChildrenPage>.Success(new ChildrenPage(query.ParentNodeId, summariesResult.Value!, nextCursor));
    }

    /// <summary>
    /// Paginierte, deterministisch sortierte Rollen (RoleId.Value ordinal aufsteigend).
    /// </summary>
    public async Task<Result<RolePage>> ListRolesAsync(
        ListRolesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var loadResult = await LoadNavigationSnapshotDataAsync(query.Context, cancellationToken).ConfigureAwait(false);
        if (!loadResult.IsSuccess)
            return Result<RolePage>.Failure(loadResult.Error!);

        var (resolvedContext, snapshotData) = loadResult.Value!;
        var effectiveLimit = query.Limit is { } limit && limit > 0
            ? Math.Min(limit, _retrievalPolicy.MaximumPageSize)
            : _retrievalPolicy.DefaultPageSize;

        var activeRoles = ActiveReadFilter.Apply(snapshotData.Roles, resolvedContext);
        var orderedRoles = activeRoles
            .OrderBy(r => r.RoleId.Value, StringComparer.Ordinal)
            .ToArray();

        var startIndexResult = ResolveRoleStartIndex(query, resolvedContext, orderedRoles);
        if (!startIndexResult.IsSuccess)
            return Result<RolePage>.Failure(startIndexResult.Error!);

        var page = orderedRoles.Skip(startIndexResult.Value).Take(effectiveLimit + 1).ToArray();
        var hasNext = page.Length > effectiveLimit;
        var pageItems = page.Take(effectiveLimit).ToArray();

        var nextCursor = hasNext && pageItems.Length > 0
            ? new RoleCursor(
                resolvedContext.SnapshotId,
                resolvedContext.ChangeVersion,
                resolvedContext.IncludeDeleted,
                pageItems[^1].RoleId).Encode()
            : null;

        return Result<RolePage>.Success(new RolePage(pageItems, nextCursor));
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private sealed record NavigationSnapshotData(
        SnapshotId SnapshotId,
        IReadOnlyList<Node> Nodes,
        IReadOnlyList<Role> Roles,
        IReadOnlyList<RoleResolution> Resolutions,
        IReadOnlyList<Domain.Content.NodeContent> Contents,
        IReadOnlyList<ContentDependency> Dependencies,
        long? ChangeVersion = null);

    private async Task<Result<(ResolvedReadContext Context, NavigationSnapshotData Data)>> LoadNavigationSnapshotDataAsync(
        ReadContext context,
        CancellationToken cancellationToken)
    {
        if (context.TransactionId is { } transactionId && context.SnapshotId is { } requestedSnapshotId)
        {
            return Result<(ResolvedReadContext Context, NavigationSnapshotData Data)>.Failure(new DomainError(
                ReadContextErrorCodes.InvalidReadContext,
                "Ein Read-Kontext darf nicht gleichzeitig eine Transaction und einen Snapshot selektieren.",
                new Dictionary<string, string>
                {
                    ["transactionId"] = transactionId.ToString(),
                    ["snapshotId"] = requestedSnapshotId.ToString()
                }));
        }

        if (context.TransactionId is { } workingTransactionId && _repos.WorkingSnapshots is not null)
        {
            var workingResult = await _repos.WorkingSnapshots.ReadOpenWorkingAsync(workingTransactionId, cancellationToken).ConfigureAwait(false);
            if (!workingResult.IsSuccess)
                return Result<(ResolvedReadContext Context, NavigationSnapshotData Data)>.Failure(workingResult.Error!);

            var workingData = workingResult.Value!;
            var resolvedContext = new ResolvedReadContext(
                workingData.Transaction.WorkingSnapshotId,
                ReadContextSource.Transaction,
                workingTransactionId,
                context.IncludeDeleted,
                workingData.ChangeVersion);

            var snapshotData = new NavigationSnapshotData(
                workingData.Transaction.WorkingSnapshotId,
                ActiveReadFilter.Apply(workingData.Nodes, resolvedContext),
                workingData.Roles,
                workingData.RoleResolutions,
                ActiveReadFilter.Apply(workingData.Contents, resolvedContext),
                workingData.Dependencies,
                workingData.ChangeVersion);

            return Result<(ResolvedReadContext Context, NavigationSnapshotData Data)>.Success((resolvedContext, snapshotData));
        }

        var contextResult = await ReadContextReader.ResolveAsync(context, _repos.Snapshots, _repos.Transactions, cancellationToken).ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Result<(ResolvedReadContext Context, NavigationSnapshotData Data)>.Failure(contextResult.Error!);

        var normalContext = contextResult.Value!;
        var snapshotId = normalContext.SnapshotId;
        var nodes = await _repos.Hierarchy.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var roles = await _repos.Roles.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var resolutions = await _repos.Roles.ListResolutionsBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var contents = await _repos.Contents.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var dependencies = await _repos.Dependencies.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        var normalData = new NavigationSnapshotData(
            snapshotId,
            ActiveReadFilter.Apply(nodes, normalContext),
            roles,
            resolutions,
            ActiveReadFilter.Apply(contents, normalContext),
            dependencies,
            normalContext.ChangeVersion);

        return Result<(ResolvedReadContext Context, NavigationSnapshotData Data)>.Success((normalContext, normalData));
    }

    private static Result<NodeWithContent> BuildNodeWithContent(
        Node node,
        RoleId roleId,
        NavigationSnapshotData data)
    {
        var resolutionResult = NodeContentResolver.Resolve(new NodeContentResolutionRequest(
            node.NodeId,
            roleId,
            data.SnapshotId,
            data.Roles,
            data.Resolutions,
            data.Contents,
            data.Dependencies));

        if (!resolutionResult.IsSuccess)
            return Result<NodeWithContent>.Failure(resolutionResult.Error!);

        var resolution = resolutionResult.Value!;
        return Result<NodeWithContent>.Success(new NodeWithContent(
            node,
            roleId,
            resolution.ResolvedRole,
            resolution.Availability,
            resolution.FallbackUsed,
            resolution.Content,
            resolution.Freshness));
    }

    private static Result<int> ResolveStartIndex(
        ListChildrenQuery query,
        ResolvedReadContext context,
        Node[] children)
    {
        if (query.Cursor is null)
            return Result<int>.Success(0);

        var cursor = NavigationCursor.TryDecode(query.Cursor);
        if (cursor is null)
            return Result<int>.Failure(CreateCursorError(
                NavigationErrorCodes.InvalidCursor,
                "Der Cursor ist ungültig oder abgelaufen.",
                query.Cursor));

        var bindingError = ValidateCursorBinding(cursor, query, context);
        if (bindingError is not null)
            return Result<int>.Failure(bindingError);

        var foundIndex = Array.FindIndex(children, node =>
            node.NodeId == cursor.LastNodeId && node.SortOrder == cursor.LastSortOrder);
        return foundIndex >= 0
            ? Result<int>.Success(foundIndex + 1)
            : Result<int>.Failure(CreateCursorError(
                NavigationErrorCodes.InvalidCursor,
                "Der Cursor gehört nicht zu diesem Ergebnis.",
                query.Cursor));
    }

    private static DomainError? ValidateCursorBinding(
        NavigationCursor cursor,
        ListChildrenQuery query,
        ResolvedReadContext context)
    {
        if (cursor.SnapshotId != context.SnapshotId)
        {
            return context.Source == ReadContextSource.Current
                ? CreateCursorError(
                    NavigationErrorCodes.CursorExpired,
                    "Der Cursor ist nach einer zwischenzeitlichen Aktualisierung des aktuellen Snapshots abgelaufen.",
                    query.Cursor!)
                : CreateCursorError(
                    NavigationErrorCodes.InvalidCursor,
                    "Der Cursor gehört nicht zu diesem Snapshot.",
                    query.Cursor!);
        }

        if (cursor.ParentNodeId != query.ParentNodeId
            || cursor.RoleId != query.RoleId
            || cursor.IncludeDeleted != context.IncludeDeleted)
        {
            return CreateCursorError(
                NavigationErrorCodes.InvalidCursor,
                "Der Cursor gehört nicht zu diesem Ergebnis.",
                query.Cursor!);
        }

        return cursor.ChangeVersion != context.ChangeVersion
            ? CreateCursorError(
                NavigationErrorCodes.CursorExpired,
                "Der Cursor ist nach einer zwischenzeitlichen Mutation der Transaktion abgelaufen.",
                query.Cursor!)
            : null;
    }

    private static Result<int> ResolveRoleStartIndex(
        ListRolesQuery query,
        ResolvedReadContext context,
        Role[] roles)
    {
        if (query.Cursor is null)
            return Result<int>.Success(0);

        var cursor = RoleCursor.TryDecode(query.Cursor);
        if (cursor is null)
            return Result<int>.Failure(CreateCursorError(
                NavigationErrorCodes.InvalidCursor,
                "Der Cursor ist ungültig oder abgelaufen.",
                query.Cursor));

        var bindingError = ValidateRoleCursorBinding(cursor, query, context);
        if (bindingError is not null)
            return Result<int>.Failure(bindingError);

        var foundIndex = Array.FindIndex(roles, r => r.RoleId == cursor.LastRoleId);
        return foundIndex >= 0
            ? Result<int>.Success(foundIndex + 1)
            : Result<int>.Failure(CreateCursorError(
                NavigationErrorCodes.InvalidCursor,
                "Der Cursor gehört nicht zu diesem Ergebnis.",
                query.Cursor));
    }

    private static DomainError? ValidateRoleCursorBinding(
        RoleCursor cursor,
        ListRolesQuery query,
        ResolvedReadContext context)
    {
        if (cursor.SnapshotId != context.SnapshotId)
        {
            return context.Source == ReadContextSource.Current
                ? CreateCursorError(
                    NavigationErrorCodes.CursorExpired,
                    "Der Cursor ist nach einer zwischenzeitlichen Aktualisierung des aktuellen Snapshots abgelaufen.",
                    query.Cursor!)
                : CreateCursorError(
                    NavigationErrorCodes.InvalidCursor,
                    "Der Cursor gehört nicht zu diesem Snapshot.",
                    query.Cursor!);
        }

        if (cursor.IncludeDeleted != context.IncludeDeleted)
        {
            return CreateCursorError(
                NavigationErrorCodes.InvalidCursor,
                "Der Cursor gehört nicht zu diesem Ergebnis.",
                query.Cursor!);
        }

        return cursor.ChangeVersion != context.ChangeVersion
            ? CreateCursorError(
                NavigationErrorCodes.CursorExpired,
                "Der Cursor ist nach einer zwischenzeitlichen Mutation der Transaktion abgelaufen.",
                query.Cursor!)
            : null;
    }

    private static DomainError CreateCursorError(string code, string message, string cursor) =>
        new(code, message, new Dictionary<string, string>
        {
            [NavigationErrorCodes.CursorDetail] = cursor
        });

    private static Result<ResolvedNodeContent> ValidateRoleInSnapshot(RoleId roleId, NavigationSnapshotData data) =>
        NodeContentResolver.Resolve(new NodeContentResolutionRequest(
            null,
            roleId,
            data.SnapshotId,
            data.Roles,
            data.Resolutions,
            Array.Empty<Domain.Content.NodeContent>(),
            Array.Empty<ContentDependency>()));

    private static Result<IReadOnlyList<ChildNodeSummary>> BuildChildSummaries(
        Node[] pageItems,
        RoleId roleId,
        NavigationSnapshotData data)
    {
        var summaries = new List<ChildNodeSummary>(pageItems.Length);
        foreach (var node in pageItems)
        {
            var summaryResult = BuildChildSummary(node, roleId, data);
            if (!summaryResult.IsSuccess)
                return Result<IReadOnlyList<ChildNodeSummary>>.Failure(summaryResult.Error!);
            summaries.Add(summaryResult.Value!);
        }

        return Result<IReadOnlyList<ChildNodeSummary>>.Success(summaries.AsReadOnly());
    }

    private static Result<ChildNodeSummary> BuildChildSummary(
        Node node,
        RoleId roleId,
        NavigationSnapshotData data)
    {
        var childCount = data.Nodes.Count(n => n.ParentNodeId == node.NodeId);
        var resolutionResult = NodeContentResolver.Resolve(new NodeContentResolutionRequest(
            node.NodeId,
            roleId,
            data.SnapshotId,
            data.Roles,
            data.Resolutions,
            data.Contents,
            data.Dependencies));

        if (!resolutionResult.IsSuccess)
            return Result<ChildNodeSummary>.Failure(resolutionResult.Error!);

        var resolution = resolutionResult.Value!;
        var contentSizeBytes = resolution.Content is not null
            ? System.Text.Encoding.UTF8.GetByteCount(resolution.Content.ContentMd)
            : 0;

        return Result<ChildNodeSummary>.Success(new ChildNodeSummary(
            node.NodeId,
            node.Title,
            node.Description,
            node.SortOrder,
            childCount,
            contentSizeBytes,
            resolution.Availability,
            resolution.ResolvedRole,
            resolution.Freshness));
    }
}
