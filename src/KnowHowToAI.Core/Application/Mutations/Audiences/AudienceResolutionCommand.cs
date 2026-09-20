using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Audiences;

/// <summary>
/// Parameter für set_audience_resolution: vollständiger Ersatz der Kandidatenliste
/// für eine angefragte Zielgruppe. Reihenfolge der Kandidaten bestimmt die Priority.
/// </summary>
public sealed record AudienceResolutionCommand(
    TransactionId TransactionId,
    AudienceId RequestedAudienceId,
    IReadOnlyList<AudienceId> CandidateAudienceIds);

