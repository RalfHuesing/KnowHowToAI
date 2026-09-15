using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Bereits geladene Kandidaten für die Auflösung eines Read-Kontexts.
/// </summary>
public sealed record ReadContextCandidates(
    SnapshotId CurrentSnapshotId,
    KnowledgeTransaction? Transaction,
    Snapshot? Snapshot);
