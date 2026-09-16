using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>Lädt die Kandidaten zur Auflösung eines transportneutralen Read-Kontexts.</summary>
public static class ReadContextReader
{
    public static async Task<Result<ResolvedReadContext>> ResolveAsync(
        ReadContext context,
        ISnapshotRepository snapshotRepository,
        ITransactionRepository transactionRepository,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(snapshotRepository);
        ArgumentNullException.ThrowIfNull(transactionRepository);

        var currentSnapshot = await snapshotRepository.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        KnowledgeTransaction? transaction = null;
        Snapshot? snapshot = null;

        if (context.TransactionId is { } transactionId)
            transaction = await transactionRepository.FindAsync(transactionId, cancellationToken).ConfigureAwait(false);

        if (context.SnapshotId is { } snapshotId)
            snapshot = await snapshotRepository.FindAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        return ReadContextResolver.Resolve(
            context,
            new ReadContextCandidates(currentSnapshot.SnapshotId, transaction, snapshot));
    }
}
