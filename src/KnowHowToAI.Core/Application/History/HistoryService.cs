using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.History;

/// <summary>
/// Transportneutrale Orchestrierung der Historien-Use-Cases (get_snapshot,
/// compare_snapshots, get_transaction_changes). Alle Methoden sind read-only.
/// Die eigentliche Diff-Logik wird in M5.4 implementiert.
/// </summary>
public sealed class HistoryService
{
    private readonly HistoryRepositories _repos;

    public HistoryService(HistoryRepositories repositories)
    {
        _repos = repositories ?? throw new ArgumentNullException(nameof(repositories));
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
    /// Vergleicht zwei Snapshots und liefert einen strukturierten Netto-Diff.
    /// Beide Snapshots müssen committed sein. Implementierung folgt in M5.4.
    /// </summary>
    public async Task<Result<SnapshotDiff>> CompareSnapshotsAsync(
        SnapshotId baseSnapshotId,
        SnapshotId targetSnapshotId,
        CancellationToken cancellationToken = default)
    {
        var baseSnapshot = await _repos.Snapshots.FindAsync(baseSnapshotId, cancellationToken).ConfigureAwait(false);
        if (baseSnapshot is null)
            return Result<SnapshotDiff>.Failure(new DomainError(
                HistoryErrorCodes.SnapshotNotFound,
                "Der Base-Snapshot existiert nicht.",
                new Dictionary<string, string> { [HistoryErrorCodes.SnapshotIdDetail] = baseSnapshotId.ToString() }));

        var targetSnapshot = await _repos.Snapshots.FindAsync(targetSnapshotId, cancellationToken).ConfigureAwait(false);
        if (targetSnapshot is null)
            return Result<SnapshotDiff>.Failure(new DomainError(
                HistoryErrorCodes.SnapshotNotFound,
                "Der Target-Snapshot existiert nicht.",
                new Dictionary<string, string> { [HistoryErrorCodes.SnapshotIdDetail] = targetSnapshotId.ToString() }));

        if (baseSnapshot.State != SnapshotState.Committed)
            return Result<SnapshotDiff>.Failure(new DomainError(
                HistoryErrorCodes.SnapshotNotCommitted,
                "Der Base-Snapshot ist nicht committed.",
                new Dictionary<string, string> { [HistoryErrorCodes.SnapshotIdDetail] = baseSnapshotId.ToString() }));

        if (targetSnapshot.State != SnapshotState.Committed)
            return Result<SnapshotDiff>.Failure(new DomainError(
                HistoryErrorCodes.SnapshotNotCommitted,
                "Der Target-Snapshot ist nicht committed.",
                new Dictionary<string, string> { [HistoryErrorCodes.SnapshotIdDetail] = targetSnapshotId.ToString() }));

        // Diff-Berechnung folgt in M5.4
        throw new NotImplementedException("CompareSnapshotsAsync wird in M5.4 implementiert.");
    }

    /// <summary>
    /// Liefert die Änderungen einer Transaction als strukturierten Netto-Diff.
    /// Implementierung folgt in M5.4.
    /// </summary>
    public async Task<Result<TransactionDiff>> GetTransactionChangesAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _repos.Transactions.FindAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (transaction is null)
            return Result<TransactionDiff>.Failure(new DomainError(
                HistoryErrorCodes.TransactionNotFound,
                "Die angefragte Transaction existiert nicht.",
                new Dictionary<string, string> { [HistoryErrorCodes.TransactionIdDetail] = transactionId.ToString() }));

        // Diff-Berechnung folgt in M5.4
        throw new NotImplementedException("GetTransactionChangesAsync wird in M5.4 implementiert.");
    }
}
