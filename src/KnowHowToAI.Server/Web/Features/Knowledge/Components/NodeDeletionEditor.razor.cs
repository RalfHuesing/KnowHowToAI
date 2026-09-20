using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Components.Shared.Dialogs;
using KnowHowToAI.Server.Web.Features.Knowledge;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Components;

/// <summary>Prüft und bestätigt die globale Löschung einer Node im Working Snapshot.</summary>
public sealed partial class NodeDeletionEditor
{
    [Inject]
    private NodeDeletionPreviewService PreviewService { get; set; } = default!;

    [Inject]
    private NodeMutationApplicationService NodeMutationService { get; set; } = default!;

    [Parameter, EditorRequired]
    public NodeDetailsViewModel Node { get; set; } = default!;

    [Parameter, EditorRequired]
    public TransactionId TransactionId { get; set; }

    [Parameter]
    public EventCallback<NodeMutationResult> OnMutationSucceeded { get; set; }

    private ConfirmationDialog? _confirmationDialog;
    private NodeDeletionPreview? _preview;
    private bool _deleteSubtree;
    private bool _isLoading;
    private string? _errorMessage;

    private string ConfirmationMessage => _preview is null
        ? string.Empty
        : $"„{_preview.Title}“ wird global über alle Rollen gelöscht. " +
          $"Die angezeigte Auswirkungsprüfung wird jetzt als {(_deleteSubtree ? "Teilbaum-Löschung" : "Einzellöschung")} ausgeführt.";

    private async Task LoadPreviewAsync()
    {
        _isLoading = true;
        _errorMessage = null;
        var result = await PreviewService.PreviewAsync(TransactionId, new NodeId(Node.NodeId));
        _isLoading = false;

        if (!result.IsSuccess)
        {
            _errorMessage = ToErrorMessage(result.Code, result.Error!.Message);
            return;
        }

        _preview = result.Value;
        _deleteSubtree = false;
    }

    private async Task OpenConfirmationAsync()
    {
        if (_preview is null || (_preview.DirectChildCount > 0 && !_deleteSubtree))
            return;

        await _confirmationDialog!.OpenAsync();
    }

    private async Task ConfirmDeleteAsync()
    {
        if (_preview is null)
            return;

        _errorMessage = null;
        var result = await NodeMutationService.DeleteAsync(
            TransactionId,
            _preview.NodeId,
            _deleteSubtree,
            _preview.ChangeVersion);
        if (!result.IsSuccess)
        {
            _errorMessage = ToErrorMessage(result.Code, result.Error!.Message);
            return;
        }

        await _confirmationDialog!.CloseAsync();
        ResetPreview();
        await OnMutationSucceeded.InvokeAsync(result.Value!);
    }

    private void CancelPreview() => ResetPreview();

    private void ResetPreview()
    {
        _preview = null;
        _deleteSubtree = false;
    }

    private static string ToErrorMessage(string code, string message) =>
        $"[{code}] {message}" + (code == "ChangeVersionConflict"
            ? " Laden Sie die Löschprüfung neu, bevor Sie es erneut versuchen."
            : string.Empty);
}
