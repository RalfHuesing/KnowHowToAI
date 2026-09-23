using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Workflow;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Tree;

/// <summary>Führt Tree-Moves gegen den Working Snapshot aus und lädt Ablehnungen serverseitig neu.</summary>
public sealed class TreeMoveCoordinator(
    NodeMutationApplicationService nodeMutationService,
    WebWriteCoordinator webWriteCoordinator,
    WorkspaceState workspaceState,
    IKnowledgeTreeWorkspace treeWorkspace,
    TreeMoveRecovery recovery)
{
    private readonly NodeMutationApplicationService _nodeMutationService = nodeMutationService ?? throw new ArgumentNullException(nameof(nodeMutationService));
    private readonly WebWriteCoordinator _webWriteCoordinator = webWriteCoordinator ?? throw new ArgumentNullException(nameof(webWriteCoordinator));
    private readonly WorkspaceState _workspaceState = workspaceState ?? throw new ArgumentNullException(nameof(workspaceState));
    private readonly IKnowledgeTreeWorkspace _treeWorkspace = treeWorkspace ?? throw new ArgumentNullException(nameof(treeWorkspace));
    private readonly TreeMoveRecovery _recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));

    public async Task<TreeMoveOperationResult> MoveAsync(
        TreeMoveRequest request,
        string? transactionId,
        string? snapshotId,
        string? releaseId,
        CancellationToken cancellationToken = default)
    {
        var target = FindVisibleNode(_treeWorkspace.VisualRootNode, request.TargetNodeId);
        var source = FindVisibleNode(_treeWorkspace.VisualRootNode, request.SourceNodeId);
        if (source is null || target is null || source.NodeId == target.NodeId)
            return TreeMoveOperationResult.Rejected("Quelle und Ziel müssen verschiedene sichtbare Knoten sein.");

        var targetParentNodeId = target.ParentNodeId;
        var sourcePrecedesTarget = source.ParentNodeId == targetParentNodeId && IsBefore(source, target);
        var (parentNodeId, sortOrder) = request.Position switch
        {
            TreeMovePosition.Parent => (new NodeId?(new NodeId(request.TargetNodeId)), int.MaxValue),
            TreeMovePosition.Before => (ToNodeId(targetParentNodeId), CalculateInsertionIndex(target.Summary.SortOrder, sourcePrecedesTarget, after: false)),
            TreeMovePosition.After => (ToNodeId(targetParentNodeId), CalculateInsertionIndex(target.Summary.SortOrder, sourcePrecedesTarget, after: true)),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };
        var result = await _webWriteCoordinator.WriteAsync(
            _workspaceState.LoadedSnapshotId,
            (transactionId, changeVersion, token) => _nodeMutationService.MoveAsync(
                transactionId,
                new MoveNodeRequest(new NodeId(request.SourceNodeId), parentNodeId, sortOrder, changeVersion),
                token),
            mutation => mutation.ChangeVersion,
            cancellationToken).ConfigureAwait(false);
        if (result.Mutation.IsSuccess)
        {
            return TreeMoveOperationResult.Succeeded(result.Mutation.Value!);
        }

        await _recovery.ReloadAfterRejectionAsync(transactionId, snapshotId, releaseId, cancellationToken).ConfigureAwait(false);
        return TreeMoveOperationResult.Rejected($"[{result.Mutation.Error!.Code}] {result.Mutation.Error.Message} Der Wissensbaum wurde vom Server neu geladen.");
    }

    private static NodeId? ToNodeId(Guid? nodeId) => nodeId.HasValue ? new NodeId(nodeId.Value) : null;

    private static int CalculateInsertionIndex(
        int targetSortOrder,
        bool sourcePrecedesTarget,
        bool after)
    {
        var targetIndexAfterSourceRemoval = targetSortOrder - (sourcePrecedesTarget ? 1 : 0);
        return after ? targetIndexAfterSourceRemoval + 1 : targetIndexAfterSourceRemoval;
    }

    private static bool IsBefore(KnowledgeTreeNodeViewModel source, KnowledgeTreeNodeViewModel target)
    {
        var orderComparison = source.Summary.SortOrder.CompareTo(target.Summary.SortOrder);
        return orderComparison < 0
            || (orderComparison == 0 && source.NodeId.CompareTo(target.NodeId) < 0);
    }

    private static KnowledgeTreeNodeViewModel? FindVisibleNode(
        KnowledgeTreeNodeViewModel? root,
        Guid nodeId)
    {
        if (root is null)
            return null;
        if (root.NodeId == nodeId)
            return root;
        if (!root.IsExpanded)
            return null;

        foreach (var child in root.Children)
        {
            var match = FindVisibleNode(child, nodeId);
            if (match is not null)
                return match;
        }

        return null;
    }
}

/// <summary>Transportiert die bestätigte Mutation oder die bereits neu geladene Ablehnung an die Seite.</summary>
public sealed record TreeMoveOperationResult(NodeMutationResult? Mutation, string? ErrorMessage)
{
    public bool IsSuccess => Mutation is not null;

    public static TreeMoveOperationResult Succeeded(NodeMutationResult mutation) => new(mutation, null);

    public static TreeMoveOperationResult Rejected(string errorMessage) => new(null, errorMessage);
}
