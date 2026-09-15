using Dapper;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.Storage.SqlServer.Repositories;

/// <summary>Gemeinsame technische Basis für kurzlebige, parametrisierte SQL-Zugriffe.</summary>
internal abstract class SqlRepository
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly SqlStoragePolicy _storagePolicy;

    protected SqlRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy)
    {
        _connectionFactory = connectionFactory;
        _storagePolicy = storagePolicy;
    }

    protected Task<SqlConnection> OpenAsync(CancellationToken cancellationToken) =>
        _connectionFactory.OpenAsync(cancellationToken);

    protected CommandDefinition CreateCommand(
        string commandText,
        object? parameters,
        CancellationToken cancellationToken) =>
        SqlCommandFactory.Create(commandText, parameters, _storagePolicy, cancellationToken);
}
