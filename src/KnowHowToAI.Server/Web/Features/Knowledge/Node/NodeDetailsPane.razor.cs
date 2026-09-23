using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Node;

/// <summary>Lädt das ausgewählte Knotendokument und steuert dessen direkten Editierfluss.</summary>
public sealed partial class NodeDetailsPane
{
    [Inject]
    private NavigationService NavigationService { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Inject]
    private WorkspaceEditState WorkspaceEditState { get; set; } = default!;

    [Parameter]
    public Guid? NodeId { get; set; }

    [Parameter]
    public ReadContext? ReadContext { get; set; }

    [Parameter]
    public string? AudienceId { get; set; }

    [Parameter]
    public long? ChangeVersion { get; set; }

    [Parameter]
    public TransactionId? TransactionId { get; set; }

    [Parameter]
    public string? QueryTransactionId { get; set; }

    [Parameter]
    public string? QuerySnapshotId { get; set; }

    [Parameter]
    public string? QueryReleaseId { get; set; }

    [Parameter]
    public bool ShowTitle { get; set; } = true;

    [Parameter]
    public EventCallback<NodeMutationResult> OnMutationSucceeded { get; set; }

    private NodeDetailsViewModel? _viewModel;
    private string? _markdownDownloadUrl;
    private string? _errorMessage;
    private bool _nodeNotFound;
    private bool _isLoading;
    private bool _isEditing;
    private bool _createIndependentContent;
    private bool _focusEditorOnMount;
    private (Guid? NodeId, ReadContext? ReadContext, string? AudienceId, long? ChangeVersion, TransactionId? TransactionId)? _loadedRequest;
    private (Guid? NodeId, string? AudienceId)? _editIdentity;

    private bool CanBeginEditing =>
        _viewModel is not null
        && IsWritableReadContext
        && !string.IsNullOrWhiteSpace(AudienceId)
        && (_viewModel.Availability == "Explicit" && _viewModel.ContentMode == "Independent"
            || _viewModel.Availability is "Fallback" or "None");

    private bool IsWritableReadContext => ReadContext is { SnapshotId: null };

    private string EditActionLabel => _viewModel?.Availability is "Fallback" or "None"
        ? "Eigene Fassung erstellen"
        : "Bearbeiten";

    private string EditorMarkdown => _createIndependentContent
        ? string.Empty
        : _viewModel?.ContentMd ?? string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        var preserveEditingDocument = PrepareEditContext();

        var request = (NodeId, ReadContext, AudienceId, ChangeVersion, TransactionId);
        if (_loadedRequest == request)
            return;

        _loadedRequest = request;
        PrepareDocumentLoad(preserveEditingDocument);
        await LoadDocumentAsync(request);
    }

    private bool PrepareEditContext()
    {
        var editIdentity = (NodeId, AudienceId);
        var preserveEditingDocument = _isEditing
            && _editIdentity == editIdentity
            && _viewModel is not null
            && ReadContext?.SnapshotId is null;
        if (_editIdentity == editIdentity)
            return preserveEditingDocument;

        _editIdentity = editIdentity;
        _isEditing = false;
        _createIndependentContent = false;
        _focusEditorOnMount = false;
        return false;
    }

    private void PrepareDocumentLoad(bool preserveEditingDocument)
    {
        _errorMessage = null;
        _nodeNotFound = false;
        _markdownDownloadUrl = null;
        if (preserveEditingDocument)
            return;

        _viewModel = null;
        if (ReadContext?.SnapshotId is null)
            return;

        _isEditing = false;
        _createIndependentContent = false;
        _focusEditorOnMount = false;
    }

    private async Task LoadDocumentAsync(
        (Guid? NodeId, ReadContext? ReadContext, string? AudienceId, long? ChangeVersion, TransactionId? TransactionId) request)
    {
        if (request.NodeId is null || request.ReadContext is null || string.IsNullOrWhiteSpace(request.AudienceId))
            return;

        _isLoading = true;
        try
        {
            var result = await NodeDocumentLoader.LoadAsync(
                NavigationService,
                new NodeDocumentRequest(
                    new NodeReadRequest(request.NodeId, request.ReadContext, request.AudienceId, request.ChangeVersion),
                    new MarkdownExportContext(QueryTransactionId, QuerySnapshotId, QueryReleaseId)));
            _viewModel = result.ViewModel;
            _markdownDownloadUrl = result.MarkdownDownloadUrl;
            _nodeNotFound = result.IsNotFound;
            _errorMessage = result.ErrorMessage;
            if (_viewModel is { Availability: "Explicit", ContentMode: "Independent" })
                _createIndependentContent = false;
        }
        catch (Exception exception)
        {
            _errorMessage = $"Dokument konnte nicht geladen werden: {exception.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void BeginEditing()
    {
        if (!CanBeginEditing)
            return;

        _createIndependentContent = _viewModel!.Availability is "Fallback" or "None";
        _isEditing = true;
        _focusEditorOnMount = true;
    }

    private void EndEditing()
    {
        if (WorkspaceEditState.IsDirty)
            return;

        _isEditing = false;
        _createIndependentContent = false;
        _focusEditorOnMount = false;
    }

    private async Task HandleNodeMutationSucceededAsync(NodeMutationResult mutation)
    {
        _loadedRequest = null;
        await OnMutationSucceeded.InvokeAsync(mutation);
    }

    private Task HandleContentMutationSucceededAsync(ContentMutationUseCaseResult mutation)
    {
        var request = (NodeId, ReadContext, AudienceId, (long?)mutation.ChangeVersion, TransactionId);
        _loadedRequest = request;
        return LoadDocumentAsync(request);
    }
}
