using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>Read-Port für die Metadaten einer KnowHowTo-AI-Transaction.</summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Erstellt kurz und atomar einen vollständigen Working Snapshot des aktuellen
    /// committed Stands und registriert die offene fachliche Transaction.
    /// </summary>
    Task<KnowledgeTransaction> BeginAsync(
        BeginTransactionRequest request,
        CancellationToken cancellationToken = default);

    Task<KnowledgeTransaction?> FindAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prüft und aktiviert einen Working Snapshot innerhalb einer kurzen atomaren
    /// SQL-Operation. Der Rückgabewert enthält auch bei fachlicher Ablehnung den
    /// vollständigen Validierungsbefund beziehungsweise den stabilen Fehler.
    /// </summary>
    Task<CommitTransactionResult> CommitAsync(
        CommitTransactionRequest request,
        CancellationToken cancellationToken = default);
}
