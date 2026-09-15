using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.Mutations.Roles;

/// <summary>
/// Transportneutrale Orchestrierung der Rollen-Mutations-Use-Cases (create/update/delete Role,
/// set_role_resolution). Jede Mutation verlangt eine offene KnowHowTo-AI-Transaction.
/// </summary>
public sealed class RoleMutationService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IContentRepository _contentRepository;
    private readonly IDependencyRepository _dependencyRepository;
    private readonly IRoleMutationRepository _roleMutationRepository;

    public RoleMutationService(
        ITransactionRepository transactionRepository,
        IRoleRepository roleRepository,
        IContentRepository contentRepository,
        IDependencyRepository dependencyRepository,
        IRoleMutationRepository roleMutationRepository)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _contentRepository = contentRepository ?? throw new ArgumentNullException(nameof(contentRepository));
        _dependencyRepository = dependencyRepository ?? throw new ArgumentNullException(nameof(dependencyRepository));
        _roleMutationRepository = roleMutationRepository ?? throw new ArgumentNullException(nameof(roleMutationRepository));
    }

    /// <summary>
    /// Legt eine neue Rolle im Working Snapshot an.
    /// Der RoleId-Wert ist der normalisierte Name (Slug) der Rolle.
    /// </summary>
    public async Task<Result<Role>> CreateRoleAsync(
        TransactionId transactionId,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Role>.Failure(new DomainError(
                RoleMutationErrorCodes.RoleNameRequired,
                "Der Rollenname darf nicht leer oder nur Whitespace sein."));

        var txResult = await GetOpenTransactionAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (!txResult.IsSuccess)
            return Result<Role>.Failure(txResult.Error!);

        var snapshotId = txResult.Value!.WorkingSnapshotId;
        var existingRoles = await _roleRepository.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        var roleId = new RoleId(name.Trim());
        if (existingRoles.Any(r => !r.IsDeleted && r.RoleId == roleId))
            return Result<Role>.Failure(new DomainError(
                RoleMutationErrorCodes.RoleInUse,
                "Eine Rolle mit diesem Namen existiert bereits in diesem Snapshot.",
                new Dictionary<string, string> { [RoleMutationErrorCodes.RoleIdDetail] = roleId.ToString() }));

        var newRole = new Role(snapshotId, roleId, name.Trim(), description?.Trim(), IsDeleted: false);
        var updatedRoles = existingRoles.Append(newRole).ToArray();

        await _roleMutationRepository.SaveRolesAsync(snapshotId, Array.AsReadOnly(updatedRoles), cancellationToken).ConfigureAwait(false);
        return Result<Role>.Success(newRole);
    }

    /// <summary>Aktualisiert Name und/oder Beschreibung einer aktiven Rolle.</summary>
    public async Task<Result<Role>> UpdateRoleAsync(
        TransactionId transactionId,
        RoleId roleId,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Role>.Failure(new DomainError(
                RoleMutationErrorCodes.RoleNameRequired,
                "Der Rollenname darf nicht leer oder nur Whitespace sein."));

        var txResult = await GetOpenTransactionAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (!txResult.IsSuccess)
            return Result<Role>.Failure(txResult.Error!);

        var snapshotId = txResult.Value!.WorkingSnapshotId;
        var existingRoles = await _roleRepository.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        var role = existingRoles.FirstOrDefault(r => !r.IsDeleted && r.RoleId == roleId);
        if (role is null)
            return Result<Role>.Failure(CreateRoleNotFoundError(roleId));

        var updatedRole = role with { Name = name.Trim(), Description = description?.Trim() };
        var updatedRoles = existingRoles.Select(r => r.RoleId == roleId ? updatedRole : r).ToArray();

        await _roleMutationRepository.SaveRolesAsync(snapshotId, Array.AsReadOnly(updatedRoles), cancellationToken).ConfigureAwait(false);
        return Result<Role>.Success(updatedRole);
    }

    /// <summary>
    /// Markiert eine Rolle als gelöscht (Tombstone). Schlägt mit <c>RoleInUse</c> fehl, wenn
    /// noch aktive Content-, Dependency- oder Resolution-Referenzen auf diese Rolle zeigen.
    /// </summary>
    public async Task<Result<Role>> DeleteRoleAsync(
        TransactionId transactionId,
        RoleId roleId,
        CancellationToken cancellationToken = default)
    {
        var txResult = await GetOpenTransactionAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (!txResult.IsSuccess)
            return Result<Role>.Failure(txResult.Error!);

        var snapshotId = txResult.Value!.WorkingSnapshotId;
        var existingRoles = await _roleRepository.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        var role = existingRoles.FirstOrDefault(r => !r.IsDeleted && r.RoleId == roleId);
        if (role is null)
            return Result<Role>.Failure(CreateRoleNotFoundError(roleId));

        var blockingError = await CheckBlockingReferencesAsync(snapshotId, roleId, cancellationToken).ConfigureAwait(false);
        if (blockingError is not null)
            return Result<Role>.Failure(blockingError);

        var deletedRole = role with { IsDeleted = true };
        var updatedRoles = existingRoles.Select(r => r.RoleId == roleId ? deletedRole : r).ToArray();

        await _roleMutationRepository.SaveRolesAsync(snapshotId, Array.AsReadOnly(updatedRoles), cancellationToken).ConfigureAwait(false);
        return Result<Role>.Success(deletedRole);
    }

    /// <summary>
    /// Ersetzt die vollständige Kandidatenliste für eine angefragte Rolle atomar.
    /// Keine schrittweisen Zwischenzustände; leere <paramref name="candidateRoleIds"/> entfernt alle Einträge.
    /// </summary>
    public async Task<Result<IReadOnlyList<RoleResolution>>> SetRoleResolutionAsync(
        TransactionId transactionId,
        RoleId requestedRoleId,
        IEnumerable<RoleId> candidateRoleIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidateRoleIds);

        var txResult = await GetOpenTransactionAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (!txResult.IsSuccess)
            return Result<IReadOnlyList<RoleResolution>>.Failure(txResult.Error!);

        var snapshotId = txResult.Value!.WorkingSnapshotId;
        var existingRoles = await _roleRepository.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        // Angefragte Rolle muss aktiv existieren
        if (!existingRoles.Any(r => !r.IsDeleted && r.RoleId == requestedRoleId))
            return Result<IReadOnlyList<RoleResolution>>.Failure(CreateRoleNotFoundError(requestedRoleId));

        var candidates = candidateRoleIds.ToArray();

        // Kandidaten validieren: müssen aktiv existieren und dürfen nicht doppelt vorkommen
        var seen = new HashSet<RoleId>();
        for (var i = 0; i < candidates.Length; i++)
        {
            var candidate = candidates[i];
            if (!existingRoles.Any(r => !r.IsDeleted && r.RoleId == candidate))
                return Result<IReadOnlyList<RoleResolution>>.Failure(new DomainError(
                    RoleResolutionErrorCodes.CandidateRoleNotFound,
                    "Eine Kandidaten-Rolle existiert nicht im Snapshot.",
                    new Dictionary<string, string> { [RoleResolutionErrorCodes.CandidateRoleIdDetail] = candidate.ToString() }));

            if (!seen.Add(candidate))
                return Result<IReadOnlyList<RoleResolution>>.Failure(new DomainError(
                    RoleResolutionErrorCodes.DuplicateCandidateRole,
                    "Eine Kandidaten-Rolle kommt mehr als einmal in der Resolution Order vor.",
                    new Dictionary<string, string> { [RoleResolutionErrorCodes.CandidateRoleIdDetail] = candidate.ToString() }));
        }

        // Neue Resolution Orders: Priority beginnt bei 1
        var newResolutions = candidates
            .Select((candidate, index) => new RoleResolution(snapshotId, requestedRoleId, candidate, index + 1))
            .ToArray();

        await _roleMutationRepository.SaveResolutionsAsync(
            snapshotId, requestedRoleId, Array.AsReadOnly(newResolutions), cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<RoleResolution>>.Success(Array.AsReadOnly(newResolutions));
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private async Task<DomainError?> CheckBlockingReferencesAsync(
        SnapshotId snapshotId,
        RoleId roleId,
        CancellationToken cancellationToken)
    {
        var contents = await _contentRepository.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var dependencies = await _dependencyRepository.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var resolutions = await _roleRepository.ListResolutionsBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        var blockingContentCount = contents.Count(c => !c.IsDeleted && c.RoleId == roleId);
        var blockingDependencyCount = dependencies.Count(d =>
            d.SnapshotId == snapshotId && (d.TargetRoleId == roleId || d.SourceRoleId == roleId));
        var blockingResolutionCount = resolutions.Count(r =>
            r.SnapshotId == snapshotId && (r.RequestedRoleId == roleId || r.CandidateRoleId == roleId));

        if (blockingContentCount == 0 && blockingDependencyCount == 0 && blockingResolutionCount == 0)
            return null;

        return new DomainError(
            RoleMutationErrorCodes.RoleInUse,
            "Die Rolle wird noch von Content, Dependencies oder Resolution Orders referenziert.",
            new Dictionary<string, string>
            {
                [RoleMutationErrorCodes.RoleIdDetail] = roleId.ToString(),
                [RoleMutationErrorCodes.BlockingContentCountDetail] = blockingContentCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [RoleMutationErrorCodes.BlockingDependencyCountDetail] = blockingDependencyCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [RoleMutationErrorCodes.BlockingResolutionCountDetail] = blockingResolutionCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
    }

    private async Task<Result<Domain.Versioning.KnowledgeTransaction>> GetOpenTransactionAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository.FindAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (transaction is null)
            return Result<Domain.Versioning.KnowledgeTransaction>.Failure(new DomainError(
                Transactions.TransactionValidationErrorCodes.TransactionNotFound,
                "Die angefragte Transaction existiert nicht.",
                new Dictionary<string, string>
                {
                    [Transactions.TransactionValidationErrorCodes.TransactionIdDetail] = transactionId.ToString()
                }));

        if (transaction.State != TransactionState.Open)
            return Result<Domain.Versioning.KnowledgeTransaction>.Failure(new DomainError(
                Transactions.TransactionValidationErrorCodes.TransactionClosed,
                "Die Transaction ist nicht mehr offen.",
                new Dictionary<string, string>
                {
                    [Transactions.TransactionValidationErrorCodes.TransactionIdDetail] = transactionId.ToString()
                }));

        return Result<Domain.Versioning.KnowledgeTransaction>.Success(transaction);
    }

    private static DomainError CreateRoleNotFoundError(RoleId roleId) =>
        new(
            RoleMutationErrorCodes.RoleNotFound,
            "Die angefragte Rolle existiert nicht.",
            new Dictionary<string, string> { [RoleMutationErrorCodes.RoleIdDetail] = roleId.ToString() });
}
