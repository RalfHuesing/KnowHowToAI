using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.TestSupport;

/// <summary>
/// Stellt eine Verbindung zu der manuell bereitgestellten Testdatenbank her. Liest die
/// Verbindung ausschließlich aus der dokumentierten Appsettings-Sektion
/// <c>DatabaseConnection</c> und erzeugt oder entfernt niemals Datenbanken.
/// </summary>
public sealed class SqlTestDatabase : IAsyncDisposable
{
    public string DatabaseName { get; }
    public string ConnectionString { get; }
    internal SqlConnectionFactory ConnectionFactory { get; }
    public SqlStorageConnectionString StorageConnectionString { get; }

    private SqlTestDatabase(string databaseName, string connectionString)
    {
        DatabaseName = databaseName;
        ConnectionString = connectionString;
        StorageConnectionString = new SqlStorageConnectionString { Value = connectionString };
        ConnectionFactory = new SqlConnectionFactory(StorageConnectionString);
    }

    /// <summary>
    /// Prüft die Verbindung zur konfigurierten, bereits vorhandenen Datenbank und gibt
    /// die Instanz zurück. Wirft eine klare Exception, wenn die Voraussetzung fehlt.
    /// </summary>
    public static async Task<SqlTestDatabase> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var settings = SqlIntegrationTestSettings.Load();
        var connectionString = settings.CreateDatabaseConnectionString();

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                $"Preflight-Fehler: Die konfigurierte Datenbank ist nicht für Integrationstests erreichbar (SQL-Fehlercode {ex.Number}). " +
                "Die Datenbank muss manuell bereitgestellt sein und die konfigurierte Anmeldung zulassen.");
        }

        return new SqlTestDatabase(settings.Database, connectionString);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
