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
    public EventCallback<NodeMutationResult> OnMutationSucceeded { get; set; }

    private NodeDetailsViewModel? _viewModel;
    private string? _errorMessage;
    private bool _nodeNotFound;
    private bool _isLoading;
    private (Guid? NodeId, ReadContext? ReadContext, string? AudienceId, long? ChangeVersion, TransactionId? TransactionId)? _loadedRequest;
    private (Guid? NodeId, string? AudienceId)? _editIdentity;

    private bool IsWritableReadContext => ReadContext is { SnapshotId: null };
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
        var preserveEditingDocument = _editIdentity == editIdentity
            && _viewModel is not null
            && ReadContext?.SnapshotId is null;
        if (_editIdentity == editIdentity)
            return preserveEditingDocument;

        _editIdentity = editIdentity;
        return false;
    }

    private void PrepareDocumentLoad(bool preserveEditingDocument)
    {
        _errorMessage = null;
        _nodeNotFound = false;
        if (preserveEditingDocument)
            return;

        _viewModel = null;
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
                    new NodeReadRequest(request.NodeId, request.ReadContext, request.AudienceId, request.ChangeVersion)));
            _viewModel = result.ViewModel;
            _nodeNotFound = result.IsNotFound;
            _errorMessage = result.ErrorMessage;
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

    private async Task HandleNodeMutationSucceededAsync(NodeMutationResult mutation)
    {
        _loadedRequest = null;
        await OnMutationSucceeded.InvokeAsync(mutation);
    }

    private Task HandleContentSavedAsync(long changeVersion)
    {
        var request = (NodeId, ReadContext, AudienceId, (long?)changeVersion, TransactionId);
        _loadedRequest = request;
        return LoadDocumentAsync(request);
    }
}
