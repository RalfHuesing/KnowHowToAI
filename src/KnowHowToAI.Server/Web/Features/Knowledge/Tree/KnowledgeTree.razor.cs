using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Workflow;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Tree;

/// <summary>
/// Nativer, mausbedienbarer Blazor-Wissensbaum mit seitenbegrenztem Paging
/// und serverseitig bestätigtem Drag-and-drop.
/// </summary>
public sealed partial class KnowledgeTree : IAsyncDisposable, IDisposable
{
    private const string ModuleAssetPath = "Web/Features/Knowledge/Tree/KnowledgeTree.razor.js";

    [Inject]
    public IKnowledgeTreeWorkspace TreeWorkspace { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<KnowledgeTree>? Logger { get; set; }

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    [Parameter]
    public EventCallback<Guid> OnNodeSelected { get; set; }

    [Parameter]
    public bool CanMove { get; set; }

    [Parameter]
    public EventCallback<NodeMutationResult> OnNodeMutationSucceeded { get; set; }

    [Parameter]
    public string? MoveTransactionId { get; set; }

    private ElementReference _treeElement;
    private Task<IJSObjectReference>? _moduleTask;
    private DotNetObjectReference<KnowledgeTree>? _dragAndDropReference;
    private string? _moveErrorMessage;
    private bool _isMutating;
    private bool _isDeleteDialogOpen;
    private string _deleteDialogTitle = string.Empty;
    private string _deleteDialogMessage = string.Empty;
    private bool _isDisposed;

    protected override void OnInitialized()
    {
        TreeWorkspace.Changed += HandleTreeStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (TreeWorkspace.VisualRootNode is not null)
        {
            try
            {
                var module = await EnsureModuleAsync();
                _dragAndDropReference ??= DotNetObjectReference.Create(this);
                await module.InvokeVoidAsync("initTreeDragAndDrop", _treeElement, _dragAndDropReference, CanMove);
            }
            catch (Exception ex)
            {
                Logger?.LogDebug(ex, "Wissensbaum-Interop konnte nicht initialisiert werden.");
            }
            await EnsureVisibleExpandedNodesLoadedAsync();
        }
    }

    private async Task EnsureVisibleExpandedNodesLoadedAsync()
    {
        var nodeToLoad = GetVisibleNodes()
            .FirstOrDefault(n => n.HasChildren && n.IsExpanded && !n.IsChildrenPageLoaded && !n.IsLoading);

        if (nodeToLoad is not null)
        {
            await TreeWorkspace.ExpandNodeAsync(nodeToLoad.NodeId);
        }
    }

    private void HandleTreeStateChanged()
    {
        InvokeAsync(StateHasChanged);
    }


    private async Task HandleSelectNodeAsync(Guid nodeId)
    {
        await TreeWorkspace.SelectNodeAsync(nodeId);
        await OnNodeSelected.InvokeAsync(nodeId);
    }

    [JSInvokable]
    public Task HandleTreeSelectionAsync(string nodeId) =>
        Guid.TryParse(nodeId, out var parsedNodeId)
            ? LogAndSelectAsync(parsedNodeId)
            : Task.CompletedTask;

    private Task LogAndSelectAsync(Guid nodeId) => InvokeAsync(() => HandleSelectNodeAsync(nodeId));

    [JSInvokable]
    public Task HandleTreeDropAsync(string sourceNodeId, string targetNodeId, string position) =>
        InvokeAsync(() => ProcessTreeDropAsync(sourceNodeId, targetNodeId, position));

    private async Task ProcessTreeDropAsync(string sourceNodeId, string targetNodeId, string position)
    {
        if (!Guid.TryParse(sourceNodeId, out var sourceId)
            || !Guid.TryParse(targetNodeId, out var targetId)
            || !Enum.TryParse<TreeMovePosition>(position, ignoreCase: false, out var movePosition))
            return;

        var target = GetVisibleNodes().FirstOrDefault(node => node.NodeId == targetId);
        if (target is null)
            return;

        await ProcessMoveAsync(sourceId, target, movePosition);
    }

    private async Task ProcessMoveAsync(Guid sourceId, KnowledgeTreeNodeViewModel target, TreeMovePosition movePosition)
    {
        _moveErrorMessage = null;
        var treeMoveCoordinator = ServiceProvider.GetService<TreeMoveCoordinator>();
        if (treeMoveCoordinator is null)
        {
            _moveErrorMessage = "Die Verschiebeaktion ist in diesem Kontext nicht verfügbar.";
            return;
        }

        var outcome = await treeMoveCoordinator.MoveAsync(new TreeMoveRequest(
            sourceId,
            target.NodeId,
            movePosition),
            MoveTransactionId);
        if (outcome.IsSuccess)
        {
            await OnNodeMutationSucceeded.InvokeAsync(outcome.Mutation!);
        }
        else
            _moveErrorMessage = outcome.ErrorMessage;

        StateHasChanged();
    }

    internal KnowledgeTreeNodeViewModel? SelectedNode => FindNode(TreeWorkspace.VisualRootNode, TreeWorkspace.SelectedNodeId);

    internal bool CanCreateChild => CanMove && TreeWorkspace.VisualRootNode is not null;

    internal bool CanCreateSibling => CanMove && SelectedNode is { ParentNodeId: not null };

    internal bool CanDelete => CanMove && SelectedNode is not null;

    internal string CreateChildTitle => SelectedNode is not null
        ? $"Unterknoten unter „{SelectedNode.Title}“ anlegen"
        : "Unterknoten unter Root anlegen";

    internal string CreateSiblingTitle => SelectedNode is null
        ? "Kein Knoten für gleiche Ebene ausgewählt"
        : SelectedNode.ParentNodeId.HasValue
            ? "Knoten auf gleicher Ebene anlegen"
            : "Der Root-Knoten kann keine Nachbarn auf gleicher Ebene haben";

    internal string DeleteTitle => SelectedNode is not null
        ? $"Knoten „{SelectedNode.Title}“ löschen"
        : "Kein Knoten zum Löschen ausgewählt";

    private static KnowledgeTreeNodeViewModel? FindNode(KnowledgeTreeNodeViewModel? root, Guid? nodeId)
    {
        if (root is null || !nodeId.HasValue)
            return null;

        if (root.NodeId == nodeId.Value)
            return root;

        foreach (var child in root.Children)
        {
            var found = FindNode(child, nodeId);
            if (found is not null)
                return found;
        }

        return null;
    }

    internal async Task CreateChildNodeAsync()
    {
        if (!CanCreateChild || _isMutating)
            return;

        var parentId = SelectedNode?.NodeId ?? TreeWorkspace.VisualRootNode?.NodeId;
        if (!parentId.HasValue)
            return;

        await CreateNodeUnderParentAsync(parentId.Value);
    }

    internal async Task CreateSiblingNodeAsync()
    {
        if (!CanCreateSibling || _isMutating)
            return;

        var parentId = SelectedNode?.ParentNodeId;
        if (!parentId.HasValue)
            return;

        await CreateNodeUnderParentAsync(parentId.Value);
    }

    private async Task CreateNodeUnderParentAsync(Guid parentId)
    {
        _isMutating = true;
        _moveErrorMessage = null;
        try
        {
            var nodeMutationService = ServiceProvider.GetService<NodeMutationApplicationService>();
            var writeCoordinator = ServiceProvider.GetService<WebWriteCoordinator>();
            var workspaceState = ServiceProvider.GetService<WorkspaceState>();
            if (nodeMutationService is null || writeCoordinator is null || workspaceState is null)
            {
                _moveErrorMessage = "Die Knotenerstellung ist in diesem Kontext nicht verfügbar.";
                return;
            }

            var result = await writeCoordinator.WriteAsync(
                workspaceState.LoadedSnapshotId,
                (transactionId, changeVersion, cancellationToken) => nodeMutationService.CreateAsync(
                    transactionId,
                    new CreateNodeRequest(new NodeId(parentId), "Neuer Eintrag", Description: null, SortOrder: 0),
                    changeVersion,
                    cancellationToken),
                mutation => mutation.ChangeVersion);

            if (!result.Mutation.IsSuccess)
            {
                _moveErrorMessage = $"[{result.Mutation.Error!.Code}] {result.Mutation.Error.Message}";
                return;
            }

            workspaceState.RequestedInitialView = "Metadata";
            await TreeWorkspace.ExpandNodeAsync(parentId);
            await OnNodeMutationSucceeded.InvokeAsync(result.Mutation.Value!);
        }
        catch (Exception ex)
        {
            _moveErrorMessage = $"[{ex.GetType().Name}] {ex.Message}";
        }
        finally
        {
            _isMutating = false;
        }
    }

    internal void RequestDeleteNode()
    {
        if (!CanDelete || _isMutating || SelectedNode is null)
            return;

        var node = SelectedNode;
        _deleteDialogTitle = $"Knoten „{node.Title}“ löschen?";
        if (node.ChildCount > 0)
        {
            _deleteDialogMessage = $"Achtung: Der Knoten „{node.Title}“ enthält {node.ChildCount} Unterknoten. Beim Löschen werden dieser Knoten und alle Unterknoten und Inhalte unwiderruflich gelöscht.";
        }
        else
        {
            _deleteDialogMessage = $"Möchten Sie den Knoten „{node.Title}“ wirklich löschen? Diese Aktion entfernt auch alle Zielgruppeninhalte dieses Knotens.";
        }

        _isDeleteDialogOpen = true;
    }

    internal void CancelDelete()
    {
        _isDeleteDialogOpen = false;
    }

    internal async Task ConfirmDeleteNodeAsync()
    {
        _isDeleteDialogOpen = false;
        if (!CanDelete || _isMutating || SelectedNode is null)
            return;

        var targetNode = SelectedNode;
        var targetParentId = targetNode.ParentNodeId;

        _isMutating = true;
        _moveErrorMessage = null;
        try
        {
            var mutationError = await ExecuteNodeDeletionAsync(targetNode.NodeId);
            if (mutationError is not null)
            {
                _moveErrorMessage = mutationError;
                return;
            }

            await NavigateAfterNodeDeletionAsync(targetNode.NodeId, targetParentId);
        }
        catch (Exception ex)
        {
            _moveErrorMessage = $"[{ex.GetType().Name}] {ex.Message}";
        }
        finally
        {
            _isMutating = false;
        }
    }

    private async Task<string?> ExecuteNodeDeletionAsync(Guid nodeId)
    {
        var nodeDeletionService = ServiceProvider.GetService<NodeDeletionApplicationService>();
        var writeCoordinator = ServiceProvider.GetService<WebWriteCoordinator>();
        var workspaceState = ServiceProvider.GetService<WorkspaceState>();
        if (nodeDeletionService is null || writeCoordinator is null || workspaceState is null)
        {
            return "Die Knotenlöschung ist in diesem Kontext nicht verfügbar.";
        }

        var result = await writeCoordinator.WriteAsync(
            workspaceState.LoadedSnapshotId,
            (transactionId, changeVersion, cancellationToken) => nodeDeletionService.DeleteAsync(
                transactionId,
                new NodeId(nodeId),
                deleteSubtree: true,
                expectedChangeVersion: changeVersion,
                cancellationToken: cancellationToken),
            mutation => mutation.ChangeVersion);

        return result.Mutation.IsSuccess
            ? null
            : $"[{result.Mutation.Error!.Code}] {result.Mutation.Error.Message}";
    }

    private async Task NavigateAfterNodeDeletionAsync(Guid deletedNodeId, Guid? parentId)
    {
        var workspaceState = ServiceProvider.GetRequiredService<WorkspaceState>();
        var fallbackId = parentId ?? (TreeWorkspace.VisualRootNode?.NodeId == deletedNodeId ? null : TreeWorkspace.VisualRootNode?.NodeId);

        if (workspaceState.CurrentAudienceId is { } audienceId)
        {
            await TreeWorkspace.RefreshAsync(workspaceState.CurrentReadContext, audienceId, CancellationToken.None);
        }

        if (fallbackId.HasValue)
        {
            await TreeWorkspace.SelectNodeAsync(fallbackId.Value, CancellationToken.None);
            await OnNodeSelected.InvokeAsync(fallbackId.Value);
        }
        else
        {
            await TreeWorkspace.SelectNodeAsync(null, CancellationToken.None);
            if (OnNodeSelected.HasDelegate)
            {
                await OnNodeSelected.InvokeAsync(Guid.Empty);
            }
        }
    }

    private async Task HandleToggleExpandAsync(KnowledgeTreeNodeViewModel node)
    {
        if (node.IsExpanded)
        {
            TreeWorkspace.CollapseNode(node.NodeId);
        }
        else
        {
            await TreeWorkspace.ExpandNodeAsync(node.NodeId);
        }
    }

    private async Task HandlePageNextAsync(Guid nodeId)
    {
        await TreeWorkspace.PageNextAsync(nodeId);
    }

    private async Task HandlePagePreviousAsync(Guid nodeId)
    {
        await TreeWorkspace.PagePreviousAsync(nodeId);
    }

    private List<KnowledgeTreeNodeViewModel> GetVisibleNodes()
    {
        var list = new List<KnowledgeTreeNodeViewModel>();
        if (TreeWorkspace.VisualRootNode is not null)
        {
            AddVisible(TreeWorkspace.VisualRootNode, list);
        }
        return list;
    }

    private void AddVisible(KnowledgeTreeNodeViewModel node, List<KnowledgeTreeNodeViewModel> list)
    {
        list.Add(node);
        if (node.IsExpanded && node.Children.Count > 0)
        {
            foreach (var child in node.Children)
            {
                AddVisible(child, list);
            }
        }
    }

    private Task<IJSObjectReference> EnsureModuleAsync()
    {
        var moduleUri = NavigationManager.ToAbsoluteUri(Assets[ModuleAssetPath]);
        return _moduleTask ??= JSRuntime.InvokeAsync<IJSObjectReference>("import", moduleUri.AbsoluteUri).AsTask();
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask is not null)
        {
            try
            {
                var module = await _moduleTask;
                await module.InvokeVoidAsync("disposeTreeDragAndDrop", _treeElement);
            }
            catch (JSDisconnectedException ex)
            {
                Logger?.LogDebug(ex, "JS-Verbindung beim Entsorgen der Wissensbaum-Drag-and-drop-Interop bereits getrennt.");
            }
            catch (Exception ex)
            {
                Logger?.LogDebug(ex, "Wissensbaum-Drag-and-drop-Interop konnte nicht regulär freigegeben werden.");
            }
        }

        _dragAndDropReference?.Dispose();

        if (_moduleTask is not null)
        {
            try
            {
                var module = await _moduleTask;
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException ex)
            {
                Logger?.LogDebug(ex, "JS-Verbindung beim Entsorgen des Wissensbaum-Moduls bereits getrennt.");
            }
            catch (Exception ex)
            {
                Logger?.LogDebug(ex, "Wissensbaum-Modul konnte nicht regulär freigegeben werden.");
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        TreeWorkspace.Changed -= HandleTreeStateChanged;
    }
}
