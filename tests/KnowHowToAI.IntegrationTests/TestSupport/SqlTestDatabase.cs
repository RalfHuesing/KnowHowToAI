using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.TestSupport;

/// <summary>
/// Erstellt eine eindeutig benannte, isolierte SQL-Testdatenbank pro Testlauf und entfernt
/// sie danach wieder. Liest die Verbindung ausschließlich aus der dokumentierten
/// Appsettings-Sektion <c>DatabaseConnection</c>.
/// Datenbankname wird vor dem Löschen gegen das Testpräfix validiert.
/// </summary>
public sealed class SqlTestDatabase : IAsyncDisposable
{
    /// <summary>Obligatorisches Präfix für Testdatenbanknamen – verhindert versehentliches Löschen.</summary>
    public const string DatabasePrefix = "KnowHowToAI_Test_";

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
    /// Liest die Verbindung aus der App-Konfiguration, erzeugt eine eindeutig benannte
    /// Testdatenbank und gibt die Instanz zurück.
    /// Wirft eine klare Exception, wenn die Voraussetzung fehlt.
    /// </summary>
    public static async Task<SqlTestDatabase> CreateAsync(CancellationToken cancellationToken = default)
    {
        var settings = SqlIntegrationTestSettings.Load();
        var masterConnStr = settings.CreateMasterConnectionString();

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..12];
        var dbName = $"{DatabasePrefix}{uniqueSuffix}";

        // Datenbank im master-Kontext anlegen
        var masterBuilder = new SqlConnectionStringBuilder(masterConnStr)
        {
            InitialCatalog = "master"
        };

        try
        {
            await using var masterConn = new SqlConnection(masterBuilder.ConnectionString);
            await masterConn.OpenAsync(cancellationToken).ConfigureAwait(false);
            var createCmd = masterConn.CreateCommand();
            // DatabaseName enthält nur alphanumerische Zeichen und Unterstriche – sicher für DDL
            createCmd.CommandText = $"CREATE DATABASE [{dbName}];";
            await createCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                $"Preflight-Fehler: SQL Server ist nicht für Integrationstests bereit (SQL-Fehlercode {ex.Number}). " +
                "Die konfigurierte Anmeldung benötigt Erreichbarkeit sowie CREATE/DROP-Berechtigung für isolierte Testdatenbanken.");
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
