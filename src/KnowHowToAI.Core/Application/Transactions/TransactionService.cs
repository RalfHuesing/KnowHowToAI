using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Transactions;

/// <summary>Transportneutrale Orchestrierung der Transaktionsoperationen.</summary>
public sealed class TransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IWorkingSnapshotValidationDataRepository _validationDataRepository;
    private readonly IIdentifierGenerator _identifierGenerator;
    private readonly ValidationPolicy _validationPolicy;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IWorkingSnapshotValidationDataRepository validationDataRepository,
        IIdentifierGenerator identifierGenerator,
        ValidationPolicy validationPolicy)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _validationDataRepository = validationDataRepository ?? throw new ArgumentNullException(nameof(validationDataRepository));
        _identifierGenerator = identifierGenerator ?? throw new ArgumentNullException(nameof(identifierGenerator));
        _validationPolicy = validationPolicy ?? throw new ArgumentNullException(nameof(validationPolicy));
    }

    /// <summary>Eröffnet eine Transaction mit einem vollständigen Working Snapshot.</summary>
    public async Task<Result<KnowledgeTransaction>> BeginAsync(
        BeginTransactionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var transaction = await _transactionRepository.BeginAsync(
            new BeginTransactionRequest(
                _identifierGenerator.CreateTransactionId(),
                options.Purpose,
                options.Actor,
                options.Client),
            cancellationToken).ConfigureAwait(false);

        return Result<KnowledgeTransaction>.Success(transaction);
    }

    /// <summary>Liefert die Metadaten einer Transaction oder einen stabilen Fachfehler.</summary>
    public async Task<Result<KnowledgeTransaction>> GetAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.FindAsync(transactionId, cancellationToken).ConfigureAwait(false);
        return transaction is null
            ? Result<KnowledgeTransaction>.Failure(CreateTransactionNotFoundError(transactionId))
            : Result<KnowledgeTransaction>.Success(transaction);
    }

    /// <summary>Liefert alle aktuell offenen Transactions, sortiert nach Erstellungszeit absteigend.</summary>
    public Task<IReadOnlyList<KnowledgeTransaction>> ListOpenAsync(
        CancellationToken cancellationToken = default) =>
        _transactionRepository.ListOpenAsync(cancellationToken);

    /// <summary>Verwirft eine offene Transaction, ohne den Current Snapshot zu verändern.</summary>
    public Task<Result<KnowledgeTransaction>> DiscardAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default) =>
        _transactionRepository.DiscardAsync(transactionId, cancellationToken);

    /// <summary>Prüft den vollständigen Zustand einer offenen Transaction ohne ihn zu verändern.</summary>
    public async Task<Result<TransactionValidationReport>> ValidateAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        var dataResult = await _validationDataRepository.ReadOpenWorkingAsync(transactionId, cancellationToken).ConfigureAwait(false);
        if (!dataResult.IsSuccess)
            return Result<TransactionValidationReport>.Failure(dataResult.Error!);

        var data = dataResult.Value!;

        var report = TransactionValidator.Validate(new TransactionValidationRequest(
            data.Nodes,
            data.Roles,
            data.RoleResolutions,
            data.Contents,
            data.Dependencies,
            _validationPolicy.ToQualityWarningThresholds(),
            _validationPolicy.PossibleEmbeddedHeadingWarning));

        return Result<TransactionValidationReport>.Success(report with { ChangeVersion = data.ChangeVersion });
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

    private static DomainError CreateTransactionNotFoundError(TransactionId transactionId) =>
        new(
            TransactionValidationErrorCodes.TransactionNotFound,
            "Die angefragte Transaction existiert nicht.",
            new Dictionary<string, string>
            {
                [TransactionValidationErrorCodes.TransactionIdDetail] = transactionId.ToString()
            });
}
