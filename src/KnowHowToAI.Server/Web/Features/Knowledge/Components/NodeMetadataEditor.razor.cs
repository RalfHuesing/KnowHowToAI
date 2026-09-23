using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Server.Web.Features.Knowledge.Node;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Workflow;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Components;

/// <summary>Erfasst Titel und Beschreibung als eigenständige, explizit speicherbare Node-Mutation.</summary>
public sealed partial class NodeMetadataEditor
{
    [Inject]
    private NodeMutationApplicationService NodeMutationService { get; set; } = default!;

    [Inject]
    private WebWriteCoordinator WebWriteCoordinator { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Inject]
    private WorkspaceEditState WorkspaceEditState { get; set; } = default!;

    [Parameter, EditorRequired]
    public NodeDetailsViewModel Node { get; set; } = default!;

    [Parameter]
    public long? ExpectedChangeVersion { get; set; }

    [Parameter]
    public EventCallback<NodeMutationResult> OnMutationSucceeded { get; set; }

    private string _title = string.Empty;
    private string? _description;
    private string? _errorMessage;
    private IReadOnlyList<DomainWarning> _warnings = [];
    private bool _isSubmitting;
    private bool _isDirty;
    private Guid _boundNodeId;
    private ElementReference _titleInputElement;
    private bool _shouldFocusTitle;

    protected override void OnParametersSet()
    {
        if (_boundNodeId != Node.NodeId)
        {
            _boundNodeId = Node.NodeId;
            _shouldFocusTitle = true;
            SetDraft(Node.Title, Node.Description);
            return;
        }

        if (!_isDirty)
            SetDraft(Node.Title, Node.Description);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_shouldFocusTitle)
        {
            _shouldFocusTitle = false;
            await _titleInputElement.FocusAsync();
        }
    }

    private async Task SubmitAsync()
    {
        if (_isSubmitting)
            return;

        _isSubmitting = true;
        _errorMessage = null;
        _warnings = [];
        try
        {
            var result = await WebWriteCoordinator.WriteAsync(
                WorkspaceState.LoadedSnapshotId,
                (transactionId, changeVersion, cancellationToken) => NodeMutationService.UpdateAsync(
                    transactionId,
                    new UpdateNodeRequest(
                        new NodeId(Node.NodeId),
                        _title,
                        _description,
                        changeVersion ?? ExpectedChangeVersion),
                    cancellationToken),
                value => value.ChangeVersion);

            _warnings = result.Mutation.Warnings;
            if (!result.Mutation.IsSuccess)
            {
                _errorMessage = $"[{result.Mutation.Error!.Code}] {result.Mutation.Error.Message}";
                return;
            }

            SetDraft(_title, _description);
            await OnMutationSucceeded.InvokeAsync(result.Mutation.Value!);
        }
        catch (Exception exception)
        {
            _errorMessage = $"[{exception.GetType().Name}] {exception.Message}";
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel()
    {
        SetDraft(Node.Title, Node.Description);
        _errorMessage = null;
        _warnings = [];
    }

    private void SetDraft(string title, string? description)
    {
        _title = title;
        _description = description;
        _isDirty = false;
        WorkspaceEditState.SetDirty(false, DirtySource);
    }

    private void UpdateDirty()
    {
        _isDirty = !string.Equals(_title, Node.Title, StringComparison.Ordinal)
            || !string.Equals(_description, Node.Description, StringComparison.Ordinal);
        WorkspaceEditState.SetDirty(_isDirty, DirtySource);
        _errorMessage = null;
        _warnings = [];
    }

    private string DirtySource => $"node-metadata:{Node.NodeId:D}";
}
