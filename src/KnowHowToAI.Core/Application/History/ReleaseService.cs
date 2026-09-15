using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.History;

/// <summary>
/// Transportneutrale Orchestrierung der Release-Use-Cases (create_release, list_releases).
/// create_release benötigt keine KnowHowTo-AI-Transaction: Es ist reine Metadatenverwaltung
/// und ändert keinen versionierten Snapshot-Inhalt (V1-Invariante).
/// </summary>
public sealed class ReleaseService
{
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly IReleaseMutationRepository _releaseMutationRepository;
    private readonly IClock _clock;
    private readonly RetrievalPolicy _retrievalPolicy;

    public ReleaseService(
        ISnapshotRepository snapshotRepository,
        IReleaseMutationRepository releaseMutationRepository,
        IClock clock,
        RetrievalPolicy retrievalPolicy)
    {
        _snapshotRepository = snapshotRepository ?? throw new ArgumentNullException(nameof(snapshotRepository));
        _releaseMutationRepository = releaseMutationRepository ?? throw new ArgumentNullException(nameof(releaseMutationRepository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _retrievalPolicy = retrievalPolicy ?? throw new ArgumentNullException(nameof(retrievalPolicy));
    }

    /// <summary>
    /// Registriert atomar einen unveränderlichen Release-Verweis auf einen committed Snapshot.
    /// Schlägt mit <c>SnapshotNotFound</c>, <c>SnapshotNotCommitted</c> oder
    /// <c>ReleaseNameConflict</c> fehl.
    /// </summary>
    public async Task<Result<Release>> CreateReleaseAsync(
        string name,
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Release>.Failure(new DomainError(
                ReleaseErrorCodes.ReleaseNameRequired,
                "Der Release-Name darf nicht leer oder nur Whitespace sein."));

        var snapshot = await _snapshotRepository.FindAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        if (snapshot is null)
            return Result<Release>.Failure(new DomainError(
                ReleaseErrorCodes.SnapshotNotFound,
                "Der referenzierte Snapshot existiert nicht.",
                new Dictionary<string, string> { [ReleaseErrorCodes.SnapshotIdDetail] = snapshotId.ToString() }));

        if (snapshot.State != SnapshotState.Committed)
            return Result<Release>.Failure(new DomainError(
                ReleaseErrorCodes.SnapshotNotCommitted,
                "Releases können nur auf committed Snapshots angelegt werden.",
                new Dictionary<string, string> { [ReleaseErrorCodes.SnapshotIdDetail] = snapshotId.ToString() }));

        return await _releaseMutationRepository.CreateAsync(
            new ReleaseId(0), // ID wird von der Datenbank vergeben
            name.Trim(),
            snapshotId,
            _clock.UtcNow,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Paginierte, deterministisch sortierte Liste aller Releases.</summary>
    public async Task<Result<ReleasePage>> ListReleasesAsync(
        int? limit,
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        var effectiveLimit = Math.Min(limit ?? _retrievalPolicy.DefaultPageSize, _retrievalPolicy.MaximumPageSize);

        long? afterReleaseId = null;
        if (cursor is not null)
        {
            if (!long.TryParse(cursor, out var parsedCursor))
                return Result<ReleasePage>.Failure(new DomainError(
                    ReleaseErrorCodes.InvalidCursor,
                    "Der Cursor ist ungültig.",
                    new Dictionary<string, string> { [ReleaseErrorCodes.ReleaseIdDetail] = cursor }));
            afterReleaseId = parsedCursor;
        }

        var releases = await _releaseMutationRepository.ListAsync(effectiveLimit + 1, afterReleaseId, cancellationToken).ConfigureAwait(false);
        var hasNext = releases.Count > effectiveLimit;
        var pageItems = releases.Take(effectiveLimit).ToArray();
        var nextCursor = hasNext && pageItems.Length > 0
            ? pageItems[^1].ReleaseId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : null;

        return Result<ReleasePage>.Success(new ReleasePage(Array.AsReadOnly(pageItems), nextCursor));
    }
}
