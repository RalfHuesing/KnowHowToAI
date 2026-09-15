using Dapper;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using KnowHowToAI.Storage.SqlServer.Repositories;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Transactions;

internal sealed class SqlTransactionRepository : SqlRepository, ITransactionRepository
{
    private const string FindSql = """
        SELECT TransactionId, BaseSnapshotId, WorkingSnapshotId, State, ChangeVersion,
               CreatedAtUtc, CommittedAtUtc, Purpose, Actor, Client, CommitMessage
        FROM dbo.KnowHowToAI_Transaction
        WHERE TransactionId = @transactionId;
        """;

    public SqlTransactionRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
        : base(connectionFactory, storagePolicy)
    {
    }

    public async Task<KnowledgeTransaction?> FindAsync(
        TransactionId transactionId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<TransactionRow>(
            CreateCommand(FindSql, new { transactionId = transactionId.Value }, cancellationToken)).ConfigureAwait(false);
        return row is null ? null : SqlRowMapper.ToTransaction(row);
    }
}
