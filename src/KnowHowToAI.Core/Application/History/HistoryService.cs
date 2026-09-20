using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.History;

/// <summary>
/// Transportneutrale Orchestrierung der Historien-Use-Cases (get_snapshot,
/// compare_snapshots, get_transaction_changes). Alle Methoden sind read-only.
/// </summary>
public sealed class HistoryService
{
    private readonly SnapshotReadRepositories _repos;
    private readonly RetrievalPolicy _retrievalPolicy;

    public HistoryService(SnapshotReadRepositories repositories, RetrievalPolicy retrievalPolicy)
    {
        ArgumentNullException.ThrowIfNull(repositories);
        ArgumentNullException.ThrowIfNull(retrievalPolicy);

        _repos = repositories;
        _retrievalPolicy = retrievalPolicy;
    }

    /// <summary>Liefert Metadaten eines Snapshots oder einen stabilen Fachfehler.</summary>
    public async Task<Result<Snapshot>> GetSnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _repos.Snapshots.FindAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        return snapshot is null
            ? Result<Snapshot>.Failure(new DomainError(
                HistoryErrorCodes.SnapshotNotFound,
                "Der angefragte Snapshot existiert nicht.",
                new Dictionary<string, string> { [HistoryErrorCodes.SnapshotIdDetail] = snapshotId.ToString() }))
            : Result<Snapshot>.Success(snapshot);
    }

    /// <summary>
    /// Liefert ausschließlich committed Snapshots als unveränderliche Historie,
    /// absteigend nach Snapshot-ID und über einen opaken Keyset-Cursor paginiert.
    /// </summary>
    public async Task<Result<SnapshotPage>> ListCommittedSnapshotsAsync(
        int? limit,
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        SnapshotId? beforeSnapshotId = null;
        if (cursor is not null)
        {
            var parsedCursor = SnapshotCursor.TryDecode(cursor);
            if (parsedCursor is null)
            {
                return Result<SnapshotPage>.Failure(new DomainError(
                    HistoryErrorCodes.InvalidCursor,
                    "Der Cursor ist ungültig.",
                    new Dictionary<string, string> { [HistoryErrorCodes.CursorDetail] = cursor }));
            }

            beforeSnapshotId = parsedCursor.BeforeSnapshotId;
        }

        var effectiveLimit = ResolvePageSize(limit);
        var snapshots = await _repos.Snapshots.ListCommittedAsync(
            effectiveLimit + 1,
            beforeSnapshotId,
            cancellationToken).ConfigureAwait(false);
        var hasNext = snapshots.Count > effectiveLimit;
        var items = snapshots.Take(effectiveLimit).ToArray();
        var nextCursor = hasNext && items.Length > 0
            ? new SnapshotCursor(items[^1].SnapshotId).Encode()
            : null;

        return Result<SnapshotPage>.Success(new SnapshotPage(Array.AsReadOnly(items), nextCursor));
    }

    /// <summary>
    /// Vergleicht zwei committed Snapshots und liefert einen strukturierten Netto-Diff.
    /// </summary>
    public async Task<Result<SnapshotDiff>> CompareSnapshotsAsync(
        SnapshotId baseSnapshotId,
        SnapshotId targetSnapshotId,
        int? limit = null,
        string? cursor = null,
        CancellationToken cancellationToken = default) =>
        await CompareSnapshotsAsync(
            new SnapshotComparisonQuery(baseSnapshotId, targetSnapshotId, limit, cursor),
            cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Vergleicht zwei committed Snapshots optional für einen einzelnen Node.
    /// Der Filter ist Teil der Cursorbindung und liefert nur Node-, Content- und Dependency-Änderungen.
    /// </summary>
    public async Task<Result<SnapshotDiff>> CompareSnapshotsAsync(
        SnapshotComparisonQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var baseResult = await ValidateCommittedSnapshotAsync(query.BaseSnapshotId, "Base-Snapshot", cancellationToken).ConfigureAwait(false);
        if (!baseResult.IsSuccess)
            return Result<SnapshotDiff>.Failure(baseResult.Error!);

        var targetResult = await ValidateCommittedSnapshotAsync(query.TargetSnapshotId, "Target-Snapshot", cancellationToken).ConfigureAwait(false);
        if (!targetResult.IsSuccess)
            return Result<SnapshotDiff>.Failure(targetResult.Error!);

        var (offset, cursorError) = ValidateCursor(query.Cursor, query.BaseSnapshotId, query.TargetSnapshotId, null, query.FilterNodeId);
        if (cursorError is not null)
            return Result<SnapshotDiff>.Failure(cursorError);

        var effectiveLimit = ResolvePageSize(query.Limit);

        var baseData = await LoadSnapshotDataAsync(query.BaseSnapshotId, cancellationToken).ConfigureAwait(false);
        var targetData = await LoadSnapshotDataAsync(query.TargetSnapshotId, cancellationToken).ConfigureAwait(false);

        var diff = SnapshotDiffCalculator.Compute(new SnapshotDiffCalculationRequest(
            query.BaseSnapshotId,
            query.TargetSnapshotId,
            baseData,
            targetData,
            effectiveLimit,
            offset,
            ChangeVersion: null,
            FilterNodeId: query.FilterNodeId));

        return Result<SnapshotDiff>.Success(diff);
    }

    /// <summary>
    /// Liefert die Änderungen einer Transaction als strukturierten Netto-Diff.
    /// </summary>
    public async Task<Result<TransactionDiff>> GetTransactionChangesAsync(
        TransactionId transactionId,
        int? limit = null,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _repos.Transactions.FindAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (transaction is null)
            return Result<TransactionDiff>.Failure(new DomainError(
                HistoryErrorCodes.TransactionNotFound,
                "Die angefragte Transaction existiert nicht.",
                new Dictionary<string, string> { [HistoryErrorCodes.TransactionIdDetail] = transactionId.ToString() }));

        if (transaction.State == TransactionState.Discarded)
            return Result<TransactionDiff>.Failure(new DomainError(
                HistoryErrorCodes.TransactionDiscarded,
                "Die Transaction wurde verworfen und besitzt keinen aktiven Diff.",
                new Dictionary<string, string> { [HistoryErrorCodes.TransactionIdDetail] = transactionId.ToString() }));

        var baseSnapshotId = transaction.BaseSnapshotId;
        var targetSnapshotId = transaction.WorkingSnapshotId;
        var effectiveLimit = ResolvePageSize(limit);

        if (transaction.State == TransactionState.Open && _repos.WorkingSnapshots is not null)
        {
            return await ComputeWorkingTransactionChangesAsync(
                transactionId,
                effectiveLimit,
                cursor,
                cancellationToken).ConfigureAwait(false);
        }

        var expectedCommittedChangeVersion = transaction.State == TransactionState.Open ? transaction.ChangeVersion : (long?)null;
        var (offset, cursorError) = ValidateCursor(cursor, baseSnapshotId, targetSnapshotId, expectedCommittedChangeVersion);
        if (cursorError is not null)
            return Result<TransactionDiff>.Failure(cursorError);

        var normalBaseData = await LoadSnapshotDataAsync(baseSnapshotId, cancellationToken).ConfigureAwait(false);
        var normalTargetData = await LoadSnapshotDataAsync(targetSnapshotId, cancellationToken).ConfigureAwait(false);

        var diff = SnapshotDiffCalculator.Compute(new SnapshotDiffCalculationRequest(
            baseSnapshotId,
            targetSnapshotId,
            normalBaseData,
            normalTargetData,
            effectiveLimit,
            offset,
            expectedCommittedChangeVersion));

        return Result<TransactionDiff>.Success(new TransactionDiff(transaction, diff));
    }

    private async Task<Result<TransactionDiff>> ComputeWorkingTransactionChangesAsync(
        TransactionId transactionId,
        int effectiveLimit,
        string? cursor,
        CancellationToken cancellationToken)
    {
        var workingResult = await _repos.WorkingSnapshots!.ReadOpenWorkingAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (!workingResult.IsSuccess)
            return Result<TransactionDiff>.Failure(workingResult.Error!);

        var workingData = workingResult.Value!;
        var expectedChangeVersion = workingData.ChangeVersion;
        var (workingOffset, workingCursorError) = ValidateCursor(cursor, workingData.Transaction.BaseSnapshotId, workingData.Transaction.WorkingSnapshotId, expectedChangeVersion);
        if (workingCursorError is not null)
            return Result<TransactionDiff>.Failure(workingCursorError);

        var baseData = await LoadSnapshotDataAsync(workingData.Transaction.BaseSnapshotId, cancellationToken).ConfigureAwait(false);
        var targetData = new SnapshotData(
            workingData.Nodes,
            workingData.Audiences,
            workingData.AudienceResolutions,
            workingData.Contents,
            workingData.Dependencies);

        var workingDiff = SnapshotDiffCalculator.Compute(new SnapshotDiffCalculationRequest(
            workingData.Transaction.BaseSnapshotId,
            workingData.Transaction.WorkingSnapshotId,
            baseData,
            targetData,
            effectiveLimit,
            workingOffset,
            expectedChangeVersion));

        return Result<TransactionDiff>.Success(new TransactionDiff(workingData.Transaction, workingDiff));
    }

    private async Task<Result<Snapshot>> ValidateCommittedSnapshotAsync(
        SnapshotId snapshotId,
        string label,
        CancellationToken cancellationToken)
    {
        var snapshot = await _repos.Snapshots.FindAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        if (snapshot is null)
            return Result<Snapshot>.Failure(new DomainError(
                HistoryErrorCodes.SnapshotNotFound,
                $"Der {label} existiert nicht.",
                new Dictionary<string, string> { [HistoryErrorCodes.SnapshotIdDetail] = snapshotId.ToString() }));

        if (snapshot.State != SnapshotState.Committed)
            return Result<Snapshot>.Failure(new DomainError(
                HistoryErrorCodes.SnapshotNotCommitted,
                $"Der {label} ist nicht committed.",
                new Dictionary<string, string> { [HistoryErrorCodes.SnapshotIdDetail] = snapshotId.ToString() }));

        return Result<Snapshot>.Success(snapshot);
    }

    private static (int Offset, DomainError? Error) ValidateCursor(
        string? cursor,
        SnapshotId baseSnapshotId,
        SnapshotId targetSnapshotId,
        long? expectedChangeVersion,
        NodeId? expectedFilterNodeId = null)
    {
        if (cursor is null)
            return (0, null);

        var parsedCursor = DiffCursor.TryDecode(cursor);
        if (parsedCursor is null)
        {
            return (0, new DomainError(
                HistoryErrorCodes.InvalidCursor,
                "Der Cursor ist ungültig oder abgelaufen.",
                new Dictionary<string, string> { [HistoryErrorCodes.CursorDetail] = cursor }));
        }

        if (parsedCursor.BaseSnapshotId != baseSnapshotId
            || parsedCursor.TargetSnapshotId != targetSnapshotId
            || parsedCursor.FilterNodeId != expectedFilterNodeId)
        {
            return (0, new DomainError(
                HistoryErrorCodes.InvalidCursor,
                "Der Cursor gehört nicht zu diesem Snapshot-Vergleich.",
                new Dictionary<string, string> { [HistoryErrorCodes.CursorDetail] = cursor }));
        }

        if (expectedChangeVersion.HasValue)
        {
            if (parsedCursor.ChangeVersion != expectedChangeVersion)
            {
                return (0, new DomainError(
                    HistoryErrorCodes.CursorExpired,
                    "Der Cursor ist nach einer zwischenzeitlichen Mutation der Transaction abgelaufen.",
                    new Dictionary<string, string> { [HistoryErrorCodes.CursorDetail] = cursor }));
            }
        }
        else if (parsedCursor.ChangeVersion.HasValue)
        {
            return (0, new DomainError(
                HistoryErrorCodes.InvalidCursor,
                "Der Cursor gehört nicht zu diesem Snapshot-Vergleich.",
                new Dictionary<string, string> { [HistoryErrorCodes.CursorDetail] = cursor }));
        }

        return (parsedCursor.NextOffset, null);
    }

    private int ResolvePageSize(int? requestedLimit) =>
        requestedLimit is > 0
            ? Math.Min(requestedLimit.Value, _retrievalPolicy.MaximumPageSize)
            : _retrievalPolicy.DefaultPageSize;

    private async Task<SnapshotData> LoadSnapshotDataAsync(SnapshotId snapshotId, CancellationToken cancellationToken)
    {
        var nodes = await _repos.Hierarchy.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var audiences = await _repos.Audiences.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var resolutions = await _repos.Audiences.ListResolutionsBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var contents = await _repos.Contents.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var dependencies = await _repos.Dependencies.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        return new SnapshotData(nodes, audiences, resolutions, contents, dependencies);
    }
}
