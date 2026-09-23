using KnowHowToAI.Core.Application.Navigation;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Tree;

public sealed partial class KnowledgeTreeState
{
    public Task InitializeAsync(ReadContext context, string audienceId, CancellationToken cancellationToken = default) =>
        InitializeCoreAsync(context, audienceId, preserveExpandedNodes: false, cancellationToken);

    Task IKnowledgeTreeWorkspace.RefreshAsync(ReadContext context, string audienceId, CancellationToken cancellationToken) =>
        RefreshAsync(context, audienceId, cancellationToken);

    internal Task RefreshAsync(ReadContext context, string audienceId, CancellationToken cancellationToken = default) =>
        InitializeCoreAsync(context, audienceId, preserveExpandedNodes: true, cancellationToken);

    private async Task InitializeCoreAsync(
        ReadContext context,
        string audienceId,
        bool preserveExpandedNodes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(audienceId);
        ThrowIfDisposed();

        var generation = Interlocked.Increment(ref _contextGeneration);
        _requestCoordinator.CancelAll();
        CurrentReadContext = context;
        CurrentAudienceId = audienceId;
        SelectedNodeId = null;
        VisualRootNodeId = null;
        RootNode = null;
        RootError = null;
        StatusMessage = null;
        _cache.Clear();
        _knownNodes.Clear();
        if (!preserveExpandedNodes)
            _expandedNodeIds.Clear();

        IsLoading = true;
        Changed?.Invoke();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_requestCoordinator.GlobalToken, cancellationToken);
        await LoadRootAsync(context, audienceId, generation, linkedCts.Token).ConfigureAwait(false);
    }
}
