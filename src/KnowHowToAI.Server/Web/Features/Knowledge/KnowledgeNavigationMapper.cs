using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Statische Mapper-Methoden zur Überführung von Navigationsergebnissen in UI-ViewModels.
/// Stellt sicher, dass Domain-Typen nicht im Rendering verwendet werden und Fehler,
/// Warnungen und Cursor vollständig erhalten bleiben.
/// </summary>
public static class KnowledgeNavigationMapper
{
    public static NodeDetailsViewModel? ToNodeDetailsViewModel(NodeWithContent? nodeWithContent, long? changeVersion = null)
    {
        if (nodeWithContent?.Node is null)
            return null;

        var node = nodeWithContent.Node;
        return new NodeDetailsViewModel(
            node.NodeId.Value,
            node.ParentNodeId?.Value,
            node.Title,
            node.Description,
            node.SortOrder,
            nodeWithContent.RequestedRoleId.Value,
            nodeWithContent.ResolvedRoleId?.Value,
            nodeWithContent.FallbackUsed,
            nodeWithContent.Availability.ToString(),
            nodeWithContent.Freshness.ToString(),
            nodeWithContent.Content?.ContentRevisionId.Value,
            nodeWithContent.Content?.ContentMode.ToString(),
            nodeWithContent.Content?.ContentMd,
            changeVersion);
    }

    public static ChildNodeViewModel ToChildNodeViewModel(ChildNodeSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new ChildNodeViewModel(
            summary.NodeId.Value,
            summary.Title,
            summary.Description,
            summary.SortOrder,
            summary.ChildCount,
            summary.ContentSizeBytes,
            summary.Availability.ToString(),
            summary.ResolvedRoleId?.Value,
            summary.Freshness.ToString());
    }

    public static ChildrenPageViewModel ToChildrenPageViewModel(ChildrenPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        var items = page.Items.Select(ToChildNodeViewModel).ToArray();
        return new ChildrenPageViewModel(
            page.ParentNodeId?.Value,
            items,
            page.NextCursor);
    }

    public static Result<NodeDetailsViewModel> ToNodeDetailsResult(Result<NodeWithContent> result, long? changeVersion = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSuccess)
            return Result<NodeDetailsViewModel>.Failure(result.Error!, result.Warnings);

        return Result<NodeDetailsViewModel>.Success(
            ToNodeDetailsViewModel(result.Value, changeVersion),
            result.Warnings);
    }

    public static Result<ChildrenPageViewModel> ToChildrenPageResult(Result<ChildrenPage> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSuccess)
            return Result<ChildrenPageViewModel>.Failure(result.Error!, result.Warnings);

        return Result<ChildrenPageViewModel>.Success(
            result.Value is null ? null : ToChildrenPageViewModel(result.Value),
            result.Warnings);
    }
}
