using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;

namespace KnowHowToAI.Core.Application.Transactions;

/// <summary>Transportneutrale Orchestrierung der Transaktionsoperationen.</summary>
public sealed class TransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IWorkingSnapshotValidationDataRepository _validationDataRepository;
    private readonly ValidationPolicy _validationPolicy;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IWorkingSnapshotValidationDataRepository validationDataRepository,
        ValidationPolicy validationPolicy)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _validationDataRepository = validationDataRepository ?? throw new ArgumentNullException(nameof(validationDataRepository));
        _validationPolicy = validationPolicy ?? throw new ArgumentNullException(nameof(validationPolicy));
    }

    /// <summary>Prüft den vollständigen Zustand einer offenen Transaction ohne ihn zu verändern.</summary>
    public async Task<Result<TransactionValidationReport>> ValidateAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        var dataResult = await _validationDataRepository.ReadOpenWorkingAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (!dataResult.IsSuccess)
            return Result<TransactionValidationReport>.Failure(dataResult.Error!);

        var data = dataResult.Value!;

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

}
