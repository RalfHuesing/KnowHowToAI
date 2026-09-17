using KnowHowToAI.Storage.SqlServer.Configuration;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.Storage.SqlServer.Connections;

/// <summary>
/// Erzeugt geöffnete SQL-Verbindungen. Connection String wird niemals geloggt oder
/// an Caller zurückgegeben.
/// </summary>
internal sealed class SqlConnectionFactory
{
    /// <summary>
    /// Fester ApplicationName aller produktiven SQL-Verbindungen — erlaubt
    /// eindeutige Zuordnung in SQL Profiler und sys.dm_exec_sessions.
    /// </summary>
    internal const string ApplicationName = "KnowHowToAi";

    private readonly string _connectionString;

    public SqlConnectionFactory(SqlStorageConnectionString connectionString) =>
        _connectionString = BuildConnectionString(connectionString.Value);

    /// <summary>
    /// Ergänzt den festen ApplicationName; ein im Connection String vorhandener
    /// Wert wird überschrieben, damit die Profiler-Zuordnung einheitlich bleibt.
    /// </summary>
    internal static string BuildConnectionString(string connectionString) =>
        new SqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = ApplicationName
        }.ConnectionString;

    /// <summary>Öffnet und gibt eine neue SQL-Verbindung zurück.</summary>
    public async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
