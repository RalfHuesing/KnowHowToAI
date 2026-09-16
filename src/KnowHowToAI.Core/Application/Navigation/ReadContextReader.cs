using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>Lädt die Kandidaten zur Auflösung eines transportneutralen Read-Kontexts.</summary>
public static class ReadContextReader
{
    /// <summary>
    /// Neutraler Platzhalter für <see cref="ReadContextCandidates.CurrentSnapshotId"/>,
    /// wenn kein Current-Kontext vorliegt; der Resolver liest ihn nur im Current-Zweig.
    /// </summary>
    private static readonly SnapshotId NeutralCurrentSnapshotId = new(0);

    public static async Task<Result<ResolvedReadContext>> ResolveAsync(
        ReadContext context,
        ISnapshotRepository snapshotRepository,
        ITransactionRepository transactionRepository,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(snapshotRepository);
        ArgumentNullException.ThrowIfNull(transactionRepository);

        var currentSnapshotId = context.TransactionId is null && context.SnapshotId is null
            ? (await snapshotRepository.GetCurrentAsync(cancellationToken).ConfigureAwait(false)).SnapshotId
            : NeutralCurrentSnapshotId;
        KnowledgeTransaction? transaction = null;
        Snapshot? snapshot = null;

        if (context.TransactionId is { } transactionId)
            transaction = await transactionRepository.FindAsync(transactionId, cancellationToken).ConfigureAwait(false);

        if (context.SnapshotId is { } snapshotId)
            snapshot = await snapshotRepository.FindAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        return ReadContextResolver.Resolve(
            context,
            new ReadContextCandidates(currentSnapshotId, transaction, snapshot));
    }
}
