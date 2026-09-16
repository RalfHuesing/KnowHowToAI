using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.History;

/// <summary>
/// Transportneutrale Orchestrierung der Release-Use-Cases (create_release, list_releases).
/// create_release benötigt keine KnowHowTo-AI-Transaction: Es ist reine Metadatenverwaltung
/// und ändert keinen versionierten Snapshot-Inhalt (V1-Invariante).
/// </summary>
public sealed class ReleaseService
{
    private readonly SnapshotReadRepositories _historyRepos;
    private readonly IReleaseMutationRepository _releaseMutationRepository;
    private readonly IClock _clock;
    private readonly RetrievalPolicy _retrievalPolicy;
    private readonly ValidationPolicy _validationPolicy;

    public ReleaseService(
        SnapshotReadRepositories historyRepositories,
        IReleaseMutationRepository releaseMutationRepository,
        IClock clock,
        RetrievalPolicy retrievalPolicy,
        ValidationPolicy validationPolicy)
    {
        ArgumentNullException.ThrowIfNull(historyRepositories);
        ArgumentNullException.ThrowIfNull(releaseMutationRepository);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(retrievalPolicy);
        ArgumentNullException.ThrowIfNull(validationPolicy);

        _historyRepos = historyRepositories;
        _releaseMutationRepository = releaseMutationRepository;
        _clock = clock;
        _retrievalPolicy = retrievalPolicy;
        _validationPolicy = validationPolicy;
    }

    /// <summary>
    /// Registriert atomar einen unveränderlichen Release-Verweis auf einen committed Snapshot.
    /// Schlägt mit <c>ReleaseNameRequired</c>, <c>SnapshotNotFound</c>, <c>SnapshotNotCommitted</c> oder
    /// <c>ReleaseNameConflict</c> fehl.
    /// Gibt etwaige Qualitätsbefunde (Warnungen) des referenzierten Snapshots transparent zurück;
    /// stale oder übergroßer Content blockiert das Release nicht (V1-Invariante).
    /// </summary>
    public async Task<Result<CreateReleaseResult>> CreateReleaseAsync(
        string name,
        SnapshotId snapshotId,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<CreateReleaseResult>.Failure(new DomainError(
                ReleaseErrorCodes.ReleaseNameRequired,
                "Der Release-Name darf nicht leer oder nur Whitespace sein."));

        var snapshot = await _historyRepos.Snapshots.FindAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        if (snapshot is null)
            return Result<CreateReleaseResult>.Failure(new DomainError(
                ReleaseErrorCodes.SnapshotNotFound,
                "Der referenzierte Snapshot existiert nicht.",
                new Dictionary<string, string> { [ReleaseErrorCodes.SnapshotIdDetail] = snapshotId.ToString() }));

        if (snapshot.State != SnapshotState.Committed)
            return Result<CreateReleaseResult>.Failure(new DomainError(
                ReleaseErrorCodes.SnapshotNotCommitted,
                "Releases können nur auf committed Snapshots angelegt werden.",
                new Dictionary<string, string> { [ReleaseErrorCodes.SnapshotIdDetail] = snapshotId.ToString() }));

        var findings = await EvaluateFindingsAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var createResult = await _releaseMutationRepository.CreateAsync(
            new CreateReleaseRecord(name.Trim(), snapshotId, _clock.UtcNow, description?.Trim()),
            cancellationToken).ConfigureAwait(false);

        if (!createResult.IsSuccess)
            return Result<CreateReleaseResult>.Failure(createResult.Error!);

        return Result<CreateReleaseResult>.Success(new CreateReleaseResult(createResult.Value!, findings));
    }

    /// <summary>Paginierte, deterministisch sortierte Liste aller Releases.</summary>
    public async Task<Result<ReleasePage>> ListReleasesAsync(
        int? limit,
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        var effectiveLimit = limit is > 0
            ? Math.Min(limit.Value, _retrievalPolicy.MaximumPageSize)
            : _retrievalPolicy.DefaultPageSize;

        long? afterReleaseId = null;
        if (cursor is not null)
        {
            var parsedCursor = ReleaseCursor.TryDecode(cursor);
            if (parsedCursor is null)
                return Result<ReleasePage>.Failure(new DomainError(
                    ReleaseErrorCodes.InvalidCursor,
                    "Der Cursor ist ungültig.",
                    new Dictionary<string, string> { [ReleaseErrorCodes.ReleaseIdDetail] = cursor }));
            afterReleaseId = parsedCursor.AfterReleaseId;
        }

        var releases = await _releaseMutationRepository.ListAsync(effectiveLimit + 1, afterReleaseId, cancellationToken).ConfigureAwait(false);
        var hasNext = releases.Count > effectiveLimit;
        var pageItems = releases.Take(effectiveLimit).ToArray();
        var nextCursor = hasNext && pageItems.Length > 0
            ? new ReleaseCursor(pageItems[^1].ReleaseId.Value).Encode()
            : null;

        return Result<ReleasePage>.Success(new ReleasePage(Array.AsReadOnly(pageItems), nextCursor));
    }

    private async Task<IReadOnlyList<DomainWarning>> EvaluateFindingsAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken)
    {
        var nodes = await _historyRepos.Hierarchy.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var roles = await _historyRepos.Roles.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var resolutions = await _historyRepos.Roles.ListResolutionsBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var contents = await _historyRepos.Contents.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var dependencies = await _historyRepos.Dependencies.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        var report = TransactionValidator.Validate(new TransactionValidationRequest(
            nodes,
            roles,
            resolutions,
            contents,
            dependencies,
            _validationPolicy.ToQualityWarningThresholds(),
            _validationPolicy.PossibleEmbeddedHeadingWarning));

        return report.Warnings;
    }
}
