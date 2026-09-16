using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Domain.Roles;

/// <summary>
/// Löst Content ausschließlich entlang der explizit gespeicherten Kandidatenreihenfolge auf.
/// </summary>
public static class RoleResolver
{
    public static Result<RoleResolutionResult> Resolve(RoleResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Roles);
        ArgumentNullException.ThrowIfNull(request.ResolutionOrder);
        ArgumentNullException.ThrowIfNull(request.Contents);

        var validation = ValidateOrder(
            request.SnapshotId,
            request.RequestedRole,
            request.Roles,
            request.ResolutionOrder);
        if (!validation.IsSuccess)
            return Result<RoleResolutionResult>.Failure(validation.Error!);

        var candidates = request.ResolutionOrder
            .Where(resolution => resolution.SnapshotId == request.SnapshotId
                && resolution.RequestedRoleId == request.RequestedRole)
            .OrderBy(resolution => resolution.Priority)
            .ToArray();

        var match = FindFirstContentMatch(request, candidates);
        if (match.Candidate is null || match.Content is null)
            return Result<RoleResolutionResult>.Success(
                CreateUnavailableResult(request.RequestedRole, isConfigured: candidates.Length > 0));

        return Result<RoleResolutionResult>.Success(CreateResolvedResult(request.RequestedRole, match.Candidate, match.Content));
    }

    /// <summary>
    /// Prueft die angefragte Rolle und ihre vollstaendige Resolution Order mit denselben
    /// Regeln und Fehlercodes wie <see cref="Resolve"/> - unabhaengig vom Content eines
    /// konkreten Node, zum Beispiel fuer Suchanfragen.
    /// </summary>
    public static Result<bool> ValidateOrder(
        SnapshotId snapshotId,
        RoleId requestedRole,
        IEnumerable<Role> roles,
        IEnumerable<RoleResolution> resolutions)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(resolutions);

        var snapshotRoles = roles.Where(role => role.SnapshotId == snapshotId).ToArray();
        var requestedRoleError = ValidateRequestedRole(requestedRole, snapshotRoles);
        if (requestedRoleError is not null)
            return Result<bool>.Failure(requestedRoleError);

        var candidates = resolutions
            .Where(resolution => resolution.SnapshotId == snapshotId
                && resolution.RequestedRoleId == requestedRole)
            .OrderBy(resolution => resolution.Priority)
            .ToArray();

        var candidateError = ValidateCandidates(candidates, snapshotRoles);
        return candidateError is not null
            ? Result<bool>.Failure(candidateError)
            : Result<bool>.Success(true);
    }

    private static DomainError? ValidateRequestedRole(RoleId requestedRoleId, IReadOnlyCollection<Role> roles)
    {
        var requestedRole = roles.SingleOrDefault(role => role.RoleId == requestedRoleId);
        if (requestedRole is null)
            return CreateError(
                RoleResolutionErrorCodes.RequestedRoleNotFound,
                "Die angefragte Rolle ist im Snapshot nicht vorhanden.",
                RoleResolutionErrorCodes.RequestedRoleIdDetail,
                requestedRoleId.ToString());

        return requestedRole.IsDeleted
            ? CreateError(
                RoleResolutionErrorCodes.RequestedRoleDeleted,
                "Die angefragte Rolle ist gelöscht.",
                RoleResolutionErrorCodes.RequestedRoleIdDetail,
                requestedRoleId.ToString())
            : null;
    }

    private static (RoleResolution? Candidate, NodeContent? Content) FindFirstContentMatch(
        RoleResolutionRequest request,
        IEnumerable<RoleResolution> candidates)
    {
        var contents = request.Contents.Where(content => content.SnapshotId == request.SnapshotId
            && content.NodeId == request.NodeId
            && !content.IsDeleted);

        foreach (var candidate in candidates)
        {
            var content = contents.FirstOrDefault(item => item.RoleId == candidate.CandidateRoleId);
            if (content is not null)
                return (candidate, content);
        }

        return (null, null);
    }

    private static RoleResolutionResult CreateUnavailableResult(RoleId requestedRole, bool isConfigured) =>
        new(requestedRole, null, Availability.None, false, isConfigured, null);

    private static RoleResolutionResult CreateResolvedResult(
        RoleId requestedRole,
        RoleResolution candidate,
        NodeContent content)
    {
        var fallbackUsed = candidate.CandidateRoleId != requestedRole;
        return new RoleResolutionResult(
            requestedRole,
            candidate.CandidateRoleId,
            fallbackUsed ? Availability.Fallback : Availability.Explicit,
            fallbackUsed,
            true,
            new ResolvedContentMetadata(
                content.NodeId,
                content.RoleId,
                content.ContentRevisionId,
                content.ContentMode,
                content.ContentMd.Length));
    }

    private static DomainError? ValidateCandidates(
        IReadOnlyList<RoleResolution> candidates,
        IReadOnlyCollection<Role> roles)
    {
        var candidateRoleIds = new HashSet<RoleId>();
        var priorities = new HashSet<int>();
        foreach (var candidate in candidates)
        {
            if (candidate.Priority <= 0)
                return CreateError(
                    RoleResolutionErrorCodes.InvalidPriority,
                    "Die Priorität eines Rollenkandidaten muss positiv sein.",
                    RoleResolutionErrorCodes.PriorityDetail,
                    candidate.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture));

            if (!candidateRoleIds.Add(candidate.CandidateRoleId))
                return CreateError(
                    RoleResolutionErrorCodes.DuplicateCandidateRole,
                    "Eine Rolle darf innerhalb einer Auflösungsreihenfolge nur einmal vorkommen.",
                    RoleResolutionErrorCodes.CandidateRoleIdDetail,
                    candidate.CandidateRoleId.ToString());

            if (!priorities.Add(candidate.Priority))
                return CreateError(
                    RoleResolutionErrorCodes.DuplicatePriority,
                    "Die Kandidatenpriorität muss innerhalb einer Auflösungsreihenfolge eindeutig sein.",
                    RoleResolutionErrorCodes.PriorityDetail,
                    candidate.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture));

            var role = roles.SingleOrDefault(item => item.RoleId == candidate.CandidateRoleId);
            if (role is null)
                return CreateError(
                    RoleResolutionErrorCodes.CandidateRoleNotFound,
                    "Ein konfigurierter Rollenkandidat ist im Snapshot nicht vorhanden.",
                    RoleResolutionErrorCodes.CandidateRoleIdDetail,
                    candidate.CandidateRoleId.ToString());

            if (role.IsDeleted)
                return CreateError(
                    RoleResolutionErrorCodes.CandidateRoleDeleted,
                    "Ein konfigurierter Rollenkandidat ist gelöscht.",
                    RoleResolutionErrorCodes.CandidateRoleIdDetail,
                    candidate.CandidateRoleId.ToString());
        }

        return null;
    }

    private static DomainError CreateError(string code, string message, string detailName, string detailValue) =>
        new(code, message, new Dictionary<string, string>
        {
            [detailName] = detailValue
        });
}
