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
}
