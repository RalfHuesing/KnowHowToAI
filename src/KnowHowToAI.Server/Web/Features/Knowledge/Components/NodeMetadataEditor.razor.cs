using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Features.Knowledge;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Components;

/// <summary>Erfasst explizit speicherbare Node-Stammdaten und Child-Nodes im Working-Kontext.</summary>
public sealed partial class NodeMetadataEditor
{
    [Inject]
    private NodeMutationApplicationService NodeMutationService { get; set; } = default!;

    [Parameter, EditorRequired]
    public NodeDetailsViewModel Node { get; set; } = default!;

    [Parameter]
    public TransactionId? TransactionId { get; set; }

    [Parameter]
    public long? ExpectedChangeVersion { get; set; }

    [Parameter]
    public EventCallback<NodeMutationResult> OnMutationSucceeded { get; set; }

    private Guid _boundNodeId;
    private string _title = string.Empty;
    private string? _description;
    private EditorMode _mode;
    private string? _errorMessage;
    private bool _isSubmitting;

    protected override void OnParametersSet()
    {
        if (_boundNodeId == Node.NodeId)
            return;

        _boundNodeId = Node.NodeId;
        ResetForm();
    }

    private void OpenEdit()
    {
        _mode = EditorMode.Edit;
        _title = Node.Title;
        _description = Node.Description;
        _errorMessage = null;
    }

    private void OpenCreate()
    {
        _mode = EditorMode.Create;
        _title = string.Empty;
        _description = null;
        _errorMessage = null;
    }

    private async Task SubmitAsync()
    {
        if (!TransactionId.HasValue)
            return;

        _isSubmitting = true;
        _errorMessage = null;
        var result = _mode == EditorMode.Edit
            ? await NodeMutationService.UpdateAsync(
                TransactionId.Value,
                new UpdateNodeRequest(new NodeId(Node.NodeId), _title, _description, ExpectedChangeVersion)).ConfigureAwait(false)
            : await NodeMutationService.CreateAsync(
                TransactionId.Value,
                new CreateNodeRequest(new NodeId(Node.NodeId), _title, _description, int.MaxValue),
                ExpectedChangeVersion).ConfigureAwait(false);
        _isSubmitting = false;

        if (!result.IsSuccess)
        {
            _errorMessage = $"[{result.Code}] {result.Error!.Message}";
            return;
        }

        ResetForm();
        await OnMutationSucceeded.InvokeAsync(result.Value!);
    }

    private void Cancel() => ResetForm();

    private void ResetForm()
    {
        _mode = EditorMode.None;
        _title = Node.Title;
        _description = Node.Description;
        _errorMessage = null;
    }

    private enum EditorMode
    {
        None,
        Edit,
        Create
    }
}
