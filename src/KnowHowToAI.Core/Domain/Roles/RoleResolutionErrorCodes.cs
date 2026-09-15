namespace KnowHowToAI.Core.Domain.Roles;

/// <summary>
/// Stabile Fehlercodes und Details für die Rollenauflösung.
/// </summary>
public static class RoleResolutionErrorCodes
{
    public const string CandidateRoleDeleted = "CandidateRoleDeleted";
    public const string CandidateRoleNotFound = "CandidateRoleNotFound";
    public const string DuplicateCandidateRole = "DuplicateCandidateRole";
    public const string DuplicatePriority = "DuplicatePriority";
    public const string InvalidPriority = "InvalidPriority";
    public const string RequestedRoleDeleted = "RequestedRoleDeleted";
    public const string RequestedRoleNotFound = "RequestedRoleNotFound";

    public const string CandidateRoleIdDetail = "candidateRoleId";
    public const string PriorityDetail = "priority";
    public const string RequestedRoleIdDetail = "requestedRoleId";
}
