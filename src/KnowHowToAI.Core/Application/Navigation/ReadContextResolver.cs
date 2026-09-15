using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Navigation;

public static class ReadContextResolver
{
    public static Result<ResolvedReadContext> Resolve(ReadContext context, ReadContextCandidates candidates)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);

        if (context.TransactionId is { } transactionId && context.SnapshotId is { } snapshotId)
            return Result<ResolvedReadContext>.Failure(new DomainError(
                ReadContextErrorCodes.InvalidReadContext,
                "Ein Read-Kontext darf nicht gleichzeitig eine Transaction und einen Snapshot selektieren.",
                new Dictionary<string, string>
                {
                    ["transactionId"] = transactionId.ToString(),
                    ["snapshotId"] = snapshotId.ToString()
                }));

        if (context.TransactionId is { } requestedTransactionId)
            return ResolveTransaction(context, candidates, requestedTransactionId);

        if (context.SnapshotId is { } requestedSnapshotId)
            return ResolveSnapshot(context, candidates, requestedSnapshotId);

        return Result<ResolvedReadContext>.Success(new ResolvedReadContext(
            candidates.CurrentSnapshotId,
            ReadContextSource.Current,
            TransactionId: null,
            context.IncludeDeleted));
    }

    private static Result<ResolvedReadContext> ResolveTransaction(
        ReadContext context,
        ReadContextCandidates candidates,
        TransactionId requestedTransactionId)
    {
        var transaction = candidates.Transaction;
        if (transaction is null || transaction.TransactionId != requestedTransactionId)
            return Result<ResolvedReadContext>.Failure(new DomainError(
                ReadContextErrorCodes.TransactionNotFound,
                "Die angefragte Transaction existiert nicht.",
                new Dictionary<string, string>
                {
                    ["transactionId"] = requestedTransactionId.ToString()
                }));

        return Result<ResolvedReadContext>.Success(new ResolvedReadContext(
            transaction.WorkingSnapshotId,
            ReadContextSource.Transaction,
            transaction.TransactionId,
            context.IncludeDeleted));
    }

    private static Result<ResolvedReadContext> ResolveSnapshot(
        ReadContext context,
        ReadContextCandidates candidates,
        SnapshotId requestedSnapshotId)
    {
        var snapshot = candidates.Snapshot;
        if (snapshot is null || snapshot.SnapshotId != requestedSnapshotId)
            return Result<ResolvedReadContext>.Failure(new DomainError(
                ReadContextErrorCodes.SnapshotNotFound,
                "Der angefragte Snapshot existiert nicht.",
                new Dictionary<string, string>
                {
                    ["snapshotId"] = requestedSnapshotId.ToString()
                }));

        return Result<ResolvedReadContext>.Success(new ResolvedReadContext(
            snapshot.SnapshotId,
            ReadContextSource.Snapshot,
            TransactionId: null,
            context.IncludeDeleted));
    }
}
