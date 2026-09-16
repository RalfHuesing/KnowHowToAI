using System.Text;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Retrieval.Export;

/// <summary>
/// Transportneutraler Export-Use-Case (export_tree): wandelt einen Teilbaum in Markdown um.
/// Überschriften entstehen ausschließlich aus der Node-Hierarchie; ContentMd darf keine enthalten.
/// Ausgewählter Root wird H1, Nachfahren erhalten relative Heading-Level.
/// Rollenauflösung und Freshness pro Node; Node ohne Content wird nur bei exportiertem Nachfahren aufgenommen.
/// </summary>
public sealed class MarkdownExportService
{
    private const int MaximumHeadingLevel = 6;
    private readonly HistoryRepositories _repos;

    public MarkdownExportService(HistoryRepositories repositories)
    {
        _repos = repositories ?? throw new ArgumentNullException(nameof(repositories));
    }

    /// <summary>
    /// Exportiert den Teilbaum ab <paramref name="rootNodeId"/> als Markdown.
    /// Der ausgewählte Root-Node wird zu H1, Nachfahren erhalten relative Heading-Level.
    /// </summary>
    public async Task<Result<string>> ExportTreeAsync(
        NodeId rootNodeId,
        ReadContext context,
        RoleId roleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var contextResult = await ResolveContextAsync(context, cancellationToken).ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Result<string>.Failure(contextResult.Error!);

        var resolvedContext = contextResult.Value!;
        var snapshotId = resolvedContext.SnapshotId;

        var allNodes = await _repos.Hierarchy.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var allRoles = await _repos.Roles.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var allResolutions = await _repos.Roles.ListResolutionsBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var allContents = await _repos.Contents.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var allDependencies = await _repos.Dependencies.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        var activeNodes = ActiveReadFilter.Apply(allNodes, resolvedContext);
        var activeRoles = ActiveReadFilter.Apply(allRoles, resolvedContext);
        var activeContents = ActiveReadFilter.Apply(allContents, resolvedContext);

        var rootNode = activeNodes.FirstOrDefault(n => n.NodeId == rootNodeId);
        if (rootNode is null)
        {
            return Result<string>.Failure(new DomainError(
                NavigationErrorCodes.NodeNotFound,
                "Die angefragte Node existiert nicht.",
                new Dictionary<string, string> { [NavigationErrorCodes.NodeIdDetail] = rootNodeId.ToString() }));
        }

        var childrenByParent = activeNodes
            .Where(n => n.ParentNodeId.HasValue)
            .ToLookup(n => n.ParentNodeId!.Value);

        var resolvedNodes = new Dictionary<NodeId, ExportNodeContext>();
        BuildExportContexts(rootNode, childrenByParent, snapshotId, roleId, activeRoles, allResolutions, activeContents, allDependencies, resolvedNodes);

        if (!resolvedNodes.TryGetValue(rootNodeId, out var rootContext) || !rootContext.IsExportable)
        {
            return Result<string>.Success(string.Empty);
        }

        var warnings = new List<DomainWarning>();
        var sb = new StringBuilder();
        var maxDepthObserved = 1;
        var hasStaleContent = false;

        void RenderNode(Node node, int depth)
        {
            if (depth > maxDepthObserved)
                maxDepthObserved = depth;

            var nodeCtx = resolvedNodes[node.NodeId];
            if (nodeCtx.Freshness == Freshness.Stale)
                hasStaleContent = true;

            var effectiveHeadingLevel = Math.Min(depth, MaximumHeadingLevel);
            var headingPrefix = new string('#', effectiveHeadingLevel);

            if (sb.Length > 0)
                sb.Append("\n\n");

            sb.Append(headingPrefix);
            sb.Append(' ');
            sb.Append(node.Title);

            if (!string.IsNullOrWhiteSpace(nodeCtx.Content?.ContentMd))
            {
                var normalizedContent = NormalizeContent(nodeCtx.Content.ContentMd);
                sb.Append("\n\n");
                sb.Append(normalizedContent);
            }

            var exportableChildren = childrenByParent[node.NodeId]
                .Where(child => resolvedNodes.TryGetValue(child.NodeId, out var ctx) && ctx.IsExportable)
                .OrderBy(child => child.SortOrder)
                .ThenBy(child => child.NodeId.Value);

            foreach (var child in exportableChildren)
            {
                RenderNode(child, depth + 1);
            }
        }

        RenderNode(rootNode, 1);
        sb.Append('\n');

        if (maxDepthObserved > MaximumHeadingLevel)
        {
            warnings.Add(new DomainWarning(
                QualityWarningCodes.HierarchyTooDeep,
                $"Der exportierte Teilbaum überschreitet mit relativer Tiefe {maxDepthObserved} das Maximum von {MaximumHeadingLevel} Überschriftsebenen.",
                new Dictionary<string, string>
                {
                    [QualityWarningCodes.ActualDepthDetail] = maxDepthObserved.ToString(),
                    [QualityWarningCodes.ThresholdDepthDetail] = MaximumHeadingLevel.ToString()
                }));
        }

        if (hasStaleContent)
        {
            warnings.Add(new DomainWarning(
                QualityWarningCodes.StaleDerivedContent,
                "Der exportierte Teilbaum enthält veralteten abgeleiteten Inhalt."));
        }

        return Result<string>.Success(sb.ToString(), warnings);
    }

    private sealed record ExportNodeContext(
        NodeContent? Content,
        Freshness Freshness,
        bool HasExportableDescendant)
    {
        public bool IsExportable => Content is not null || HasExportableDescendant;
    }

    private static bool BuildExportContexts(
        Node node,
        ILookup<NodeId, Node> childrenByParent,
        SnapshotId snapshotId,
        RoleId roleId,
        IReadOnlyList<Role> roles,
        IReadOnlyList<RoleResolution> resolutions,
        IReadOnlyList<NodeContent> contents,
        IReadOnlyList<ContentDependency> dependencies,
        Dictionary<NodeId, ExportNodeContext> contexts)
    {
        var (content, _, _, _) = ResolveContent(node.NodeId, roleId, snapshotId, roles, resolutions, contents);
        var freshness = content is not null
            ? FreshnessEvaluator.Evaluate(content, contents, dependencies)
            : Freshness.Unknown;

        var hasExportableChild = false;
        foreach (var child in childrenByParent[node.NodeId])
        {
            var childExportable = BuildExportContexts(child, childrenByParent, snapshotId, roleId, roles, resolutions, contents, dependencies, contexts);
            if (childExportable)
                hasExportableChild = true;
        }

        var ctx = new ExportNodeContext(content, freshness, hasExportableChild);
        contexts[node.NodeId] = ctx;
        return ctx.IsExportable;
    }

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

    private static (NodeContent? content, RoleId? resolvedRoleId, Availability availability, bool fallback)
        ResolveContent(
            NodeId nodeId,
            RoleId requestedRoleId,
            SnapshotId snapshotId,
            IReadOnlyList<Role> roles,
            IReadOnlyList<RoleResolution> resolutions,
            IReadOnlyList<NodeContent> contents)
    {
        var nodeContents = contents.Where(c => c.NodeId == nodeId).ToArray();

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

    private static string NormalizeContent(string contentMd)
    {
        return contentMd
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Trim();
    }
}
