using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>
/// Write-Port für die atomare Registrierung eines unveränderlichen Release-Verweises.
/// Diese Operation benötigt keine KnowHowTo-AI-Transaction, da sie keinen versionierten
/// Snapshot-Inhalt ändert (V1-Invariante: Release ist Metadatenverwaltung).
/// </summary>
public interface IReleaseMutationRepository
{
    /// <summary>
    /// Registriert atomar einen neuen Release-Verweis. Schlägt mit <c>ReleaseNameConflict</c>
    /// fehl, wenn der Name bereits vergeben ist. Der referenzierte Snapshot muss committed sein.
    /// </summary>
    Task<Result<Release>> CreateAsync(
        ReleaseId releaseId,
        string name,
        SnapshotId snapshotId,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Paginierte, deterministisch sortierte Liste aller Releases.</summary>
    Task<IReadOnlyList<Release>> ListAsync(
        int limit,
        long? afterReleaseId,
        CancellationToken cancellationToken = default);
}
