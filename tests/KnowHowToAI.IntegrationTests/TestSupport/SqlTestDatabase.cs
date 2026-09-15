using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.TestSupport;

/// <summary>
/// Erstellt eine eindeutig benannte, isolierte SQL-Testdatenbank pro Testlauf und entfernt
/// sie danach wieder. Liest den Connection String ausschließlich aus der dokumentierten
/// Konfiguration (Environment-Variable KHTOAI_INTEGRATION_CONNSTR).
/// Datenbankname wird vor dem Löschen gegen das Testpräfix validiert.
/// </summary>
public sealed class SqlTestDatabase : IAsyncDisposable
{
    /// <summary>Obligatorisches Präfix für Testdatenbanknamen – verhindert versehentliches Löschen.</summary>
    public const string DatabasePrefix = "KnowHowToAI_Test_";

    /// <summary>Environment-Variable, aus der der Connection String gelesen wird.</summary>
    public const string ConnectionStringEnvVar = "KHTOAI_INTEGRATION_CONNSTR";

    private readonly string _masterConnectionString;
    private bool _disposed;

    public string DatabaseName { get; }
    public string ConnectionString { get; }
    internal SqlConnectionFactory ConnectionFactory { get; }
    public SqlStorageConnectionString StorageConnectionString { get; }

    private SqlTestDatabase(string databaseName, string connectionString, string masterConnectionString)
    {
        DatabaseName = databaseName;
        ConnectionString = connectionString;
        _masterConnectionString = masterConnectionString;
        StorageConnectionString = new SqlStorageConnectionString { Value = connectionString };
        ConnectionFactory = new SqlConnectionFactory(StorageConnectionString);
    }

    /// <summary>
    /// Liest den Connection String aus der Environment-Variable, erzeugt eine eindeutig
    /// benannte Testdatenbank und gibt die Instanz zurück.
    /// Wirft eine klare Exception, wenn die Voraussetzung fehlt.
    /// </summary>
    public static async Task<SqlTestDatabase> CreateAsync(CancellationToken cancellationToken = default)
    {
        var masterConnStr = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
        if (string.IsNullOrWhiteSpace(masterConnStr))
        {
            throw new InvalidOperationException(
                $"Preflight-Fehler: Die Environment-Variable '{ConnectionStringEnvVar}' ist nicht gesetzt. " +
                "Sie muss einen SQL-Server-Connection-String für Integrationstests enthalten. " +
                "Bitte die Variable in der Test-Umgebung setzen (User Secrets / CI-Variable). " +
                "Credentials gehören niemals in den Code.");
        }

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..12];
        var dbName = $"{DatabasePrefix}{uniqueSuffix}";

        // Datenbank im master-Kontext anlegen
        var masterBuilder = new SqlConnectionStringBuilder(masterConnStr)
        {
            InitialCatalog = "master"
        };

        await using (var masterConn = new SqlConnection(masterBuilder.ConnectionString))
        {
            await masterConn.OpenAsync(cancellationToken).ConfigureAwait(false);
            var createCmd = masterConn.CreateCommand();
            // DatabaseName enthält nur alphanumerische Zeichen und Unterstriche – sicher für DDL
            createCmd.CommandText = $"CREATE DATABASE [{dbName}];";
            await createCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        // Connection String auf die neue Datenbank umstellen
        var testBuilder = new SqlConnectionStringBuilder(masterConnStr)
        {
            InitialCatalog = dbName
        };

        return new SqlTestDatabase(dbName, testBuilder.ConnectionString, masterBuilder.ConnectionString);
    }

    /// <summary>
    /// Entfernt die Testdatenbank. Validiert den Datenbanknamen gegen das Testpräfix,
    /// bevor die Löschanweisung ausgeführt wird.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        // Sicherheitscheck: nur Datenbanken mit dem definierten Testpräfix löschen
        if (!DatabaseName.StartsWith(DatabasePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Sicherheitsfehler: Datenbank '{DatabaseName}' hat nicht das erwartete Testpräfix " +
                $"'{DatabasePrefix}' und wird nicht gelöscht.");
        }

        await using var masterConn = new SqlConnection(_masterConnectionString);
        await masterConn.OpenAsync().ConfigureAwait(false);

        // Aktive Verbindungen zur Testdatenbank trennen, bevor gelöscht wird.
        var killCmd = masterConn.CreateCommand();
        killCmd.CommandText = $"""
            IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'{DatabaseName}')
            BEGIN
                ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{DatabaseName}];
            END
            """;
        await killCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        _disposed = true;
    }
}
