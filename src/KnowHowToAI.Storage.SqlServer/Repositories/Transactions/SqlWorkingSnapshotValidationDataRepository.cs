using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Transactions;

/// <summary>Liest die gesamte Validierungsansicht einer offenen Working-Transaction atomar über die konsistente M5-Read-Sicht.</summary>
internal sealed class SqlWorkingSnapshotValidationDataRepository : IWorkingSnapshotValidationDataRepository
{
    private readonly SqlWorkingSnapshotReadRepository _readRepository;

    public SqlWorkingSnapshotValidationDataRepository(
        SqlConnectionFactory connectionFactory,
        SqlStoragePolicy storagePolicy,
        Func<CancellationToken, Task>? afterGuardReadForTestAsync = null)
    {
        _readRepository = new SqlWorkingSnapshotReadRepository(connectionFactory, storagePolicy, afterGuardReadForTestAsync);
    }

    public async Task<Result<WorkingSnapshotValidationData>> ReadOpenWorkingAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        var readResult = await _readRepository.ReadOpenWorkingAsync(transactionId, cancellationToken).ConfigureAwait(false);
        return readResult.IsSuccess
            ? Result<WorkingSnapshotValidationData>.Success(new WorkingSnapshotValidationData(
                readResult.Value!.Nodes,
                readResult.Value.Roles,
                readResult.Value.RoleResolutions,
                readResult.Value.Contents,
                readResult.Value.Dependencies))
            : Result<WorkingSnapshotValidationData>.Failure(readResult.Error!);
    }
}
