using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.Mutations.Roles;

/// <summary>Orchestriert Rollen und vollständige Resolution Orders innerhalb einer offenen Transaction.</summary>
public sealed class RoleMutationService(IRoleMutationRepository repository)
{
    private readonly IRoleMutationRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<Result<Role>> CreateRoleAsync(TransactionId transactionId, string name, string? description, long expectedChangeVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Role>.Failure(RoleNameRequiredError());
        var result = await CreateRoleMutationAsync(transactionId, name, description, expectedChangeVersion, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Result<Role>.Success(result.Value!.Role) : Result<Role>.Failure(result.Error!, result.Warnings);
    }

    public async Task<Result<Role>> UpdateRoleAsync(
        TransactionId transactionId,
        UpdateRoleMutationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<Role>.Failure(RoleNameRequiredError());
        var result = await UpdateRoleMutationAsync(transactionId, request, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Result<Role>.Success(result.Value!.Role) : Result<Role>.Failure(result.Error!, result.Warnings);
    }

    public async Task<Result<Role>> DeleteRoleAsync(TransactionId transactionId, RoleId roleId, long expectedChangeVersion, CancellationToken cancellationToken = default)
    {
        var result = await DeleteRoleMutationAsync(transactionId, roleId, expectedChangeVersion, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Result<Role>.Success(result.Value!.Role) : Result<Role>.Failure(result.Error!, result.Warnings);
    }

    public Task<Result<RoleMutationResult>> CreateRoleMutationAsync(
        TransactionId transactionId,
        string name,
        string? description,
        long expectedChangeVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Task.FromResult(Result<RoleMutationResult>.Failure(RoleNameRequiredError()));
        return ExecuteRoleMutationAsync(transactionId, state => CreateRoleDecision(state, name, description), expectedChangeVersion, cancellationToken);
    }

    public Task<Result<RoleMutationResult>> UpdateRoleMutationAsync(
        TransactionId transactionId,
        UpdateRoleMutationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name))
            return Task.FromResult(Result<RoleMutationResult>.Failure(RoleNameRequiredError()));
        return ExecuteRoleMutationAsync(transactionId, state => UpdateRoleDecision(state, request.RoleId, request.Name, request.Description), request.ExpectedChangeVersion, cancellationToken);
    }

    public Task<Result<RoleMutationResult>> DeleteRoleMutationAsync(
        TransactionId transactionId,
        RoleId roleId,
        long expectedChangeVersion,
        CancellationToken cancellationToken = default) =>
        ExecuteRoleMutationAsync(transactionId, state => DeleteRoleDecision(state, roleId), expectedChangeVersion, cancellationToken);

    public async Task<Result<IReadOnlyList<RoleResolution>>> SetRoleResolutionAsync(
        TransactionId transactionId,
        RoleId requestedRoleId,
        IEnumerable<RoleId> candidateRoleIds,
        long expectedChangeVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidateRoleIds);
        var candidates = candidateRoleIds.ToArray();
        var result = await _repository.ExecuteAsync(
            transactionId,
            state => SetResolutionDecision(state, requestedRoleId, candidates),
            expectedChangeVersion,
            cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? Result<IReadOnlyList<RoleResolution>>.Success(result.Value!.Value)
            : Result<IReadOnlyList<RoleResolution>>.Failure(result.Error!);
    }

    public async Task<Result<RoleResolutionMutationResult>> SetRoleResolutionMutationAsync(
        TransactionId transactionId,
        RoleId requestedRoleId,
        IEnumerable<RoleId> candidateRoleIds,
        long expectedChangeVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidateRoleIds);
        var candidates = candidateRoleIds.ToArray();
        var result = await _repository.ExecuteAsync(
            transactionId,
            state => SetResolutionDecision(state, requestedRoleId, candidates),
            expectedChangeVersion,
            cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return Result<RoleResolutionMutationResult>.Failure(result.Error!, result.Warnings);

        var execution = result.Value!;
        return Result<RoleResolutionMutationResult>.Success(
            new RoleResolutionMutationResult(requestedRoleId, execution.Value, execution.SnapshotId, execution.ChangeVersion),
            result.Warnings);
    }

    private async Task<Result<RoleMutationResult>> ExecuteRoleMutationAsync(
        TransactionId transactionId,
        Func<WorkingRoleMutationState, Result<WorkingRoleMutationDecision<Role>>> mutate,
        long expectedChangeVersion,
        CancellationToken cancellationToken)
    {
        var result = await _repository.ExecuteAsync(transactionId, mutate, expectedChangeVersion, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return Result<RoleMutationResult>.Failure(result.Error!, result.Warnings);

        var execution = result.Value!;
        return Result<RoleMutationResult>.Success(
            new RoleMutationResult(execution.Value, execution.SnapshotId, execution.ChangeVersion),
            result.Warnings);
    }

    private static Result<WorkingRoleMutationDecision<Role>> CreateRoleDecision(WorkingRoleMutationState state, string name, string? description)
    {
        var normalizedName = name.Trim();
        var roleId = new RoleId(normalizedName);
        var existingRole = state.Roles.SingleOrDefault(role => role.RoleId == roleId);
        if (existingRole is { IsDeleted: false })
            return Result<WorkingRoleMutationDecision<Role>>.Failure(RoleInUseError(roleId));

        var duplicate = state.Roles.FirstOrDefault(candidate =>
            !candidate.IsDeleted
            && string.Equals(candidate.Name, normalizedName, StringComparison.Ordinal));
        if (duplicate is not null)
            return Result<WorkingRoleMutationDecision<Role>>.Failure(RoleInUseError(duplicate.RoleId));

        var role = new Role(state.SnapshotId, roleId, normalizedName, description?.Trim(), IsDeleted: false);
        var roles = existingRole is null
            ? state.Roles.Append(role).ToArray()
            : state.Roles.Select(candidate => candidate.RoleId == roleId ? role : candidate).ToArray();
        return Result<WorkingRoleMutationDecision<Role>>.Success(
            new WorkingRoleMutationDecision<Role>(role, state with { Roles = roles }));
    }

    private static Result<WorkingRoleMutationDecision<Role>> UpdateRoleDecision(WorkingRoleMutationState state, RoleId roleId, string name, string? description)
    {
        var role = FindActiveRole(state, roleId);
        if (role is null)
            return Result<WorkingRoleMutationDecision<Role>>.Failure(RoleNotFoundError(roleId));

        var normalizedName = name.Trim();
        var duplicate = state.Roles.FirstOrDefault(candidate =>
            !candidate.IsDeleted
            && candidate.RoleId != roleId
            && string.Equals(candidate.Name, normalizedName, StringComparison.Ordinal));
        if (duplicate is not null)
            return Result<WorkingRoleMutationDecision<Role>>.Failure(RoleInUseError(duplicate.RoleId));

        var updatedRole = role with { Name = normalizedName, Description = description?.Trim() };
        return Result<WorkingRoleMutationDecision<Role>>.Success(new WorkingRoleMutationDecision<Role>(
            updatedRole,
            state with { Roles = state.Roles.Select(candidate => candidate.RoleId == roleId ? updatedRole : candidate).ToArray() }));
    }

    private static Result<WorkingRoleMutationDecision<Role>> DeleteRoleDecision(WorkingRoleMutationState state, RoleId roleId)
    {
        var role = FindActiveRole(state, roleId);
        if (role is null)
            return Result<WorkingRoleMutationDecision<Role>>.Failure(RoleNotFoundError(roleId));

        var blockingError = FindBlockingReferenceError(state, roleId);
        if (blockingError is not null)
            return Result<WorkingRoleMutationDecision<Role>>.Failure(blockingError);

        var deletedRole = role with { IsDeleted = true };
        return Result<WorkingRoleMutationDecision<Role>>.Success(new WorkingRoleMutationDecision<Role>(
            deletedRole,
            state with { Roles = state.Roles.Select(candidate => candidate.RoleId == roleId ? deletedRole : candidate).ToArray() }));
    }

    private static Result<WorkingRoleMutationDecision<IReadOnlyList<RoleResolution>>> SetResolutionDecision(
        WorkingRoleMutationState state,
        RoleId requestedRoleId,
        IReadOnlyList<RoleId> candidateRoleIds)
    {
        if (FindActiveRole(state, requestedRoleId) is null)
            return Result<WorkingRoleMutationDecision<IReadOnlyList<RoleResolution>>>.Failure(RoleNotFoundError(requestedRoleId));

        var seen = new HashSet<RoleId>();
        foreach (var candidateRoleId in candidateRoleIds)
        {
            if (FindActiveRole(state, candidateRoleId) is null)
            {
                return Result<WorkingRoleMutationDecision<IReadOnlyList<RoleResolution>>>.Failure(new DomainError(
                    RoleResolutionErrorCodes.CandidateRoleNotFound,
                    "Eine Kandidaten-Rolle existiert nicht im Snapshot.",
                    new Dictionary<string, string> { [RoleResolutionErrorCodes.CandidateRoleIdDetail] = candidateRoleId.ToString() }));
            }

            if (!seen.Add(candidateRoleId))
            {
                return Result<WorkingRoleMutationDecision<IReadOnlyList<RoleResolution>>>.Failure(new DomainError(
                    RoleResolutionErrorCodes.DuplicateCandidateRole,
                    "Eine Kandidaten-Rolle kommt mehr als einmal in der Resolution Order vor.",
                    new Dictionary<string, string> { [RoleResolutionErrorCodes.CandidateRoleIdDetail] = candidateRoleId.ToString() }));
            }
        }

        var resolutions = candidateRoleIds.Select((candidateRoleId, index) =>
            new RoleResolution(state.SnapshotId, requestedRoleId, candidateRoleId, index + 1)).ToArray();
        var updatedResolutions = state.Resolutions.Where(resolution => resolution.RequestedRoleId != requestedRoleId)
            .Concat(resolutions).ToArray();
        return Result<WorkingRoleMutationDecision<IReadOnlyList<RoleResolution>>>.Success(
            new WorkingRoleMutationDecision<IReadOnlyList<RoleResolution>>(
                Array.AsReadOnly(resolutions),
                state with { Resolutions = updatedResolutions }));
    }

    private static Role? FindActiveRole(WorkingRoleMutationState state, RoleId roleId) =>
        state.Roles.SingleOrDefault(role => !role.IsDeleted && role.RoleId == roleId);

    private static DomainError? FindBlockingReferenceError(WorkingRoleMutationState state, RoleId roleId)
    {
        var contentCount = state.Contents.Count(content => !content.IsDeleted && content.RoleId == roleId);
        var dependencyCount = state.Dependencies.Count(dependency => dependency.TargetRoleId == roleId || dependency.SourceRoleId == roleId);
        var resolutionCount = state.Resolutions.Count(resolution => resolution.RequestedRoleId == roleId || resolution.CandidateRoleId == roleId);
        if (contentCount == 0 && dependencyCount == 0 && resolutionCount == 0)
            return null;

        return new DomainError(
            RoleMutationErrorCodes.RoleInUse,
            "Die Rolle wird noch von Content, Dependencies oder Resolution Orders referenziert.",
            new Dictionary<string, string>
            {
                [RoleMutationErrorCodes.RoleIdDetail] = roleId.ToString(),
                [RoleMutationErrorCodes.BlockingContentCountDetail] = contentCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [RoleMutationErrorCodes.BlockingDependencyCountDetail] = dependencyCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [RoleMutationErrorCodes.BlockingResolutionCountDetail] = resolutionCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
    }

    private static DomainError RoleNameRequiredError() => new(RoleMutationErrorCodes.RoleNameRequired, "Der Rollenname darf nicht leer oder nur Whitespace sein.");

    private static DomainError RoleInUseError(RoleId roleId) => new(
        RoleMutationErrorCodes.RoleInUse,
        "Eine Rolle mit diesem Namen existiert bereits in diesem Snapshot.",
        new Dictionary<string, string> { [RoleMutationErrorCodes.RoleIdDetail] = roleId.ToString() });

    private static DomainError RoleNotFoundError(RoleId roleId) => new(
        RoleMutationErrorCodes.RoleNotFound,
        "Die angefragte Rolle existiert nicht.",
        new Dictionary<string, string> { [RoleMutationErrorCodes.RoleIdDetail] = roleId.ToString() });
}
