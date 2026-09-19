using System.Collections.Concurrent;

namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Koordiniert asynchrone Knoten-Lade-Requests für den Wissensbaum.
/// Gewährleistet isolierte Stornierung pro Knoten, Verknüpfung mit einem globalen CancellationToken
/// und Vermeidung von Race Conditions über Request-IDs und Kontext-Generationen.
/// </summary>
internal sealed class KnowledgeTreeRequestCoordinator : IDisposable
{
    private readonly ConcurrentDictionary<Guid, ActiveNodeRequest> _activeRequests = new();
    private CancellationTokenSource _globalCts = new();
    private long _nextRequestId;
    private bool _isDisposed;

    public CancellationToken GlobalToken => _globalCts.Token;

    public ActiveNodeRequest RegisterRequest(
        Guid nodeId,
        int generation,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var requestId = Interlocked.Increment(ref _nextRequestId);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token, cancellationToken);
        var request = new ActiveNodeRequest(requestId, generation, cts);

        _activeRequests.AddOrUpdate(
            nodeId,
            request,
            (_, old) =>
            {
                try { old.Cts.Cancel(); } catch (ObjectDisposedException) { }
                old.Cts.Dispose();
                return request;
            });

        return request;
    }

    public bool IsCurrentRequest(Guid nodeId, long requestId, int generation, int currentGeneration)
    {
        return generation == currentGeneration &&
               _activeRequests.TryGetValue(nodeId, out var active) &&
               active.RequestId == requestId;
    }

    public bool TryCompleteRequest(Guid nodeId, ActiveNodeRequest request, int currentGeneration)
    {
        var isCurrent = request.ContextGeneration == currentGeneration &&
                        _activeRequests.TryGetValue(nodeId, out var active) &&
                        active.RequestId == request.RequestId;

        if (isCurrent)
        {
            _activeRequests.TryRemove(new KeyValuePair<Guid, ActiveNodeRequest>(nodeId, request));
        }

        request.Cts.Dispose();
        return isCurrent;
    }

    public void CancelRequest(Guid nodeId)
    {
        if (_activeRequests.TryRemove(nodeId, out var existing))
        {
            try { existing.Cts.Cancel(); } catch (ObjectDisposedException) { }
            existing.Cts.Dispose();
        }
    }

    public void CancelAll()
    {
        try { _globalCts.Cancel(); } catch (ObjectDisposedException) { }
        _globalCts.Dispose();
        _globalCts = new CancellationTokenSource();

        foreach (var kvp in _activeRequests)
        {
            if (_activeRequests.TryRemove(kvp.Key, out var req))
            {
                try { req.Cts.Cancel(); } catch (ObjectDisposedException) { }
                req.Cts.Dispose();
            }
        }

        _activeRequests.Clear();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        CancelAll();
        _globalCts.Dispose();
    }
}
