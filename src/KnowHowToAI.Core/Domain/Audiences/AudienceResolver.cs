using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Domain.Audiences;

/// <summary>
/// Löst Content ausschließlich entlang der explizit gespeicherten Kandidatenreihenfolge auf.
/// </summary>
public static class AudienceResolver
{
    public static Result<AudienceResolutionResult> Resolve(AudienceResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Audiences);
        ArgumentNullException.ThrowIfNull(request.ResolutionOrder);
        ArgumentNullException.ThrowIfNull(request.Contents);

        var validation = ValidateOrder(
            request.SnapshotId,
            request.RequestedAudience,
            request.Audiences,
            request.ResolutionOrder);
        if (!validation.IsSuccess)
            return Result<AudienceResolutionResult>.Failure(validation.Error!);

        var candidates = request.ResolutionOrder
            .Where(resolution => resolution.SnapshotId == request.SnapshotId
                && resolution.RequestedAudienceId == request.RequestedAudience)
            .OrderBy(resolution => resolution.Priority)
            .ToArray();

        var match = FindFirstContentMatch(request, candidates);
        if (match.Candidate is null || match.Content is null)
            return Result<AudienceResolutionResult>.Success(
                CreateUnavailableResult(request.RequestedAudience, isConfigured: candidates.Length > 0));

        return Result<AudienceResolutionResult>.Success(CreateResolvedResult(request.RequestedAudience, match.Candidate, match.Content));
    }

    /// <summary>
    /// Prueft die angefragte Zielgruppe und ihre vollstaendige Resolution Order mit denselben
    /// Regeln und Fehlercodes wie <see cref="Resolve"/> - unabhaengig vom Content eines
    /// konkreten Node, zum Beispiel fuer Suchanfragen.
    /// </summary>
    public static Result<bool> ValidateOrder(
        SnapshotId snapshotId,
        AudienceId requestedAudience,
        IEnumerable<Audience> audiences,
        IEnumerable<AudienceResolution> resolutions)
    {
        ArgumentNullException.ThrowIfNull(audiences);
        ArgumentNullException.ThrowIfNull(resolutions);

        var snapshotAudiences = audiences.Where(audience => audience.SnapshotId == snapshotId).ToArray();
        var requestedAudienceError = ValidateRequestedAudience(requestedAudience, snapshotAudiences);
        if (requestedAudienceError is not null)
            return Result<bool>.Failure(requestedAudienceError);

        var candidates = resolutions
            .Where(resolution => resolution.SnapshotId == snapshotId
                && resolution.RequestedAudienceId == requestedAudience)
            .OrderBy(resolution => resolution.Priority)
            .ToArray();

        var candidateError = ValidateCandidates(candidates, snapshotAudiences);
        return candidateError is not null
            ? Result<bool>.Failure(candidateError)
            : Result<bool>.Success(true);
    }

    private static DomainError? ValidateRequestedAudience(AudienceId requestedAudienceId, IReadOnlyCollection<Audience> audiences)
    {
        var requestedAudience = audiences.SingleOrDefault(audience => audience.AudienceId == requestedAudienceId);
        if (requestedAudience is null)
            return CreateError(
                AudienceResolutionErrorCodes.RequestedAudienceNotFound,
                "Die angefragte Zielgruppe ist im Snapshot nicht vorhanden.",
                AudienceResolutionErrorCodes.RequestedAudienceIdDetail,
                requestedAudienceId.ToString());

        return requestedAudience.IsDeleted
            ? CreateError(
                AudienceResolutionErrorCodes.RequestedAudienceDeleted,
                "Die angefragte Zielgruppe ist gelöscht.",
                AudienceResolutionErrorCodes.RequestedAudienceIdDetail,
                requestedAudienceId.ToString())
            : null;
    }

    private static (AudienceResolution? Candidate, NodeContent? Content) FindFirstContentMatch(
        AudienceResolutionRequest request,
        IEnumerable<AudienceResolution> candidates)
    {
        var contents = request.Contents.Where(content => content.SnapshotId == request.SnapshotId
            && content.NodeId == request.NodeId
            && !content.IsDeleted);

        foreach (var candidate in candidates)
        {
            var content = contents.FirstOrDefault(item => item.AudienceId == candidate.CandidateAudienceId);
            if (content is not null)
                return (candidate, content);
        }

        return (null, null);
    }

    private static AudienceResolutionResult CreateUnavailableResult(AudienceId requestedAudience, bool isConfigured) =>
        new(requestedAudience, null, Availability.None, false, isConfigured, null);

    private static AudienceResolutionResult CreateResolvedResult(
        AudienceId requestedAudience,
        AudienceResolution candidate,
        NodeContent content)
    {
        var fallbackUsed = candidate.CandidateAudienceId != requestedAudience;
        return new AudienceResolutionResult(
            requestedAudience,
            candidate.CandidateAudienceId,
            fallbackUsed ? Availability.Fallback : Availability.Explicit,
            fallbackUsed,
            true,
            new ResolvedContentMetadata(
                content.NodeId,
                content.AudienceId,
                content.ContentRevisionId,
                content.ContentMode,
                content.ContentMd.Length));
    }

    private static DomainError? ValidateCandidates(
        IReadOnlyList<AudienceResolution> candidates,
        IReadOnlyCollection<Audience> audiences)
    {
        var candidateAudienceIds = new HashSet<AudienceId>();
        var priorities = new HashSet<int>();
        foreach (var candidate in candidates)
        {
            if (candidate.Priority <= 0)
                return CreateError(
                    AudienceResolutionErrorCodes.InvalidPriority,
                    "Die Priorität eines Rollenkandidaten muss positiv sein.",
                    AudienceResolutionErrorCodes.PriorityDetail,
                    candidate.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture));

            if (!candidateAudienceIds.Add(candidate.CandidateAudienceId))
                return CreateError(
                    AudienceResolutionErrorCodes.DuplicateCandidateAudience,
                    "Eine Zielgruppe darf innerhalb einer Auflösungsreihenfolge nur einmal vorkommen.",
                    AudienceResolutionErrorCodes.CandidateAudienceIdDetail,
                    candidate.CandidateAudienceId.ToString());

            if (!priorities.Add(candidate.Priority))
                return CreateError(
                    AudienceResolutionErrorCodes.DuplicatePriority,
                    "Die Kandidatenpriorität muss innerhalb einer Auflösungsreihenfolge eindeutig sein.",
                    AudienceResolutionErrorCodes.PriorityDetail,
                    candidate.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture));

            var audience = audiences.SingleOrDefault(item => item.AudienceId == candidate.CandidateAudienceId);
            if (audience is null)
                return CreateError(
                    AudienceResolutionErrorCodes.CandidateAudienceNotFound,
                    "Ein konfigurierter Rollenkandidat ist im Snapshot nicht vorhanden.",
                    AudienceResolutionErrorCodes.CandidateAudienceIdDetail,
                    candidate.CandidateAudienceId.ToString());

            if (audience.IsDeleted)
                return CreateError(
                    AudienceResolutionErrorCodes.CandidateAudienceDeleted,
                    "Ein konfigurierter Rollenkandidat ist gelöscht.",
                    AudienceResolutionErrorCodes.CandidateAudienceIdDetail,
                    candidate.CandidateAudienceId.ToString());
        }

        return null;
    }

    private static DomainError CreateError(string code, string message, string detailName, string detailValue) =>
        new(code, message, new Dictionary<string, string>
        {
            [detailName] = detailValue
        });
}
