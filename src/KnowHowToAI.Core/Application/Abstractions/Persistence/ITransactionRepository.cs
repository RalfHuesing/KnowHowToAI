using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>Read-Port für die Metadaten einer KnowHowTo-AI-Transaction.</summary>
public interface ITransactionRepository
{
    Task<KnowledgeTransaction?> FindAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default);
}
