namespace KnowHowToAI.Core.Domain.Audiences;

/// <summary>
/// Stabile Fehlercodes und Details für die Rollenauflösung.
/// </summary>
public static class AudienceResolutionErrorCodes
{
    public const string CandidateAudienceDeleted = "CandidateAudienceDeleted";
    public const string CandidateAudienceNotFound = "CandidateAudienceNotFound";
    public const string DuplicateCandidateAudience = "DuplicateCandidateAudience";
    public const string DuplicatePriority = "DuplicatePriority";
    public const string InvalidPriority = "InvalidPriority";
    public const string RequestedAudienceDeleted = "RequestedAudienceDeleted";
    public const string RequestedAudienceNotFound = "RequestedAudienceNotFound";

    public const string CandidateAudienceIdDetail = "candidateAudienceId";
    public const string PriorityDetail = "priority";
    public const string RequestedAudienceIdDetail = "requestedAudienceId";
}
