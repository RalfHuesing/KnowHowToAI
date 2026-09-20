using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Roles;

/// <summary>
/// Löst den explizit gewählten Rollen-Lesekontext auf und synchronisiert ihn
/// mit dem flüchtigen Seiten- und Workspace-Zustand.
/// </summary>
public sealed partial class RolesPage : ComponentBase
{
    [Inject]
    private IWebReadContextResolver ReadContextResolver { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Inject]
    private PageRegionState PageRegions { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "transactionId")]
    private string? QueryTransactionId { get; set; }

    [SupplyParameterFromQuery(Name = "snapshotId")]
    private string? QuerySnapshotId { get; set; }

    [SupplyParameterFromQuery(Name = "releaseId")]
    private string? QueryReleaseId { get; set; }

    private ReadContext _readContext = new();
    private string? _contextErrorMessage;
    private bool _isLoading;

    private long? CurrentChangeVersion => WorkspaceState.CurrentChangeVersion;

    protected override async Task OnParametersSetAsync()
    {
        var previousTransactionId = WorkspaceState.ActiveTransactionId;
        _contextErrorMessage = null;
        _isLoading = true;

        var resolution = await ReadContextResolver.ResolveAsync(
            QueryTransactionId,
            QuerySnapshotId,
            QueryReleaseId,
            CancellationToken.None);

        if (!resolution.IsSuccess)
        {
            _contextErrorMessage = RoleMapper.ToErrorMessage(resolution.Error!);
            PageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(
                KnowledgeReadContextKind.Current,
                DisplayName: "Ungültiger Kontext"));
            _isLoading = false;
            return;
        }

        var resolved = resolution.Value!;
        _readContext = resolved.ReadContext;
        var context = resolved.ContextViewModel with { ChangeVersion = resolved.ChangeVersion };
        PageRegions.SetKnowledgeContext(context);
        WorkspaceState.SetContext(context, _readContext);
        WorkspaceState.SetChangeVersion(resolved.ChangeVersion);
        if (previousTransactionId != resolved.ReadContext.TransactionId)
            WorkspaceState.SetDirty(false);

        _isLoading = false;
    }

    private Task HandleMutationSucceededAsync(long changeVersion)
    {
        WorkspaceState.SetChangeVersion(changeVersion);
        WorkspaceState.SetDirty(true);
        var context = WorkspaceState.CurrentContext with
        {
            IsDirty = true,
            ChangeVersion = changeVersion
        };
        WorkspaceState.SetContext(context, _readContext);
        PageRegions.SetKnowledgeContext(context);
        return Task.CompletedTask;
    }

}
