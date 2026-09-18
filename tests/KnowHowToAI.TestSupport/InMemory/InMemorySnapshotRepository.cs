using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// In-Memory-<see cref="ISnapshotRepository"/> über einen gemeinsamen
/// <see cref="InMemoryKnowledgeStore"/>: <c>FindAsync</c> sucht per SnapshotId,
/// <c>GetCurrentAsync</c> liefert den über <see cref="InMemoryKnowledgeStore.CurrentSnapshotId"/>
/// adressierten Stand oder – ohne gesetzte Kennung – den ersten Committed-Stand.
/// Für Szenarien, die den Current-Zweig ausschließen wollen, lässt sich der Aufruf
/// über <see cref="ThrowOnGetCurrent"/> abschalten und über <see cref="GetCurrentCalls"/> zählen.
/// </summary>
public sealed class InMemorySnapshotRepository(InMemoryKnowledgeStore store) : ISnapshotRepository
{
    /// <summary>
    /// Löst bei <c>true</c> jeden GetCurrentAsync-Aufruf mit einer
    /// <see cref="InvalidOperationException"/> aus, bevor ein Snapshot gesucht wird.
    /// </summary>
    public bool ThrowOnGetCurrent { get; init; }

    /// <summary>Anzahl der GetCurrentAsync-Aufrufe seit Erzeugung des Fakes.</summary>
    public int GetCurrentCalls { get; private set; }

    public Task<Snapshot?> FindAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Snapshots.FirstOrDefault(snapshot => snapshot.SnapshotId == snapshotId));

    public Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        GetCurrentCalls++;
        if (ThrowOnGetCurrent)
            throw new InvalidOperationException("GetCurrentAsync darf nur im Current-Zweig aufgerufen werden.");

        var current = store.CurrentSnapshotId is { } currentSnapshotId
            ? store.Snapshots.FirstOrDefault(snapshot => snapshot.SnapshotId == currentSnapshotId)
            : store.Snapshots.FirstOrDefault(snapshot => snapshot.State == SnapshotState.Committed);

        return Task.FromResult(current ?? throw new InvalidOperationException(
            "Der In-Memory-Store enthält keinen aktuellen Snapshot."));
    }
}
