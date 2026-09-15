using KnowHowToAI.Storage.SqlServer.Configuration;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.Storage.SqlServer.Connections;

/// <summary>
/// Erzeugt geöffnete SQL-Verbindungen. Connection String wird niemals geloggt oder
/// an Caller zurückgegeben.
/// </summary>
internal sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(SqlStorageConnectionString connectionString) =>
        _connectionString = connectionString.Value;

    /// <summary>Öffnet und gibt eine neue SQL-Verbindung zurück.</summary>
    public async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
