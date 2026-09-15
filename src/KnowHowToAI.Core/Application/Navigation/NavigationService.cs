using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Transportneutrale Orchestrierung der Navigation-Use-Cases (get_root, get_node,
/// list_children, list_roles). Liest ausschließlich, verändert keinen Snapshot-Zustand.
/// Read-Kontext wird immer über <see cref="ReadContextResolver"/> aufgelöst.
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
    /// Gibt <c>availability = None</c> zurück, wenn der Snapshot noch keinen Root besitzt.
    /// </summary>
    public async Task<Result<NodeWithContent>> GetRootAsync(
        ReadContext context,
        RoleId roleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var contextResult = await ResolveContextAsync(context, cancellationToken).ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Result<NodeWithContent>.Failure(contextResult.Error!);

        var snapshotId = contextResult.Value!.SnapshotId;
        var nodes = await _repos.Hierarchy.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var root = nodes.FirstOrDefault(node => !node.IsDeleted && node.ParentNodeId is null);

        if (root is null)
            return Result<NodeWithContent>.Success(
                new NodeWithContent(
                    Node: new Node(snapshotId, default, null, string.Empty, null, 0, false),
                    RequestedRoleId: roleId,
                    ResolvedRoleId: null,
                    Availability: Availability.None,
                    FallbackUsed: false,
                    Content: null,
                    Freshness: Freshness.Unknown));

        return await BuildNodeWithContentAsync(root, roleId, snapshotId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Liefert eine einzelne Node mit aufgelöstem Rollen-Content.</summary>
    public async Task<Result<NodeWithContent>> GetNodeAsync(
        NodeId nodeId,
        ReadContext context,
        RoleId roleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var contextResult = await ResolveContextAsync(context, cancellationToken).ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Result<NodeWithContent>.Failure(contextResult.Error!);

        var snapshotId = contextResult.Value!.SnapshotId;
        var nodes = await _repos.Hierarchy.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var node = nodes.FirstOrDefault(n => !n.IsDeleted && n.NodeId == nodeId);

        if (node is null)
            return Result<NodeWithContent>.Failure(new DomainError(
                NavigationErrorCodes.NodeNotFound,
                "Die angefragte Node existiert nicht.",
                new Dictionary<string, string> { [NavigationErrorCodes.NodeIdDetail] = nodeId.ToString() }));

        return await BuildNodeWithContentAsync(node, roleId, snapshotId, cancellationToken).ConfigureAwait(false);
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

        var contextResult = await ResolveContextAsync(query.Context, cancellationToken).ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Result<ChildrenPage>.Failure(contextResult.Error!);

        var snapshotId = contextResult.Value!.SnapshotId;
        var effectiveLimit = Math.Min(query.Limit ?? _retrievalPolicy.DefaultPageSize, _retrievalPolicy.MaximumPageSize);

        var nodes = await _repos.Hierarchy.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var roles = await _repos.Roles.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var resolutions = await _repos.Roles.ListResolutionsBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var contents = await _repos.Contents.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var dependencies = await _repos.Dependencies.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        var children = nodes
            .Where(node => !node.IsDeleted && node.ParentNodeId == query.ParentNodeId)
            .OrderBy(node => node.SortOrder)
            .ThenBy(node => node.NodeId.Value)
            .ToArray();

        // Der Cursor ist eine opaque Node-ID aus genau diesem, deterministisch sortierten Resultset.
        NodeId? afterNodeId = null;
        if (query.Cursor is not null)
        {
            var parsed = ParseCursor(query.Cursor);
            if (parsed is null)
                return Result<ChildrenPage>.Failure(new DomainError(
                    NavigationErrorCodes.InvalidCursor,
                    "Der Cursor ist ungültig oder abgelaufen.",
                    new Dictionary<string, string> { [NavigationErrorCodes.CursorDetail] = query.Cursor }));
            afterNodeId = parsed;
        }

        var startIndex = 0;
        if (afterNodeId is { } cursorNodeId)
        {
            startIndex = Array.FindIndex(children, node => node.NodeId == cursorNodeId);
            if (startIndex < 0)
                return Result<ChildrenPage>.Failure(new DomainError(
                    NavigationErrorCodes.InvalidCursor,
                    "Der Cursor gehört nicht zu diesem Ergebnis.",
                    new Dictionary<string, string> { [NavigationErrorCodes.CursorDetail] = query.Cursor! }));

            startIndex++;
        }

        var page = children.Skip(startIndex).Take(effectiveLimit + 1).ToArray();
        var hasNext = page.Length > effectiveLimit;
        var pageItems = page.Take(effectiveLimit).ToArray();

        var snapshotData = new NavigationSnapshotData(nodes, roles, resolutions, contents, dependencies);
        var summaries = pageItems
            .Select(node => BuildChildSummary(node, query.RoleId, snapshotData))
            .ToArray();

        var nextCursor = hasNext && pageItems.Length > 0
            ? BuildCursor(pageItems[^1])
            : null;

        return Result<ChildrenPage>.Success(new ChildrenPage(query.ParentNodeId, Array.AsReadOnly(summaries), nextCursor));
    }

    /// <summary>Liefert alle aktiven Rollen des aufgelösten Snapshots.</summary>
    public async Task<Result<IReadOnlyList<Role>>> ListRolesAsync(
        ReadContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var contextResult = await ResolveContextAsync(context, cancellationToken).ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Result<IReadOnlyList<Role>>.Failure(contextResult.Error!);

        var snapshotId = contextResult.Value!.SnapshotId;
        var roles = await _repos.Roles.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<Role>>.Success(roles.Where(r => !r.IsDeleted).ToArray());
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private sealed record NavigationSnapshotData(
        IReadOnlyList<Node> Nodes,
        IReadOnlyList<Role> Roles,
        IReadOnlyList<RoleResolution> Resolutions,
        IReadOnlyList<Domain.Content.NodeContent> Contents,
        IReadOnlyList<ContentDependency> Dependencies);

    private async Task<Result<ResolvedReadContext>> ResolveContextAsync(
        ReadContext context,
        CancellationToken cancellationToken)
    {
        KnowledgeTransaction? transaction = null;
        Snapshot? snapshot = null;
        var currentSnapshot = await _repos.Snapshots.GetCurrentAsync(cancellationToken).ConfigureAwait(false);

        if (context.TransactionId is { } txId)
            transaction = await _repos.Transactions.FindAsync(txId, cancellationToken).ConfigureAwait(false);

        if (context.SnapshotId is { } snapId)
            snapshot = await _repos.Snapshots.FindAsync(snapId, cancellationToken).ConfigureAwait(false);

        return ReadContextResolver.Resolve(context, new ReadContextCandidates(currentSnapshot.SnapshotId, transaction, snapshot));
    }

    private async Task<Result<NodeWithContent>> BuildNodeWithContentAsync(
        Node node,
        RoleId roleId,
        SnapshotId snapshotId,
        CancellationToken cancellationToken)
    {
        var roles = await _repos.Roles.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var resolutions = await _repos.Roles.ListResolutionsBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var contents = await _repos.Contents.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var dependencies = await _repos.Dependencies.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        var (resolvedContent, resolvedRoleId, availability, fallback) =
            ResolveContent(node.NodeId, roleId, roles, resolutions, contents);
        var freshness = resolvedContent is not null
            ? EvaluateFreshness(resolvedContent, contents, dependencies)
            : Freshness.Unknown;

        return Result<NodeWithContent>.Success(new NodeWithContent(
            node, roleId, resolvedRoleId, availability, fallback, resolvedContent, freshness));
    }

    private static ChildNodeSummary BuildChildSummary(
        Node node,
        RoleId roleId,
        NavigationSnapshotData data)
    {
        var childCount = data.Nodes.Count(n => !n.IsDeleted && n.ParentNodeId == node.NodeId);
        var (resolvedContent, resolvedRoleId, availability, _) =
            ResolveContent(node.NodeId, roleId, data.Roles, data.Resolutions, data.Contents);
        var contentSizeBytes = resolvedContent is not null
            ? System.Text.Encoding.UTF8.GetByteCount(resolvedContent.ContentMd)
            : 0;
        var freshness = resolvedContent is not null
            ? EvaluateFreshness(resolvedContent, data.Contents, data.Dependencies)
            : Freshness.Unknown;

        return new ChildNodeSummary(
            node.NodeId,
            node.Title,
            node.Description,
            node.SortOrder,
            childCount,
            contentSizeBytes,
            availability,
            resolvedRoleId,
            freshness);
    }


    private static (Domain.Content.NodeContent? content, RoleId? resolvedRoleId, Availability availability, bool fallback)
        ResolveContent(
            NodeId nodeId,
            RoleId requestedRoleId,
            IReadOnlyList<Role> roles,
            IReadOnlyList<RoleResolution> resolutions,
            IReadOnlyList<Domain.Content.NodeContent> contents)
    {
        // SnapshotId wird aus dem ersten aktiven Content ermittelt; alle Contents
        // desselben Snapshots haben dieselbe SnapshotId.
        var nodeContents = contents.Where(c => !c.IsDeleted && c.NodeId == nodeId).ToArray();
        var snapshotId = nodeContents.Length > 0
            ? nodeContents[0].SnapshotId
            : (roles.Count > 0 ? roles[0].SnapshotId : default);

        var resolutionResult = RoleResolver.Resolve(new RoleResolutionRequest(
            snapshotId,
            nodeId,
            requestedRoleId,
            roles,
            resolutions,
            nodeContents));

        if (!resolutionResult.IsSuccess)
            return (null, null, Availability.None, false);

        var resolved = resolutionResult.Value!;
        if (resolved.Availability == Availability.None)
            return (null, null, Availability.None, false);

        var content = nodeContents.FirstOrDefault(c => c.RoleId == resolved.ResolvedRole);
        return (content, resolved.ResolvedRole, resolved.Availability, resolved.FallbackUsed);
    }

    private static Freshness EvaluateFreshness(
        Domain.Content.NodeContent content,
        IReadOnlyList<Domain.Content.NodeContent> allContents,
        IReadOnlyList<ContentDependency> dependencies)
    {
        var result = FreshnessEvaluator.Evaluate(content, allContents, dependencies);
        return result;
    }

    private static NodeId? ParseCursor(string cursor)
    {
        if (Guid.TryParse(cursor, out var guid))
            return new NodeId(guid);
        return null;
    }

    private static string BuildCursor(Node node) => node.NodeId.Value.ToString("D");
}
