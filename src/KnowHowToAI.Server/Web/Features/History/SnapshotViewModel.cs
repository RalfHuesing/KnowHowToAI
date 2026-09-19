namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>
/// Unveränderliches UI-Datenmodell für einen Snapshot.
/// Entkoppelt Razor-Komponenten vollständig von Domain-Typen.
/// </summary>
/// <param name="SnapshotId">Eindeutige ID des Snapshots.</param>
/// <param name="State">Zustand des Snapshots (z. B. Open, Committed).</param>
/// <param name="CreatedAtUtc">Erstellungszeitpunkt in UTC.</param>
/// <param name="BaseSnapshotId">Optionale ID des Basissnapshots.</param>
/// <param name="CommittedAtUtc">Optionaler Commit-Zeitpunkt in UTC.</param>
public sealed record SnapshotViewModel(
    long SnapshotId,
    string State,
    DateTimeOffset CreatedAtUtc,
    long? BaseSnapshotId,
    DateTimeOffset? CommittedAtUtc);
