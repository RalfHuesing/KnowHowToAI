using System.Text;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Validation;

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
        var data = await LoadExportDataAsync(resolvedContext, roleId, cancellationToken).ConfigureAwait(false);
        var rootNode = data.Nodes.FirstOrDefault(node => node.NodeId == rootNodeId);
        if (rootNode is null)
            return Result<string>.Failure(new DomainError(
                NavigationErrorCodes.NodeNotFound,
                "Die angefragte Node existiert nicht.",
                new Dictionary<string, string> { [NavigationErrorCodes.NodeIdDetail] = rootNodeId.ToString() }));

        var resolvedNodes = new Dictionary<NodeId, ExportNodeContext>();
        BuildExportContexts(rootNode, data, resolvedNodes);

        if (!resolvedNodes.TryGetValue(rootNodeId, out var rootContext) || !rootContext.IsExportable)
            return Result<string>.Success(string.Empty);

        var rendered = RenderExport(rootNode, data.ChildrenByParent, resolvedNodes);
        return Result<string>.Success(rendered.Markdown, BuildWarnings(rendered));
    }

    private sealed record ExportNodeContext(
        NodeContent? Content,
        Freshness Freshness,
        bool HasExportableDescendant)
    {
        public bool IsExportable => Content is not null || HasExportableDescendant;
    }

    private sealed record ExportSnapshotData(
        SnapshotId SnapshotId,
        RoleId RoleId,
        IReadOnlyList<Node> Nodes,
        IReadOnlyList<Role> Roles,
        IReadOnlyList<RoleResolution> Resolutions,
        IReadOnlyList<NodeContent> Contents,
        IReadOnlyList<ContentDependency> Dependencies,
        ILookup<NodeId, Node> ChildrenByParent);

    private sealed record RenderedExport(string Markdown, int MaximumDepth, bool HasStaleContent);

    private sealed class ExportRenderState
    {
        public StringBuilder Markdown { get; } = new();
        public int MaximumDepth { get; set; } = 1;
        public bool HasStaleContent { get; set; }
    }

    private async Task<ExportSnapshotData> LoadExportDataAsync(
        ResolvedReadContext context,
        RoleId roleId,
        CancellationToken cancellationToken)
    {
        var snapshotId = context.SnapshotId;
        var nodes = ActiveReadFilter.Apply(
            await _repos.Hierarchy.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false),
            context);
        var roles = ActiveReadFilter.Apply(
            await _repos.Roles.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false),
            context);
        var resolutions = await _repos.Roles.ListResolutionsBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var contents = ActiveReadFilter.Apply(
            await _repos.Contents.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false),
            context);
        var dependencies = await _repos.Dependencies.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var childrenByParent = nodes.Where(node => node.ParentNodeId.HasValue).ToLookup(node => node.ParentNodeId!.Value);

        return new ExportSnapshotData(
            snapshotId,
            roleId,
            nodes,
            roles,
            resolutions,
            contents,
            dependencies,
            childrenByParent);
    }

    private static bool BuildExportContexts(
        Node node,
        ExportSnapshotData data,
        Dictionary<NodeId, ExportNodeContext> contexts)
    {
        var resolution = NodeContentResolver.ResolveOrUnavailable(new NodeContentResolutionRequest(
            node.NodeId,
            data.RoleId,
            data.SnapshotId,
            data.Roles,
            data.Resolutions,
            data.Contents));
        var freshness = resolution.Content is not null
            ? FreshnessEvaluator.Evaluate(resolution.Content, data.Contents, data.Dependencies)
            : Freshness.Unknown;

        var hasExportableChild = false;
        foreach (var child in data.ChildrenByParent[node.NodeId])
        {
            var childExportable = BuildExportContexts(child, data, contexts);
            if (childExportable)
                hasExportableChild = true;
        }

        var ctx = new ExportNodeContext(resolution.Content, freshness, hasExportableChild);
        contexts[node.NodeId] = ctx;
        return ctx.IsExportable;
    }

    private static RenderedExport RenderExport(
        Node rootNode,
        ILookup<NodeId, Node> childrenByParent,
        IReadOnlyDictionary<NodeId, ExportNodeContext> contexts)
    {
        var state = new ExportRenderState();
        RenderNode(rootNode, 1, childrenByParent, contexts, state);
        state.Markdown.Append('\n');
        return new RenderedExport(state.Markdown.ToString(), state.MaximumDepth, state.HasStaleContent);
    }

    private static void RenderNode(
        Node node,
        int depth,
        ILookup<NodeId, Node> childrenByParent,
        IReadOnlyDictionary<NodeId, ExportNodeContext> contexts,
        ExportRenderState state)
    {
        state.MaximumDepth = Math.Max(state.MaximumDepth, depth);
        var nodeContext = contexts[node.NodeId];
        state.HasStaleContent |= nodeContext.Freshness == Freshness.Stale;

        if (state.Markdown.Length > 0)
            state.Markdown.Append("\n\n");

        state.Markdown.Append(new string('#', Math.Min(depth, MaximumHeadingLevel)));
        state.Markdown.Append(' ');
        state.Markdown.Append(node.Title);
        AppendContent(state.Markdown, nodeContext.Content);

        var children = childrenByParent[node.NodeId]
            .Where(child => contexts.TryGetValue(child.NodeId, out var childContext) && childContext.IsExportable)
            .OrderBy(child => child.SortOrder)
            .ThenBy(child => child.NodeId.Value);
        foreach (var child in children)
            RenderNode(child, depth + 1, childrenByParent, contexts, state);
    }

    private static void AppendContent(StringBuilder markdown, NodeContent? content)
    {
        if (string.IsNullOrWhiteSpace(content?.ContentMd))
            return;

        markdown.Append("\n\n");
        markdown.Append(NormalizeContent(content.ContentMd));
    }

    private static IReadOnlyList<DomainWarning> BuildWarnings(RenderedExport rendered)
    {
        var warnings = new List<DomainWarning>();
        if (rendered.MaximumDepth > MaximumHeadingLevel)
        {
            warnings.Add(new DomainWarning(
                QualityWarningCodes.HierarchyTooDeep,
                $"Der exportierte Teilbaum überschreitet mit relativer Tiefe {rendered.MaximumDepth} das Maximum von {MaximumHeadingLevel} Überschriftsebenen.",
                new Dictionary<string, string>
                {
                    [QualityWarningCodes.ActualDepthDetail] = rendered.MaximumDepth.ToString(),
                    [QualityWarningCodes.ThresholdDepthDetail] = MaximumHeadingLevel.ToString()
                }));
        }

        if (rendered.HasStaleContent)
        {
            warnings.Add(new DomainWarning(
                QualityWarningCodes.StaleDerivedContent,
                "Der exportierte Teilbaum enthält veralteten abgeleiteten Inhalt."));
        }

        return Array.AsReadOnly(warnings.ToArray());
    }

    private Task<Result<ResolvedReadContext>> ResolveContextAsync(
        ReadContext context,
        CancellationToken cancellationToken) =>
        ReadContextReader.ResolveAsync(context, _repos.Snapshots, _repos.Transactions, cancellationToken);

    private static string NormalizeContent(string contentMd)
    {
        return contentMd
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Trim();
    }
}
