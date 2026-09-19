using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.TestSupport;

public sealed class InMemoryWorkingSnapshotValidationDataRepository : IWorkingSnapshotValidationDataRepository
{
    private readonly Dictionary<TransactionId, Result<WorkingSnapshotValidationData>> _data = new();

    public void SetValidationData(TransactionId transactionId, WorkingSnapshotValidationData data) =>
        _data[transactionId] = Result<WorkingSnapshotValidationData>.Success(data);

    public void SetFailure(TransactionId transactionId, DomainError error) =>
        _data[transactionId] = Result<WorkingSnapshotValidationData>.Failure(error);

    public Task<Result<WorkingSnapshotValidationData>> ReadOpenWorkingAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        if (_data.TryGetValue(transactionId, out var result))
        {
            return Task.FromResult(result);
        }

        return Task.FromResult(Result<WorkingSnapshotValidationData>.Failure(new DomainError(
            TransactionValidationErrorCodes.TransactionNotFound,
            "Working-Snapshot nicht gefunden.",
            new Dictionary<string, string> { [TransactionValidationErrorCodes.TransactionIdDetail] = transactionId.ToString() })));
    }
}
