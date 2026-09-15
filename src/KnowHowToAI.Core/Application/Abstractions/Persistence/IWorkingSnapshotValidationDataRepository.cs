using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>Liest eine offene Working-Transaction samt vollständiger Validierungsansicht atomar.</summary>
public interface IWorkingSnapshotValidationDataRepository
{
    Task<Result<WorkingSnapshotValidationData>> ReadOpenWorkingAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default);
}
