using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Node;

/// <summary>Lädt den lesbaren Dokumentstand der aus der Route gewählten Node.</summary>
public sealed partial class NodeDocument
{
    [Inject]
    private NavigationService NavigationService { get; set; } = default!;

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

    private NodeDetailsViewModel? _viewModel;
    private string? _markdownDownloadUrl;
    private string? _errorMessage;
    private bool _nodeNotFound;
    private bool _isLoading;
    private (Guid? NodeId, ReadContext? ReadContext, string? AudienceId, long? ChangeVersion)? _loadedRequest;

    protected override async Task OnParametersSetAsync()
    {
        var request = (NodeId, ReadContext, AudienceId, ChangeVersion);
        if (_loadedRequest == request)
            return;

        _loadedRequest = request;
        Clear();
        if (NodeId is null || ReadContext is null || string.IsNullOrWhiteSpace(AudienceId))
            return;

        _isLoading = true;
        var result = await NodeDocumentLoader.LoadAsync(
            NavigationService,
            new NodeDocumentRequest(
                new NodeReadRequest(NodeId, ReadContext, AudienceId, ChangeVersion),
                new MarkdownExportContext(QueryTransactionId)));
        _isLoading = false;
        _viewModel = result.ViewModel;
        _markdownDownloadUrl = result.MarkdownDownloadUrl;
        _nodeNotFound = result.IsNotFound;
        _errorMessage = result.ErrorMessage;
    }

    private void Clear()
    {
        _viewModel = null;
        _markdownDownloadUrl = null;
        _errorMessage = null;
        _nodeNotFound = false;
        _isLoading = false;
    }
}

