using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Components;

/// <summary>Erfasst den ersten Root-Knoten einer leeren Wissensbasis im Working-Kontext.</summary>
public sealed partial class RootNodeEditor : IDisposable
{
    [Inject]
    private NodeMutationApplicationService NodeMutationService { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Parameter]
    public TransactionId? TransactionId { get; set; }

    [Parameter]
    public long? ExpectedChangeVersion { get; set; }

    [Parameter]
    public EventCallback<NodeMutationResult> OnMutationSucceeded { get; set; }

    private string _title = string.Empty;
    private string? _description;
    private string? _errorMessage;
    private bool _isSubmitting;

    private const string InitialTitle = "";
    private const string? InitialDescription = null;

    private async Task SubmitAsync()
    {
        if (!TransactionId.HasValue)
            return;

        _isSubmitting = true;
        _errorMessage = null;
        var result = await NodeMutationService.CreateAsync(
            TransactionId.Value,
            new CreateNodeRequest(ParentNodeId: null, _title, _description, SortOrder: 0),
            ExpectedChangeVersion);
        _isSubmitting = false;

        if (!result.IsSuccess)
        {
            _errorMessage = $"[{result.Code}] {result.Error!.Message}";
            return;
        }

        WorkspaceState.SetDirty(false);
        await OnMutationSucceeded.InvokeAsync(result.Value!);
    }

    private void UpdateDirty()
    {
        var isDirty = !string.Equals(_title, InitialTitle, StringComparison.Ordinal)
            || !string.Equals(_description, InitialDescription, StringComparison.Ordinal);
        WorkspaceState.SetDirty(isDirty);
    }

    private void Cancel()
    {
        _title = InitialTitle;
        _description = InitialDescription;
        _errorMessage = null;
        WorkspaceState.SetDirty(false);
    }

    public void Dispose() => WorkspaceState.SetDirty(false);
}
