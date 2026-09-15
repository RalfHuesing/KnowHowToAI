using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Eindeutig aufgelöster Snapshot samt Herkunft und Tombstone-Policy für einen Read.
/// </summary>
public sealed record ResolvedReadContext(
    SnapshotId SnapshotId,
    ReadContextSource Source,
    TransactionId? TransactionId,
    bool IncludeDeleted);
