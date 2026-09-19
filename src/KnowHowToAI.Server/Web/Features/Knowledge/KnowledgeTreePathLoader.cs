using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Lädt gezielt den Pfad zu einem noch nicht geladenen Knoten vom Root abwärts,
/// expandiert die Zwischenknoten und blättert ggf. zur passenden 100er-Seite.
/// </summary>
internal sealed class KnowledgeTreePathLoader
{
    private readonly NavigationService _navigationService;
    private readonly Func<Guid, CancellationToken, Task> _expandNode;
    private readonly Func<Guid, CancellationToken, Task> _pageNext;
    private readonly Func<Guid, bool> _isNodeKnown;
    private readonly Func<Guid, KnowledgeTreeNodeViewModel?> _getNode;

    public KnowledgeTreePathLoader(
        NavigationService navigationService,
        Func<Guid, CancellationToken, Task> expandNode,
        Func<Guid, CancellationToken, Task> pageNext,
        Func<Guid, bool> isNodeKnown,
        Func<Guid, KnowledgeTreeNodeViewModel?> getNode)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _expandNode = expandNode ?? throw new ArgumentNullException(nameof(expandNode));
        _pageNext = pageNext ?? throw new ArgumentNullException(nameof(pageNext));
        _isNodeKnown = isNodeKnown ?? throw new ArgumentNullException(nameof(isNodeKnown));
        _getNode = getNode ?? throw new ArgumentNullException(nameof(getNode));
    }

    public async Task EnsurePathLoadedAsync(
        Guid targetNodeId,
        ReadContext readContext,
        string? roleId,
        CancellationToken cancellationToken)
    {
        var path = await CollectAncestorsAsync(targetNodeId, readContext, roleId, cancellationToken).ConfigureAwait(false);
        if (path.Count == 0)
            return;

        await ExpandAncestorsAsync(path, targetNodeId, cancellationToken).ConfigureAwait(false);
        await PageToTargetChildAsync(path, targetNodeId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<Guid>> CollectAncestorsAsync(
        Guid targetNodeId,
        ReadContext readContext,
        string? roleId,
        CancellationToken cancellationToken)
    {
        var currentId = targetNodeId;
        var path = new List<Guid>();

        while (!_isNodeKnown(currentId))
        {
            path.Add(currentId);
            var nodeResult = await _navigationService.GetNodeAsync(
                new NodeId(currentId),
                readContext,
                new RoleId(roleId ?? string.Empty),
                cancellationToken).ConfigureAwait(false);

            if (!nodeResult.IsSuccess || nodeResult.Value?.Node is null)
                return new List<Guid>();

            var parentId = nodeResult.Value.Node.ParentNodeId?.Value;
            if (parentId is null)
                break;

            currentId = parentId.Value;
        }

        if (_isNodeKnown(currentId) && !path.Contains(currentId))
        {
            path.Add(currentId);
        }

        path.Reverse();
        return path;
    }

    private async Task ExpandAncestorsAsync(
        List<Guid> path,
        Guid targetNodeId,
        CancellationToken cancellationToken)
    {
        foreach (var id in path)
        {
            if (id == targetNodeId)
                break;

            var node = _getNode(id);
            if (node is not null && !node.IsExpanded && node.HasChildren)
            {
                await _expandNode(node.NodeId, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task PageToTargetChildAsync(
        List<Guid> path,
        Guid targetNodeId,
        CancellationToken cancellationToken)
    {
        if (_isNodeKnown(targetNodeId) || path.Count < 2)
            return;

        var parentId = path[^2];
        var parentNode = _getNode(parentId);
        if (parentNode is null)
            return;

        while (!_isNodeKnown(targetNodeId) && parentNode.HasNextPage)
        {
            await _pageNext(parentId, cancellationToken).ConfigureAwait(false);
        }
    }
}
