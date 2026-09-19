using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Bündelt Rückruffunktionen für den Wissensbaum-Pfad-Lader.
/// </summary>
internal sealed record TreePathLoaderCallbacks(
    Func<Guid, CancellationToken, Task> ExpandNode,
    Func<Guid, CancellationToken, Task> PageNext,
    Func<Guid, bool> IsNodeKnown,
    Func<Guid, KnowledgeTreeNodeViewModel?> GetNode,
    Func<Guid, CancellationToken, Task>? PagePrevious = null,
    Action<IReadOnlyList<Guid>>? OnPathResolved = null);

/// <summary>
/// Ergebnis des gezielten Ladens eines Knotenpfads.
/// </summary>
internal sealed record PathLoadResult(
    bool IsSuccess,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    Guid? FailedNodeId = null)
{
    public static PathLoadResult Success() => new(true);

    public static PathLoadResult Failure(string errorCode, string errorMessage, Guid? failedNodeId = null) =>
        new(false, errorCode, errorMessage, failedNodeId);
}

/// <summary>
/// Lädt gezielt den Pfad zu einem noch nicht geladenen Knoten vom Root abwärts,
/// expandiert die Zwischenknoten und blättert deren opake 100er-Seiten nacheinander durch,
/// bis jedes Pfadsegment sichtbar ist oder ein strukturierter Fehler entsteht.
/// </summary>
internal sealed class KnowledgeTreePathLoader
{
    private readonly NavigationService _navigationService;
    private readonly TreePathLoaderCallbacks _callbacks;

    public KnowledgeTreePathLoader(
        NavigationService navigationService,
        TreePathLoaderCallbacks callbacks)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
    }

    public async Task<PathLoadResult> EnsurePathLoadedAsync(
        Guid targetNodeId,
        ReadContext readContext,
        string? roleId,
        CancellationToken cancellationToken)
    {
        if (_callbacks.IsNodeKnown(targetNodeId))
            return PathLoadResult.Success();

        var (path, ancestorError) = await CollectAncestorsAsync(targetNodeId, readContext, roleId, cancellationToken).ConfigureAwait(false);
        if (ancestorError is not null)
            return ancestorError;

        if (path.Count == 0)
            return PathLoadResult.Failure(NavigationErrorCodes.NodeNotFound, "Knotenpfad konnte nicht ermittelt werden.", targetNodeId);

        _callbacks.OnPathResolved?.Invoke(path);
        return await ExpandAndPageSegmentsAsync(path, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(List<Guid> Path, PathLoadResult? Error)> CollectAncestorsAsync(
        Guid targetNodeId,
        ReadContext readContext,
        string? roleId,
        CancellationToken cancellationToken)
    {
        var currentId = targetNodeId;
        var path = new List<Guid>();
        var visited = new HashSet<Guid>();

        while (!_callbacks.IsNodeKnown(currentId))
        {
            if (!visited.Add(currentId))
            {
                return (new List<Guid>(), PathLoadResult.Failure(
                    "CyclicHierarchy",
                    "Zyklische Pfadbeziehung in der Hierarchie erkannt.",
                    currentId));
            }

            path.Add(currentId);
            var nodeResult = await _navigationService.GetNodeAsync(
                new NodeId(currentId),
                readContext,
                new RoleId(roleId ?? string.Empty),
                cancellationToken).ConfigureAwait(false);

            if (!nodeResult.IsSuccess || nodeResult.Value?.Node is null)
            {
                var errorCode = nodeResult.Error?.Code ?? NavigationErrorCodes.NodeNotFound;
                var errorMessage = nodeResult.Error?.Message ?? "Die angefragte Node existiert nicht.";
                return (new List<Guid>(), PathLoadResult.Failure(errorCode, errorMessage, currentId));
            }

            var parentId = nodeResult.Value.Node.ParentNodeId?.Value;
            if (parentId is null)
                break;

            currentId = parentId.Value;
        }

        if (_callbacks.IsNodeKnown(currentId) && !path.Contains(currentId))
        {
            path.Add(currentId);
        }

        path.Reverse();
        return (path, null);
    }

    private async Task<PathLoadResult> ExpandAndPageSegmentsAsync(
        List<Guid> path,
        CancellationToken cancellationToken)
    {
        for (var i = 1; i < path.Count; i++)
        {
            var result = await EnsureSegmentLoadedAsync(path[i - 1], path[i], cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
                return result;
        }

        return PathLoadResult.Success();
    }

    private async Task<PathLoadResult> EnsureSegmentLoadedAsync(
        Guid parentId,
        Guid segmentId,
        CancellationToken cancellationToken)
    {
        var parentNode = _callbacks.GetNode(parentId);
        if (parentNode is null)
        {
            return PathLoadResult.Failure(
                NavigationErrorCodes.NodeNotFound,
                "Der übergeordnete Knoten konnte im Baum nicht gefunden werden.",
                parentId);
        }

        var expandResult = await EnsureParentExpandedAsync(parentNode, cancellationToken).ConfigureAwait(false);
        if (!expandResult.IsSuccess)
            return expandResult;

        var pageResult = await PageToSegmentAsync(parentNode, segmentId, cancellationToken).ConfigureAwait(false);
        if (!pageResult.IsSuccess)
            return pageResult;

        if (!_callbacks.IsNodeKnown(segmentId))
        {
            var errorMsg = "Das Pfadsegment konnte unter dem übergeordneten Knoten nicht gefunden werden.";
            parentNode.Error = errorMsg;
            return PathLoadResult.Failure(NavigationErrorCodes.NodeNotFound, errorMsg, parentId);
        }

        return PathLoadResult.Success();
    }

    private async Task<PathLoadResult> EnsureParentExpandedAsync(
        KnowledgeTreeNodeViewModel parentNode,
        CancellationToken cancellationToken)
    {
        if (parentNode.IsExpanded || !parentNode.HasChildren)
            return PathLoadResult.Success();

        await _callbacks.ExpandNode(parentNode.NodeId, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(parentNode.Error)
            ? PathLoadResult.Success()
            : PathLoadResult.Failure(ExtractErrorCode(parentNode.Error), parentNode.Error, parentNode.NodeId);
    }

    private async Task<PathLoadResult> PageToSegmentAsync(
        KnowledgeTreeNodeViewModel parentNode,
        Guid segmentId,
        CancellationToken cancellationToken)
    {
        while (!_callbacks.IsNodeKnown(segmentId) && parentNode.HasNextPage)
        {
            await _callbacks.PageNext(parentNode.NodeId, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(parentNode.Error))
                return PathLoadResult.Failure(ExtractErrorCode(parentNode.Error), parentNode.Error, parentNode.NodeId);
        }

        if (!_callbacks.IsNodeKnown(segmentId) && _callbacks.PagePrevious is not null)
        {
            while (!_callbacks.IsNodeKnown(segmentId) && parentNode.HasPreviousPage)
            {
                await _callbacks.PagePrevious(parentNode.NodeId, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(parentNode.Error))
                    return PathLoadResult.Failure(ExtractErrorCode(parentNode.Error), parentNode.Error, parentNode.NodeId);
            }
        }

        return PathLoadResult.Success();
    }

    private static string ExtractErrorCode(string message)
    {
        if (message.Contains(NavigationErrorCodes.InvalidCursor, StringComparison.OrdinalIgnoreCase))
            return NavigationErrorCodes.InvalidCursor;
        if (message.Contains(NavigationErrorCodes.CursorExpired, StringComparison.OrdinalIgnoreCase))
            return NavigationErrorCodes.CursorExpired;
        return "LoadError";
    }
}
