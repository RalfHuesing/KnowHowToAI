using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// UI-ViewModel für einen Knoten im nativen Wissensbaum.
/// Hält Navigations-, Darstellungs- und Paging-Zustand für die Blazor-Komponente
/// und entkoppelt sie vollständig von Domain-Typen.
/// </summary>
public sealed record KnowledgeTreeNodeViewModel
{
    public ChildNodeViewModel Summary { get; init; } = null!;

    public Guid NodeId => Summary.NodeId;

    public Guid? ParentNodeId { get; init; }

    public string Title => Summary.Title;

    public int Depth { get; init; }

    public int ChildCount { get; internal set; }

    public bool HasChildren => ChildCount > 0;

    public bool IsExpanded { get; internal set; }

    public bool IsLoading { get; internal set; }

    public bool IsSelected { get; internal set; }

    public string? Error { get; internal set; }

    public IReadOnlyList<KnowledgeTreeNodeViewModel> Children { get; internal set; } = Array.Empty<KnowledgeTreeNodeViewModel>();

    public string? NextCursor { get; internal set; }

    public bool HasPreviousPage { get; internal set; }

    public bool HasNextPage => !string.IsNullOrEmpty(NextCursor);

    internal static KnowledgeTreeNodeViewModel FromRoot(NodeWithContent nodeWithContent, int childCount = 1)
    {
        ArgumentNullException.ThrowIfNull(nodeWithContent);

        var node = nodeWithContent.Node;
        var contentSizeBytes = nodeWithContent.Content is not null
            ? System.Text.Encoding.UTF8.GetByteCount(nodeWithContent.Content.ContentMd)
            : 0;

        var summary = new ChildNodeViewModel(
            node?.NodeId.Value ?? Guid.Empty,
            node?.Title ?? string.Empty,
            node?.Description,
            node?.SortOrder ?? 0,
            childCount,
            contentSizeBytes,
            nodeWithContent.Availability.ToString(),
            nodeWithContent.ResolvedAudienceId?.Value,
            nodeWithContent.Freshness.ToString(),
            nodeWithContent.Freshness == Freshness.Stale
                ? [QualityWarningCodes.StaleDerivedContent]
                : []);

        return new KnowledgeTreeNodeViewModel
        {
            Summary = summary,
            ParentNodeId = null,
            Depth = 0,
            ChildCount = childCount,
            IsExpanded = false,
            IsLoading = false,
            IsSelected = false
        };
    }

    internal static KnowledgeTreeNodeViewModel FromChildSummary(ChildNodeSummary summary, Guid parentNodeId, int depth)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var vm = KnowledgeNavigationMapper.ToChildNodeViewModel(summary);

        return new KnowledgeTreeNodeViewModel
        {
            Summary = vm,
            ParentNodeId = parentNodeId,
            Depth = depth,
            ChildCount = summary.ChildCount,
            IsExpanded = false,
            IsLoading = false,
            IsSelected = false
        };
    }
}
