using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Selektiert den Wissensstand für einen Read. Ohne Selektor wird der Current Snapshot gelesen.
/// </summary>
public sealed record ReadContext(
    TransactionId? TransactionId = null,
    SnapshotId? SnapshotId = null,
    bool IncludeDeleted = false);
