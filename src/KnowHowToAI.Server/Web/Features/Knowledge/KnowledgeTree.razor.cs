using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Nativer Blazor-Wissensbaum mit seitenbegrenztem Paging,
/// WAI-ARIA-Treeview-Semantik und Roving-Tabindex-Tastaturnavigation.
/// </summary>
public sealed partial class KnowledgeTree : IDisposable
{
    [Inject]
    public IKnowledgeTreeWorkspace TreeWorkspace { get; set; } = default!;

    [Parameter]
    public EventCallback<Guid> OnNodeSelected { get; set; }

    private readonly Dictionary<Guid, ElementReference> _nodeElements = new();
    private Guid? _focusedNodeId;
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

    private void HandleFocus(Guid nodeId)
    {
        _focusedNodeId = nodeId;
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

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        TreeWorkspace.Changed -= HandleTreeStateChanged;
    }
}
