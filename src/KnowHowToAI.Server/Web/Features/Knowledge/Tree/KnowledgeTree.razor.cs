using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using KnowHowToAI.Core.Application.Mutations.Nodes;
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
    private Guid? _creatingChildNodeId;
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

    private void BeginCreateChild(Guid parentNodeId)
    {
        _creatingChildNodeId = _creatingChildNodeId == parentNodeId ? null : parentNodeId;
    }

    private async Task HandleNodeMutationSucceededAsync(NodeMutationResult mutation)
    {
        _creatingChildNodeId = null;
        await OnNodeMutationSucceeded.InvokeAsync(mutation);
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
