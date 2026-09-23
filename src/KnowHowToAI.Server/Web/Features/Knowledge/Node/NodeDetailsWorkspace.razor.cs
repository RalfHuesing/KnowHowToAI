using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Shared.Tabs;
using KnowHowToAI.Server.Web.Features.Content;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Node;

/// <summary>Steuert die Ansichten und hält die Editor-Instanz beim Wechsel erhalten.</summary>
public sealed partial class NodeDetailsWorkspace
{
    [Parameter]
    public NodeDetailsViewModel? ViewModel { get; set; }

    [Parameter]
    public bool IsLoading { get; set; }

    [Parameter]
    public string? ErrorMessage { get; set; }

    [Parameter]
    public bool NodeNotFound { get; set; }

    [Parameter]
    public bool IsWorking { get; set; }

    [Parameter]
    public bool IsWritableReadContext { get; set; }

    [Parameter]
    public long? ChangeVersion { get; set; }

    [Parameter]
    public long? LoadedCurrentSnapshotId { get; set; }

    [Parameter]
    public EventCallback<NodeMutationResult> OnNodeMutationSucceeded { get; set; }

    [Parameter]
    public EventCallback<long> OnContentSaved { get; set; }

    private ContentEditor? _contentEditor;
    private NodeView _activeView = NodeView.Read;
    private (Guid NodeId, string AudienceId)? _identity;
    private bool _hasOpenedMetadata;
    private bool _hasOpenedEditor;
    private bool _createIndependentContent;
    private bool _focusEditorOnMount;
    private bool _activateEditorAfterRender;

    private static readonly IReadOnlyList<TabDefinition> Tabs =
    [
        new("Read", "Lesen"),
        new("Editor", "Bearbeiten"),
        new("Metadata", "Titel"),
        new("Technical", "Technische Details")
    ];

    private string ActiveViewKey => _activeView.ToString();
    private string ActiveViewName => ActiveViewKey;

    private string EditorMarkdown => _createIndependentContent ? string.Empty : ViewModel?.ContentMd ?? string.Empty;

    protected override void OnParametersSet()
    {
        if (ViewModel is null)
            return;

        var identity = (ViewModel.NodeId, ViewModel.RequestedAudienceId);
        if (_identity == identity)
            return;

        _identity = identity;
        _activeView = NodeView.Read;
        _hasOpenedMetadata = false;
        _hasOpenedEditor = false;
        _createIndependentContent = false;
        _focusEditorOnMount = false;
        _activateEditorAfterRender = false;
        _contentEditor = null;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_activateEditorAfterRender)
            return;

        _activateEditorAfterRender = false;
        _focusEditorOnMount = false;
        if (_contentEditor is not null)
            await _contentEditor.ActivateAsync();
    }

    private void SelectView(NodeView view)
    {
        if (ViewModel is null)
            return;

        _activeView = view;
        if (view == NodeView.Metadata)
            _hasOpenedMetadata = true;
        if (view != NodeView.Editor)
        {
            _focusEditorOnMount = false;
            return;
        }

        _createIndependentContent = ViewModel.Availability is "Fallback" or "None";
        if (ViewModel.ContentMode == "Derived" || _hasOpenedEditor)
            _activateEditorAfterRender = _hasOpenedEditor;
        _hasOpenedEditor = true;
        _focusEditorOnMount = !_activateEditorAfterRender;
    }

    private Task SelectTabAsync(string key)
    {
        if (Enum.TryParse<NodeView>(key, out var view))
            SelectView(view);

        return Task.CompletedTask;
    }

    private Task HandleContentMutationSucceededAsync(ContentMutationUseCaseResult result) =>
        OnContentSaved.InvokeAsync(result.ChangeVersion);

    private enum NodeView
    {
        Read,
        Metadata,
        Editor,
        Technical
    }
}
