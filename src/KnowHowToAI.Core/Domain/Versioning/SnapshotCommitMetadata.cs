using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Versioning;

/// <summary>
/// Herkunftsmetadaten eines durch eine Transaction erzeugten committed Snapshots.
/// Der initiale Snapshot besitzt keine Transaction und damit keine Metadaten.
/// </summary>
public sealed record SnapshotCommitMetadata(
    TransactionId TransactionId,
    string? Actor,
    string? Client,
    string? Purpose,
    string? CommitMessage);
