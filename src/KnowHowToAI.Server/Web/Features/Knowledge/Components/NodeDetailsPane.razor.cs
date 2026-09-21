using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Components;

/// <summary>Lädt und zeigt die Details des aus der Route ausgewählten Knotens.</summary>
public sealed partial class NodeDetailsPane
{
    [Inject]
    private NavigationService NavigationService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

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
    public EventCallback<NodeMutationResult> OnMutationSucceeded { get; set; }

    [Parameter]
    public EventCallback<ContentMutationUseCaseResult> OnContentMutationSucceeded { get; set; }

    private NodeDetailsViewModel? _viewModel;
    private string? _markdownDownloadUrl;
    private string? _errorMessage;
    private bool _nodeNotFound;
    private bool _isLoading;
    private (Guid? NodeId, ReadContext? ReadContext, string? AudienceId, long? ChangeVersion)? _loadedRequest;
    private bool _editDialogOpen;
    private bool _isLoadingEditOptions;
    private bool _isBeginningWorkingCopy;
    private string? _editDialogError;
    private string? _beginErrorMessage;
    private IReadOnlyList<WorkingCopyOption> _workingCopyOptions = [];

    private bool IsExplicitIndependentContent =>
        _viewModel is not null
        && _viewModel.Availability == "Explicit"
        && _viewModel.ContentMode == "Independent";

    private bool CanEditContent => IsExplicitIndependentContent && TransactionId.HasValue;

    private bool CanStartEdit =>
        IsExplicitIndependentContent
        && !TransactionId.HasValue
        && ReadContext is { TransactionId: null, SnapshotId: null }
        && !string.IsNullOrWhiteSpace(AudienceId);

    private bool ShowReadOnlyContent => !CanEditContent;

    protected override async Task OnParametersSetAsync()
    {
        var request = (NodeId, ReadContext, AudienceId, ChangeVersion);
        if (_loadedRequest == request)
            return;

        _loadedRequest = request;
        Clear();
        if (NodeId is not { } nodeId || ReadContext is null || string.IsNullOrWhiteSpace(AudienceId))
            return;

        _isLoading = true;
        var result = await NavigationService.GetNodeAsync(
            new NodeId(nodeId),
            ReadContext,
            new AudienceId(AudienceId),
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
        _markdownDownloadUrl = CreateMarkdownDownloadUrl(nodeId, AudienceId);
    }

    private async Task HandleMutationSucceededAsync(NodeMutationResult mutation)
    {
        await OnMutationSucceeded.InvokeAsync(mutation);
        _loadedRequest = null;
    }

    private async Task OpenEditDialogAsync()
    {
        if (!CanStartEdit)
            return;

        _editDialogOpen = true;
        _isLoadingEditOptions = true;
        _editDialogError = null;
        _beginErrorMessage = null;
        _workingCopyOptions = [];
        await InvokeAsync(StateHasChanged);

        try
        {
            var transactionService = Services.GetRequiredService<TransactionService>();
            var audienceCatalog = Services.GetRequiredService<IContextSelectionAudienceCatalog>();
            var openTransactions = await transactionService.ListOpenAsync(CancellationToken.None);
            var options = new List<WorkingCopyOption>(openTransactions.Count);
            foreach (var transaction in openTransactions
                         .OrderByDescending(item => item.CreatedAtUtc)
                         .ThenBy(item => item.TransactionId.Value))
            {
                options.Add(await EvaluateWorkingCopyAsync(transaction, audienceCatalog));
            }

            _workingCopyOptions = options;
        }
        catch (Exception exception)
        {
            _editDialogError = $"Offene Arbeitskopien konnten nicht geprüft werden: {exception.Message}";
        }
        finally
        {
            _isLoadingEditOptions = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private Task CloseEditDialogAsync()
    {
        _editDialogOpen = false;
        _editDialogError = null;
        _beginErrorMessage = null;
        return InvokeAsync(StateHasChanged);
    }

    private async Task SelectWorkingCopyAsync(WorkingCopyOption option)
    {
        if (!option.IsCompatible || _isBeginningWorkingCopy)
            return;

        NavigateToWorkingCopy(option.TransactionId);
        await Task.CompletedTask;
    }

    private async Task BeginWorkingCopyAsync()
    {
        if (_isBeginningWorkingCopy || !CanStartEdit)
            return;

        _isBeginningWorkingCopy = true;
        _beginErrorMessage = null;
        await InvokeAsync(StateHasChanged);

        Result<KnowledgeTransaction> result;
        try
        {
            result = await Services.GetRequiredService<TransactionService>().BeginAsync(
                new BeginTransactionOptions(
                    $"Bearbeitung: {_viewModel!.Title}",
                    Services.GetRequiredService<ICurrentUserService>().GetCurrentUserName(),
                    "Web UI"),
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _isBeginningWorkingCopy = false;
            _beginErrorMessage = $"Neue Arbeitskopie konnte nicht begonnen werden: {exception.Message}";
            await InvokeAsync(StateHasChanged);
            return;
        }

        _isBeginningWorkingCopy = false;
        if (!result.IsSuccess)
        {
            _beginErrorMessage = result.Error?.Message ?? "Neue Arbeitskopie konnte nicht begonnen werden.";
            await InvokeAsync(StateHasChanged);
            return;
        }

        var transaction = result.Value!;
        WorkingCopyOption option;
        try
        {
            option = await EvaluateWorkingCopyAsync(
                transaction,
                Services.GetRequiredService<IContextSelectionAudienceCatalog>());
        }
        catch (Exception exception)
        {
            option = WorkingCopyOption.Incompatible(
                transaction,
                string.IsNullOrWhiteSpace(transaction.Purpose)
                    ? $"Arbeitskopie {transaction.TransactionId.Value:D}"
                    : transaction.Purpose,
                $"Working Snapshot konnte nach dem Start nicht stabil geprüft werden ({exception.Message}).");
        }
        if (option.IsCompatible)
        {
            NavigateToWorkingCopy(transaction.TransactionId.Value);
            return;
        }

        // Current wurde nach dem Start verändert oder erfüllt den Vertrag nicht
        // mehr. Die erfolgreich erzeugte Arbeitskopie bleibt sichtbar und offen.
        _workingCopyOptions = _workingCopyOptions
            .Where(item => item.TransactionId != transaction.TransactionId.Value)
            .Append(option)
            .ToArray();
        _beginErrorMessage = "Die Arbeitskopie wurde begonnen, ist für diesen Wissenseintrag aber wegen eines zwischenzeitlichen Current-Races nicht kompatibel. Sie bleibt offen und kann über ihre Details geprüft werden.";
        await InvokeAsync(StateHasChanged);
    }

    private async Task<WorkingCopyOption> EvaluateWorkingCopyAsync(
        KnowledgeTransaction transaction,
        IContextSelectionAudienceCatalog audienceCatalog)
    {
        var displayName = string.IsNullOrWhiteSpace(transaction.Purpose)
            ? $"Arbeitskopie {transaction.TransactionId.Value:D}"
            : transaction.Purpose;
        var workingContext = new ReadContext(TransactionId: transaction.TransactionId);

        var audiences = await audienceCatalog.LoadAsync(workingContext, CancellationToken.None);
        if (!string.IsNullOrWhiteSpace(audiences.ErrorMessage))
            return WorkingCopyOption.Incompatible(transaction, displayName, "Zielgruppen konnten im Working Snapshot nicht geprüft werden.");

        if (!audiences.Audiences.Any(item => string.Equals(item.Id, AudienceId, StringComparison.Ordinal)))
            return WorkingCopyOption.Incompatible(transaction, displayName, $"Die Zielgruppe „{AudienceId}“ ist im Working Snapshot nicht aktiv oder verfügbar.");

        var nodeResult = await NavigationService.GetNodeAsync(
            new NodeId(NodeId!.Value),
            workingContext,
            new AudienceId(AudienceId!),
            CancellationToken.None);
        if (!nodeResult.IsSuccess || nodeResult.Value is null)
            return WorkingCopyOption.Incompatible(transaction, displayName, "Der Wissenseintrag ist im Working Snapshot nicht aktiv.");

        var node = KnowledgeNavigationMapper.ToNodeDetailsViewModel(nodeResult.Value);
        if (node is null)
            return WorkingCopyOption.Incompatible(transaction, displayName, "Der Wissenseintrag ist im Working Snapshot nicht aktiv.");
        if (!string.Equals(node.Availability, "Explicit", StringComparison.Ordinal))
            return WorkingCopyOption.Incompatible(transaction, displayName, "Der Inhalt ist im Working Snapshot nicht Explicit.");
        if (!string.Equals(node.ContentMode, "Independent", StringComparison.Ordinal))
            return WorkingCopyOption.Incompatible(transaction, displayName, "Der Inhalt ist im Working Snapshot nicht Independent.");

        return WorkingCopyOption.Compatible(transaction, displayName);
    }

    private void NavigateToWorkingCopy(Guid transactionId)
    {
        var uri = $"/knowledge/{NodeId!.Value:D}?transactionId={transactionId:D}&audienceId={Uri.EscapeDataString(AudienceId!)}";
        NavigationManager.NavigateTo(uri);
    }

    private static string GetTransactionDetailsUrl(Guid transactionId) =>
        $"/transactions/{transactionId:D}";

    private Task HandleContentMutationSucceededAsync(ContentMutationUseCaseResult mutation) =>
        OnContentMutationSucceeded.InvokeAsync(mutation);

    private string CreateMarkdownDownloadUrl(Guid nodeId, string audienceId)
    {
        var query = new Dictionary<string, string?>
        {
            ["nodeId"] = nodeId.ToString("D"),
            ["audienceId"] = AudienceId,
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
        _editDialogOpen = false;
        _isLoadingEditOptions = false;
        _isBeginningWorkingCopy = false;
        _editDialogError = null;
        _beginErrorMessage = null;
        _workingCopyOptions = [];
    }

    private sealed record WorkingCopyOption(
        Guid TransactionId,
        string DisplayName,
        bool IsCompatible,
        string StatusLabel,
        string? ExclusionReason)
    {
        public static WorkingCopyOption Compatible(KnowledgeTransaction transaction, string displayName) =>
            new(transaction.TransactionId.Value, displayName, true, "Open", null);

        public static WorkingCopyOption Incompatible(KnowledgeTransaction transaction, string displayName, string reason) =>
            new(transaction.TransactionId.Value, displayName, false, "Open", reason);
    }
}
