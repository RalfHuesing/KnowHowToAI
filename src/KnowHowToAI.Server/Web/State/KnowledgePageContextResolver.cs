using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Server.Web.State;

/// <summary>Aufgelöster Current- oder Draft-Lesekontext der Wissensseite.</summary>
public sealed record KnowledgePageContextResolution(
    ReadContext ReadContext,
    KnowledgeContextViewModel ContextViewModel,
    long? ChangeVersion,
    long LoadedSnapshotId);

/// <summary>Löst den Draft-Kontext der Knowledge-URL auf; ohne ID gilt Current.</summary>
public sealed class KnowledgePageContextResolver(
    ISnapshotRepository snapshotRepository,
    ITransactionRepository transactionRepository)
{
    private readonly ISnapshotRepository _snapshotRepository = snapshotRepository ?? throw new ArgumentNullException(nameof(snapshotRepository));
    private readonly ITransactionRepository _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));

    public async Task<Result<KnowledgePageContextResolution>> ResolveAsync(
        string? transactionIdRaw,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionIdRaw))
        {
            var snapshot = await _snapshotRepository.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
            return Result<KnowledgePageContextResolution>.Success(new KnowledgePageContextResolution(
                new ReadContext(),
                new KnowledgeContextViewModel(KnowledgeReadContextKind.Current),
                ChangeVersion: null,
                LoadedSnapshotId: snapshot.SnapshotId.Value));
        }

        if (!Guid.TryParseExact(transactionIdRaw, "D", out var transactionGuid))
        {
            return Result<KnowledgePageContextResolution>.Failure(new DomainError(
                ReadContextErrorCodes.InvalidReadContext,
                $"Der Wert '{transactionIdRaw}' ist keine gültige Entwurfs-ID.",
                new Dictionary<string, string> { ["transactionId"] = transactionIdRaw }));
        }

        var transactionId = new TransactionId(transactionGuid);
        var transaction = await _transactionRepository.FindAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (transaction is null)
        {
            return Result<KnowledgePageContextResolution>.Failure(new DomainError(
                ReadContextErrorCodes.TransactionNotFound,
                "Der angefragte Entwurf existiert nicht.",
                new Dictionary<string, string> { ["transactionId"] = transactionIdRaw }));
        }

        if (transaction.State != TransactionState.Open)
        {
            return Result<KnowledgePageContextResolution>.Failure(new DomainError(
                ReadContextErrorCodes.TransactionClosed,
                "Der angefragte Entwurf ist bereits geschlossen.",
                new Dictionary<string, string> { ["transactionId"] = transactionIdRaw }));
        }

        var readContext = new ReadContext(TransactionId: transactionId);
        var contextViewModel = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Transaction,
            ContextId: transactionId.Value.ToString("D"),
            DisplayName: string.IsNullOrWhiteSpace(transaction.Purpose)
                ? $"Entwurf {transactionId.Value:D}"
                : transaction.Purpose,
            BaseSnapshotId: transaction.BaseSnapshotId.Value,
            ChangeVersion: transaction.ChangeVersion);
        return Result<KnowledgePageContextResolution>.Success(new KnowledgePageContextResolution(
            readContext,
            contextViewModel,
            transaction.ChangeVersion,
            transaction.BaseSnapshotId.Value));
    }
}
