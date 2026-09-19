using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.State;

namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>Führt Tree-Moves gegen den Working Snapshot aus und lädt Ablehnungen serverseitig neu.</summary>
public sealed class TreeMoveCoordinator(
    NodeMutationApplicationService nodeMutationService,
    WorkspaceState workspaceState,
    WebReadContextResolver readContextResolver,
    PageRegionState pageRegions,
    IKnowledgeTreeWorkspace treeWorkspace)
{
    private readonly NodeMutationApplicationService _nodeMutationService = nodeMutationService ?? throw new ArgumentNullException(nameof(nodeMutationService));
    private readonly WorkspaceState _workspaceState = workspaceState ?? throw new ArgumentNullException(nameof(workspaceState));
    private readonly WebReadContextResolver _readContextResolver = readContextResolver ?? throw new ArgumentNullException(nameof(readContextResolver));
    private readonly PageRegionState _pageRegions = pageRegions ?? throw new ArgumentNullException(nameof(pageRegions));
    private readonly IKnowledgeTreeWorkspace _treeWorkspace = treeWorkspace ?? throw new ArgumentNullException(nameof(treeWorkspace));

    public async Task<TreeMoveOperationResult> MoveAsync(
        TreeMoveRequest request,
        string? transactionId,
        string? snapshotId,
        string? releaseId,
        CancellationToken cancellationToken = default)
    {
        if (_workspaceState.ActiveTransactionId is not { } activeTransactionId)
            return TreeMoveOperationResult.Rejected("Strukturänderungen erfordern eine offene Transaction.");

        var (parentNodeId, sortOrder) = request.Position switch
        {
            TreeMovePosition.Parent => (new NodeId?(new NodeId(request.TargetNodeId)), int.MaxValue),
            TreeMovePosition.Before => (ToNodeId(request.TargetParentNodeId), request.TargetSortOrder),
            TreeMovePosition.After => (ToNodeId(request.TargetParentNodeId), request.TargetSortOrder + 1),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };
        var result = await _nodeMutationService.MoveAsync(
            activeTransactionId,
            new MoveNodeRequest(new NodeId(request.SourceNodeId), parentNodeId, sortOrder, _workspaceState.CurrentChangeVersion),
            cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
            return TreeMoveOperationResult.Succeeded(result.Value!);

        await ReloadAfterRejectionAsync(transactionId, snapshotId, releaseId, cancellationToken).ConfigureAwait(false);
        return TreeMoveOperationResult.Rejected($"[{result.Code}] {result.Error!.Message} Der Wissensbaum wurde vom Server neu geladen.");
    }

    private async Task ReloadAfterRejectionAsync(
        string? transactionId,
        string? snapshotId,
        string? releaseId,
        CancellationToken cancellationToken)
    {
        if (_workspaceState.CurrentRoleId is not { } roleId)
            return;

        var contextResolution = await _readContextResolver.ResolveAsync(transactionId, snapshotId, releaseId, cancellationToken).ConfigureAwait(false);
        if (!contextResolution.IsSuccess)
            return;

        var resolved = contextResolution.Value!;
        var updatedContext = _workspaceState.CurrentContext with { ChangeVersion = resolved.ChangeVersion };
        _workspaceState.SetContext(updatedContext, resolved.ReadContext);
        _workspaceState.SetChangeVersion(resolved.ChangeVersion);
        _pageRegions.SetKnowledgeContext(updatedContext);
        await _treeWorkspace.InitializeAsync(resolved.ReadContext, roleId, cancellationToken).ConfigureAwait(false);
    }

    private static NodeId? ToNodeId(Guid? nodeId) => nodeId.HasValue ? new NodeId(nodeId.Value) : null;
}

/// <summary>Transportiert die bestätigte Mutation oder die bereits neu geladene Ablehnung an die Seite.</summary>
public sealed record TreeMoveOperationResult(NodeMutationResult? Mutation, string? ErrorMessage)
{
    public bool IsSuccess => Mutation is not null;

    public static TreeMoveOperationResult Succeeded(NodeMutationResult mutation) => new(mutation, null);

    public static TreeMoveOperationResult Rejected(string errorMessage) => new(null, errorMessage);
}
