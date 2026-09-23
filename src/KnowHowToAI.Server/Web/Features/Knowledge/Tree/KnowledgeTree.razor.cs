using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Tree;

/// <summary>
/// Nativer Blazor-Wissensbaum mit seitenbegrenztem Paging,
/// WAI-ARIA-Treeview-Semantik und Roving-Tabindex-Tastaturnavigation.
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

    private readonly Dictionary<Guid, ElementReference> _nodeElements = new();
    private ElementReference _treeElement;
    private Task<IJSObjectReference>? _moduleTask;
    private DotNetObjectReference<KnowledgeTree>? _dragAndDropReference;
    private Guid? _focusedNodeId;
    private Guid? _lastSelectedNodeId;
    private string? _moveErrorMessage;
    private Guid? _creatingChildNodeId;
    private Guid? _movingNodeId;
    private bool _isDisposed;

    private Guid? EffectiveFocusedNodeId
    {
        get
        {
            var visible = GetVisibleNodes();
            if (visible.Count == 0)
                return null;

            if (_focusedNodeId.HasValue && visible.Any(n => n.NodeId == _focusedNodeId.Value))
                return _focusedNodeId.Value;

            if (TreeWorkspace.SelectedNodeId.HasValue && visible.Any(n => n.NodeId == TreeWorkspace.SelectedNodeId.Value))
                return TreeWorkspace.SelectedNodeId.Value;

            return visible[0].NodeId;
        }
    }

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
                await module.InvokeVoidAsync("initTreeKeyboard", _treeElement);

                if (CanMove)
                {
                    _dragAndDropReference ??= DotNetObjectReference.Create(this);
                    await module.InvokeVoidAsync("initTreeDragAndDrop", _treeElement, _dragAndDropReference);
                }
                else
                {
                    await module.InvokeVoidAsync("disposeTreeDragAndDrop", _treeElement);
                    _dragAndDropReference?.Dispose();
                    _dragAndDropReference = null;
                }
            }
            catch (Exception ex)
            {
                Logger?.LogDebug(ex, "Wissensbaum-Interop konnte nicht initialisiert werden.");
            }
        }

        var selectedNodeId = TreeWorkspace.SelectedNodeId;
        if (selectedNodeId is not { } nodeId || _lastSelectedNodeId == nodeId || !_nodeElements.TryGetValue(nodeId, out var element))
            return;

        _lastSelectedNodeId = nodeId;
        _focusedNodeId = nodeId;
        await element.FocusAsync();
    }

    private void HandleTreeStateChanged()
    {
        InvokeAsync(StateHasChanged);
    }


    private async Task HandleSelectNodeAsync(Guid nodeId)
    {
        _focusedNodeId = nodeId;
        await TreeWorkspace.SelectNodeAsync(nodeId);
        await OnNodeSelected.InvokeAsync(nodeId);
    }

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
            _focusedNodeId = sourceId;
            await OnNodeMutationSucceeded.InvokeAsync(outcome.Mutation!);
        }
        else
            _moveErrorMessage = outcome.ErrorMessage;

        StateHasChanged();
    }

    private void HandleFocus(Guid nodeId)
    {
        _focusedNodeId = nodeId;
    }

    private void BeginCreateChild(Guid parentNodeId)
    {
        _creatingChildNodeId = _creatingChildNodeId == parentNodeId ? null : parentNodeId;
        _movingNodeId = null;
    }

    private void BeginMove(Guid sourceNodeId)
    {
        _movingNodeId = _movingNodeId == sourceNodeId ? null : sourceNodeId;
        _creatingChildNodeId = null;
    }

    private async Task HandleKeyboardMoveAsync(KnowledgeTreeNodeViewModel target, TreeMovePosition position)
    {
        if (_movingNodeId is not { } sourceNodeId)
            return;

        await ProcessMoveAsync(sourceNodeId, target, position);
        _movingNodeId = null;
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

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        var visibleNodes = GetVisibleNodes();
        if (visibleNodes.Count == 0)
            return;

        var currentIndex = visibleNodes.FindIndex(n => n.NodeId == EffectiveFocusedNodeId);
        if (currentIndex < 0)
        {
            currentIndex = 0;
            _focusedNodeId = visibleNodes[0].NodeId;
        }

        var currentNode = visibleNodes[currentIndex];

        switch (e.Key)
        {
            case "ArrowDown":
                await MoveFocusDownAsync(visibleNodes, currentIndex);
                break;

            case "ArrowUp":
                await MoveFocusUpAsync(visibleNodes, currentIndex);
                break;

            case "ArrowRight":
                await HandleArrowRightAsync(currentNode);
                break;

            case "ArrowLeft":
                await HandleArrowLeftAsync(currentNode);
                break;

            case "Home":
                await SetFocusAsync(visibleNodes[0].NodeId);
                break;

            case "End":
                await SetFocusAsync(visibleNodes[^1].NodeId);
                break;

            case "Enter":
            case " ":
                await HandleSelectNodeAsync(currentNode.NodeId);
                break;
        }
    }

    private async Task MoveFocusDownAsync(List<KnowledgeTreeNodeViewModel> visibleNodes, int currentIndex)
    {
        if (currentIndex < visibleNodes.Count - 1)
        {
            await SetFocusAsync(visibleNodes[currentIndex + 1].NodeId);
        }
    }

    private async Task MoveFocusUpAsync(List<KnowledgeTreeNodeViewModel> visibleNodes, int currentIndex)
    {
        if (currentIndex > 0)
        {
            await SetFocusAsync(visibleNodes[currentIndex - 1].NodeId);
        }
    }

    private async Task HandleArrowRightAsync(KnowledgeTreeNodeViewModel currentNode)
    {
        if (currentNode.HasChildren && !currentNode.IsExpanded)
        {
            await TreeWorkspace.ExpandNodeAsync(currentNode.NodeId);
        }
        else if (currentNode.HasChildren && currentNode.IsExpanded && currentNode.Children.Count > 0)
        {
            await SetFocusAsync(currentNode.Children[0].NodeId);
        }
    }

    private async Task HandleArrowLeftAsync(KnowledgeTreeNodeViewModel currentNode)
    {
        if (currentNode.HasChildren && currentNode.IsExpanded)
        {
            TreeWorkspace.CollapseNode(currentNode.NodeId);
        }
        else if (currentNode.ParentNodeId.HasValue)
        {
            await SetFocusAsync(currentNode.ParentNodeId.Value);
        }
    }

    private async Task SetFocusAsync(Guid nodeId)
    {
        _focusedNodeId = nodeId;
        if (_nodeElements.TryGetValue(nodeId, out var el))
        {
            await el.FocusAsync();
        }
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
                await module.InvokeVoidAsync("disposeTreeKeyboard", _treeElement);
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
                Logger?.LogDebug(ex, "Wissensbaum-Tastaturmodul konnte nicht regulär freigegeben werden.");
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
