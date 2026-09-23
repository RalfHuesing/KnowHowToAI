using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.State;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Tree;

/// <summary>Stellt nach abgelehnten Moves den bestätigten Lesezustand des Baums wieder her.</summary>
public sealed class TreeMoveRecovery(
    WorkspaceState workspaceState,
    KnowledgePageContextResolver readContextResolver,
    PageRegionState pageRegions,
    IKnowledgeTreeWorkspace treeWorkspace)
{
    private readonly WorkspaceState _workspaceState = workspaceState ?? throw new ArgumentNullException(nameof(workspaceState));
    private readonly KnowledgePageContextResolver _readContextResolver = readContextResolver ?? throw new ArgumentNullException(nameof(readContextResolver));
    private readonly PageRegionState _pageRegions = pageRegions ?? throw new ArgumentNullException(nameof(pageRegions));
    private readonly IKnowledgeTreeWorkspace _treeWorkspace = treeWorkspace ?? throw new ArgumentNullException(nameof(treeWorkspace));

    public async Task ReloadAfterRejectionAsync(
        string? transactionId,
        CancellationToken cancellationToken)
    {
        if (_workspaceState.CurrentAudienceId is not { } audienceId)
            return;

        if (_workspaceState.ActiveTransactionId.HasValue)
        {
            await ReloadWorkspaceAsync(audienceId, cancellationToken).ConfigureAwait(false);
            return;
        }

        var contextResolution = await _readContextResolver.ResolveAsync(
            transactionId,
            cancellationToken).ConfigureAwait(false);
        if (!contextResolution.IsSuccess)
            return;

        var resolved = contextResolution.Value!;
        var updatedContext = _workspaceState.CurrentContext with { ChangeVersion = resolved.ChangeVersion };
        _workspaceState.SetContext(updatedContext, resolved.ReadContext);
        _workspaceState.SetChangeVersion(resolved.ChangeVersion);
        _pageRegions.SetKnowledgeContext(updatedContext);
        await ReloadWorkspaceAsync(audienceId, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReloadWorkspaceAsync(string audienceId, CancellationToken cancellationToken)
    {
        await _treeWorkspace.InitializeAsync(_workspaceState.CurrentReadContext, audienceId, cancellationToken).ConfigureAwait(false);
        if (_workspaceState.CurrentNodeId is { } nodeId)
            await _treeWorkspace.SelectNodeAsync(nodeId, cancellationToken).ConfigureAwait(false);
    }
}
