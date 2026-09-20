using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>
/// Persistence-Port für eine zusammenhängende, konsistente M5-Read-Sicht auf offene Working Snapshots.
/// Serialisiert über die Transaction-Zeilensperre und gibt Transaction, ChangeVersion, Nodes, Zielgruppen,
/// Resolution Orders, Contents und Dependencies atomar zurück.
/// </summary>
public interface IWorkingSnapshotReadRepository
{
    Task<Result<WorkingSnapshotReadData>> ReadOpenWorkingAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default);
}
