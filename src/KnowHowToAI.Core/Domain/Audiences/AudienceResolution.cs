using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Audiences;

/// <summary>
/// Ein expliziter Kandidat einer nicht rekursiven Zielgruppenauflösungsreihenfolge.
/// </summary>
public sealed record AudienceResolution(
    SnapshotId SnapshotId,
    AudienceId RequestedAudienceId,
    AudienceId CandidateAudienceId,
    int Priority);
