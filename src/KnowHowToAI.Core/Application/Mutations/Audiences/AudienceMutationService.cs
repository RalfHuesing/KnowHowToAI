using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Application.Mutations.Audiences;

/// <summary>Orchestriert Zielgruppen und vollständige Resolution Orders innerhalb einer offenen Transaction.</summary>
public sealed class AudienceMutationService(IAudienceMutationRepository repository)
{
    private readonly IAudienceMutationRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<Result<Audience>> CreateAudienceAsync(TransactionId transactionId, string name, string? description, long expectedChangeVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Audience>.Failure(AudienceNameRequiredError());
        var result = await CreateAudienceMutationAsync(transactionId, name, description, expectedChangeVersion, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Result<Audience>.Success(result.Value!.Audience) : Result<Audience>.Failure(result.Error!, result.Warnings);
    }

    public async Task<Result<Audience>> UpdateAudienceAsync(
        TransactionId transactionId,
        UpdateAudienceMutationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<Audience>.Failure(AudienceNameRequiredError());
        var result = await UpdateAudienceMutationAsync(transactionId, request, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Result<Audience>.Success(result.Value!.Audience) : Result<Audience>.Failure(result.Error!, result.Warnings);
    }

    public async Task<Result<Audience>> DeleteAudienceAsync(TransactionId transactionId, AudienceId audienceId, long expectedChangeVersion, CancellationToken cancellationToken = default)
    {
        var result = await DeleteAudienceMutationAsync(transactionId, audienceId, expectedChangeVersion, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Result<Audience>.Success(result.Value!.Audience) : Result<Audience>.Failure(result.Error!, result.Warnings);
    }

    public Task<Result<AudienceMutationResult>> CreateAudienceMutationAsync(
        TransactionId transactionId,
        string name,
        string? description,
        long expectedChangeVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult(Result<AudienceMutationResult>.Failure(AudienceNameRequiredError()));
        return ExecuteAudienceMutationAsync(transactionId, state => CreateAudienceDecision(state, name, description), expectedChangeVersion, cancellationToken);
    }

    public Task<Result<AudienceMutationResult>> UpdateAudienceMutationAsync(
        TransactionId transactionId,
        UpdateAudienceMutationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name))
            return Task.FromResult(Result<AudienceMutationResult>.Failure(AudienceNameRequiredError()));
        return ExecuteAudienceMutationAsync(transactionId, state => UpdateAudienceDecision(state, request.AudienceId, request.Name, request.Description), request.ExpectedChangeVersion, cancellationToken);
    }

    public Task<Result<AudienceMutationResult>> DeleteAudienceMutationAsync(
        TransactionId transactionId,
        AudienceId audienceId,
        long expectedChangeVersion,
        CancellationToken cancellationToken = default) =>
        ExecuteAudienceMutationAsync(transactionId, state => DeleteAudienceDecision(state, audienceId), expectedChangeVersion, cancellationToken);

    public async Task<Result<IReadOnlyList<AudienceResolution>>> SetAudienceResolutionAsync(
        TransactionId transactionId,
        AudienceId requestedAudienceId,
        IEnumerable<AudienceId> candidateAudienceIds,
        long expectedChangeVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidateAudienceIds);
        var candidates = candidateAudienceIds.ToArray();
        var result = await _repository.ExecuteAsync(
            transactionId,
            state => SetResolutionDecision(state, requestedAudienceId, candidates),
            expectedChangeVersion,
            cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? Result<IReadOnlyList<AudienceResolution>>.Success(result.Value!.Value)
            : Result<IReadOnlyList<AudienceResolution>>.Failure(result.Error!);
    }

    public async Task<Result<AudienceResolutionMutationResult>> SetAudienceResolutionMutationAsync(
        TransactionId transactionId,
        AudienceId requestedAudienceId,
        IEnumerable<AudienceId> candidateAudienceIds,
        long expectedChangeVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidateAudienceIds);
        var candidates = candidateAudienceIds.ToArray();
        var result = await _repository.ExecuteAsync(
            transactionId,
            state => SetResolutionDecision(state, requestedAudienceId, candidates),
            expectedChangeVersion,
            cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return Result<AudienceResolutionMutationResult>.Failure(result.Error!, result.Warnings);

        var execution = result.Value!;
        return Result<AudienceResolutionMutationResult>.Success(
            new AudienceResolutionMutationResult(requestedAudienceId, execution.Value, execution.SnapshotId, execution.ChangeVersion),
            result.Warnings);
    }

    private async Task<Result<AudienceMutationResult>> ExecuteAudienceMutationAsync(
        TransactionId transactionId,
        Func<WorkingAudienceMutationState, Result<WorkingAudienceMutationDecision<Audience>>> mutate,
        long expectedChangeVersion,
        CancellationToken cancellationToken)
    {
        var result = await _repository.ExecuteAsync(transactionId, mutate, expectedChangeVersion, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return Result<AudienceMutationResult>.Failure(result.Error!, result.Warnings);

        var execution = result.Value!;
        return Result<AudienceMutationResult>.Success(
            new AudienceMutationResult(execution.Value, execution.SnapshotId, execution.ChangeVersion),
            result.Warnings);
    }

    private static Result<WorkingAudienceMutationDecision<Audience>> CreateAudienceDecision(WorkingAudienceMutationState state, string name, string? description)
    {
        var normalizedName = name.Trim();
        var audienceId = new AudienceId(normalizedName);
        var existingAudience = state.Audiences.SingleOrDefault(audience => audience.AudienceId == audienceId);
        if (existingAudience is { IsDeleted: false })
            return Result<WorkingAudienceMutationDecision<Audience>>.Failure(AudienceInUseError(audienceId));

        var duplicate = state.Audiences.FirstOrDefault(candidate =>
            !candidate.IsDeleted
            && string.Equals(candidate.Name, normalizedName, StringComparison.Ordinal));
        if (duplicate is not null)
            return Result<WorkingAudienceMutationDecision<Audience>>.Failure(AudienceInUseError(duplicate.AudienceId));

        var audience = new Audience(state.SnapshotId, audienceId, normalizedName, description?.Trim(), IsDeleted: false);
        var audiences = existingAudience is null
            ? state.Audiences.Append(audience).ToArray()
            : state.Audiences.Select(candidate => candidate.AudienceId == audienceId ? audience : candidate).ToArray();
        return Result<WorkingAudienceMutationDecision<Audience>>.Success(
            new WorkingAudienceMutationDecision<Audience>(audience, state with { Audiences = audiences }));
    }

    private static Result<WorkingAudienceMutationDecision<Audience>> UpdateAudienceDecision(WorkingAudienceMutationState state, AudienceId audienceId, string name, string? description)
    {
        var audience = FindActiveAudience(state, audienceId);
        if (audience is null)
            return Result<WorkingAudienceMutationDecision<Audience>>.Failure(AudienceNotFoundError(audienceId));

        var normalizedName = name.Trim();
        var duplicate = state.Audiences.FirstOrDefault(candidate =>
            !candidate.IsDeleted
            && candidate.AudienceId != audienceId
            && string.Equals(candidate.Name, normalizedName, StringComparison.Ordinal));
        if (duplicate is not null)
            return Result<WorkingAudienceMutationDecision<Audience>>.Failure(AudienceInUseError(duplicate.AudienceId));

        var updatedAudience = audience with { Name = normalizedName, Description = description?.Trim() };
        return Result<WorkingAudienceMutationDecision<Audience>>.Success(new WorkingAudienceMutationDecision<Audience>(
            updatedAudience,
            state with { Audiences = state.Audiences.Select(candidate => candidate.AudienceId == audienceId ? updatedAudience : candidate).ToArray() }));
    }

    private static Result<WorkingAudienceMutationDecision<Audience>> DeleteAudienceDecision(WorkingAudienceMutationState state, AudienceId audienceId)
    {
        var audience = FindActiveAudience(state, audienceId);
        if (audience is null)
            return Result<WorkingAudienceMutationDecision<Audience>>.Failure(AudienceNotFoundError(audienceId));

        var blockingError = FindBlockingReferenceError(state, audienceId);
        if (blockingError is not null)
            return Result<WorkingAudienceMutationDecision<Audience>>.Failure(blockingError);

        var deletedAudience = audience with { IsDeleted = true };
        return Result<WorkingAudienceMutationDecision<Audience>>.Success(new WorkingAudienceMutationDecision<Audience>(
            deletedAudience,
            state with { Audiences = state.Audiences.Select(candidate => candidate.AudienceId == audienceId ? deletedAudience : candidate).ToArray() }));
    }

    private static Result<WorkingAudienceMutationDecision<IReadOnlyList<AudienceResolution>>> SetResolutionDecision(
        WorkingAudienceMutationState state,
        AudienceId requestedAudienceId,
        IReadOnlyList<AudienceId> candidateAudienceIds)
    {
        if (FindActiveAudience(state, requestedAudienceId) is null)
            return Result<WorkingAudienceMutationDecision<IReadOnlyList<AudienceResolution>>>.Failure(AudienceNotFoundError(requestedAudienceId));

        var seen = new HashSet<AudienceId>();
        foreach (var candidateAudienceId in candidateAudienceIds)
        {
            if (FindActiveAudience(state, candidateAudienceId) is null)
            {
                return Result<WorkingAudienceMutationDecision<IReadOnlyList<AudienceResolution>>>.Failure(new DomainError(
                    AudienceResolutionErrorCodes.CandidateAudienceNotFound,
                    "Eine Kandidaten-Zielgruppe existiert nicht im Snapshot.",
                    new Dictionary<string, string> { [AudienceResolutionErrorCodes.CandidateAudienceIdDetail] = candidateAudienceId.ToString() }));
            }

            if (!seen.Add(candidateAudienceId))
            {
                return Result<WorkingAudienceMutationDecision<IReadOnlyList<AudienceResolution>>>.Failure(new DomainError(
                    AudienceResolutionErrorCodes.DuplicateCandidateAudience,
                    "Eine Kandidaten-Zielgruppe kommt mehr als einmal in der Resolution Order vor.",
                    new Dictionary<string, string> { [AudienceResolutionErrorCodes.CandidateAudienceIdDetail] = candidateAudienceId.ToString() }));
            }
        }

        var resolutions = candidateAudienceIds.Select((candidateAudienceId, index) =>
            new AudienceResolution(state.SnapshotId, requestedAudienceId, candidateAudienceId, index + 1)).ToArray();
        var updatedResolutions = state.Resolutions.Where(resolution => resolution.RequestedAudienceId != requestedAudienceId)
            .Concat(resolutions).ToArray();
        return Result<WorkingAudienceMutationDecision<IReadOnlyList<AudienceResolution>>>.Success(
            new WorkingAudienceMutationDecision<IReadOnlyList<AudienceResolution>>(
                Array.AsReadOnly(resolutions),
                state with { Resolutions = updatedResolutions }));
    }

    private static Audience? FindActiveAudience(WorkingAudienceMutationState state, AudienceId audienceId) =>
        state.Audiences.SingleOrDefault(audience => !audience.IsDeleted && audience.AudienceId == audienceId);

    private static DomainError? FindBlockingReferenceError(WorkingAudienceMutationState state, AudienceId audienceId)
    {
        var contentCount = state.Contents.Count(content => !content.IsDeleted && content.AudienceId == audienceId);
        var dependencyCount = state.Dependencies.Count(dependency => dependency.TargetAudienceId == audienceId || dependency.SourceAudienceId == audienceId);
        var resolutionCount = state.Resolutions.Count(resolution => resolution.RequestedAudienceId == audienceId || resolution.CandidateAudienceId == audienceId);
        if (contentCount == 0 && dependencyCount == 0 && resolutionCount == 0)
            return null;

        return new DomainError(
            AudienceMutationErrorCodes.AudienceInUse,
            "Die Zielgruppe wird noch von Content, Dependencies oder Resolution Orders referenziert.",
            new Dictionary<string, string>
            {
                [AudienceMutationErrorCodes.AudienceIdDetail] = audienceId.ToString(),
                [AudienceMutationErrorCodes.BlockingContentCountDetail] = contentCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [AudienceMutationErrorCodes.BlockingDependencyCountDetail] = dependencyCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [AudienceMutationErrorCodes.BlockingResolutionCountDetail] = resolutionCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
    }

    private static DomainError AudienceNameRequiredError() => new(AudienceMutationErrorCodes.AudienceNameRequired, "Der Zielgruppenname darf nicht leer oder nur Whitespace sein.");

    private static DomainError AudienceInUseError(AudienceId audienceId) => new(
        AudienceMutationErrorCodes.AudienceInUse,
        "Eine Zielgruppe mit diesem Namen existiert bereits in diesem Snapshot.",
        new Dictionary<string, string> { [AudienceMutationErrorCodes.AudienceIdDetail] = audienceId.ToString() });

    private static DomainError AudienceNotFoundError(AudienceId audienceId) => new(
        AudienceMutationErrorCodes.AudienceNotFound,
        "Die angefragte Zielgruppe existiert nicht.",
        new Dictionary<string, string> { [AudienceMutationErrorCodes.AudienceIdDetail] = audienceId.ToString() });
}
