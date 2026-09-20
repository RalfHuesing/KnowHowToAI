using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Components;

/// <summary>Lädt und zeigt die Details des aus der Route ausgewählten Knotens.</summary>
public sealed partial class NodeDetailsPane
{
    [Inject]
    private NavigationService NavigationService { get; set; } = default!;

    [Parameter]
    public Guid? NodeId { get; set; }

    [Parameter]
    public ReadContext? ReadContext { get; set; }

    [Parameter]
    public string? RoleId { get; set; }

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
    public EventCallback<NodeMutationResult> OnMutationSucceeded { get; set; }

    private NodeDetailsViewModel? _viewModel;
    private string? _markdownDownloadUrl;
    private string? _errorMessage;
    private bool _nodeNotFound;
    private bool _isLoading;
    private (Guid? NodeId, ReadContext? ReadContext, string? RoleId, long? ChangeVersion)? _loadedRequest;

    protected override async Task OnParametersSetAsync()
    {
        var request = (NodeId, ReadContext, RoleId, ChangeVersion);
        if (_loadedRequest == request)
            return;

        _loadedRequest = request;
        Clear();
        if (NodeId is not { } nodeId || ReadContext is null || string.IsNullOrWhiteSpace(RoleId))
            return;

        _isLoading = true;
        var result = await NavigationService.GetNodeAsync(
            new NodeId(nodeId),
            ReadContext,
            new RoleId(RoleId),
            CancellationToken.None);
        _isLoading = false;

        if (!result.IsSuccess)
        {
            if (string.Equals(result.Error!.Code, "NodeNotFound", StringComparison.Ordinal))
                _nodeNotFound = true;
            else
                _errorMessage = result.Error.Message;
            return;
        }

        _viewModel = KnowledgeNavigationMapper.ToNodeDetailsViewModel(result.Value, ChangeVersion);
        _markdownDownloadUrl = CreateMarkdownDownloadUrl(nodeId, RoleId);
    }

    private async Task HandleMutationSucceededAsync(NodeMutationResult mutation)
    {
        await OnMutationSucceeded.InvokeAsync(mutation);
        _loadedRequest = null;
    }

    private string CreateMarkdownDownloadUrl(Guid nodeId, string roleId)
    {
        var query = new Dictionary<string, string?>
        {
            ["nodeId"] = nodeId.ToString("D"),
            ["roleId"] = roleId,
            ["transactionId"] = QueryTransactionId,
            ["snapshotId"] = QuerySnapshotId,
            ["releaseId"] = QueryReleaseId
        };

        return QueryHelpers.AddQueryString("/downloads/markdown", query);
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
