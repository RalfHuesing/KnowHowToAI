using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Core.Domain.Validation;

namespace KnowHowToAI.Core.Application.Transactions;

/// <summary>Transportneutrale Orchestrierung der Transaktionsoperationen.</summary>
public sealed class TransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly WorkingSnapshotValidationDataReader _validationDataReader;
    private readonly ValidationPolicy _validationPolicy;

    public TransactionService(
        ITransactionRepository transactionRepository,
        ISnapshotRepository snapshotRepository,
        WorkingSnapshotValidationDataReader validationDataReader,
        ValidationPolicy validationPolicy)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _snapshotRepository = snapshotRepository ?? throw new ArgumentNullException(nameof(snapshotRepository));
        _validationDataReader = validationDataReader ?? throw new ArgumentNullException(nameof(validationDataReader));
        _validationPolicy = validationPolicy ?? throw new ArgumentNullException(nameof(validationPolicy));
    }

    /// <summary>Prüft den vollständigen Zustand einer offenen Transaction ohne ihn zu verändern.</summary>
    public async Task<Result<TransactionValidationReport>> ValidateAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.FindAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (transaction is null)
            return Result<TransactionValidationReport>.Failure(CreateTransactionError(
                TransactionValidationErrorCodes.TransactionNotFound,
                "Die angefragte Transaction existiert nicht.",
                transactionId));

        if (transaction.State != TransactionState.Open)
            return Result<TransactionValidationReport>.Failure(CreateTransactionError(
                TransactionValidationErrorCodes.TransactionClosed,
                "Die angefragte Transaction ist nicht offen.",
                transactionId));

        var snapshot = await _snapshotRepository.FindAsync(transaction.WorkingSnapshotId, cancellationToken).ConfigureAwait(false);
        if (snapshot is null || snapshot.State != SnapshotState.Working)
            return Result<TransactionValidationReport>.Failure(CreateTransactionError(
                TransactionValidationErrorCodes.WorkingSnapshotNotOpen,
                "Der Working Snapshot der Transaction ist nicht bearbeitbar.",
                transactionId));

        var data = await _validationDataReader.ReadAsync(transaction.WorkingSnapshotId, cancellationToken).ConfigureAwait(false);

        return Result<TransactionValidationReport>.Success(TransactionValidator.Validate(new TransactionValidationRequest(
            data.Nodes,
            data.Roles,
            data.RoleResolutions,
            data.Contents,
            data.Dependencies,
            _validationPolicy.ToQualityWarningThresholds(),
            _validationPolicy.PossibleEmbeddedHeadingWarning)));
    }

    /// <summary>
    /// Committet den Working Snapshot. Die persistente Implementierung validiert unter
    /// derselben kurzen SQL-Transaction, damit zwischen Validierung und Aktivierung
    /// kein konkurrierender Write eingeschleust werden kann.
    /// </summary>
    public Task<CommitTransactionResult> CommitAsync(
        TransactionId transactionId,
        string? commitMessage,
        CancellationToken cancellationToken = default) =>
        _transactionRepository.CommitAsync(
            new CommitTransactionRequest(
                transactionId,
                commitMessage,
                _validationPolicy.ToQualityWarningThresholds(),
                _validationPolicy.PossibleEmbeddedHeadingWarning),
            cancellationToken);

    private static DomainError CreateTransactionError(string code, string message, TransactionId transactionId) =>
        new(code, message, new Dictionary<string, string>
        {
            [TransactionValidationErrorCodes.TransactionIdDetail] = transactionId.ToString()
        });
}
