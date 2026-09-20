using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Migrations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowHowToAI.IntegrationTests.TestSupport;

/// <summary>
/// Stellt eine Verbindung zu der manuell bereitgestellten Testdatenbank her. Liest die
/// Verbindung ausschließlich aus der dokumentierten Appsettings-Sektion
/// <c>DatabaseConnection</c> und erzeugt oder entfernt niemals Datenbanken. Manuelle
/// Migrationstests verwenden ausschließlich eine dafür dedizierte Datenbank.
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

    /// <summary>
    /// Prüft für einen expliziten manuellen Migrationstest den vom Benutzer bereits
    /// hergestellten leeren KnowHowToAI-Schemazustand. Es werden keine Objekte entfernt.
    /// </summary>
    public static async Task<SqlTestDatabase> ConnectFreshAsync(CancellationToken cancellationToken = default)
    {
        var database = await ConnectAsync(cancellationToken).ConfigureAwait(false);
        await database.AssertKnowHowToAISchemaIsEmptyAsync(cancellationToken).ConfigureAwait(false);
        return database;
    }

    internal static SqlSchemaMigrator CreateMigrator(SqlTestDatabase database) => new(
        database.ConnectionFactory,
        new SqlStoragePolicy { CommandTimeoutSeconds = 30 },
        new MigrationPolicy { LockTimeoutSeconds = 30, ApplyOnStartup = true },
        new EmbeddedMigrationCatalog(),
        NullLogger<SqlSchemaMigrator>.Instance);

    public async ValueTask DisposeAsync()
    {
        await ValueTask.CompletedTask;
    }

    public async Task<int> GetSqlServerMajorVersionAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT CAST(SERVERPROPERTY('ProductMajorVersion') AS INT);";
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is int majorVersion
            ? majorVersion
            : throw new InvalidOperationException("Preflight-Fehler: Die SQL-Server-Hauptversion konnte nicht ermittelt werden.");
    }

    internal async Task ExecuteAsync(
        string commandText,
        params SqlParameter[] parameters)
    {
        await using var connection = await ConnectionFactory.OpenAsync().ConfigureAwait(false);
        await using var command = new SqlCommand(commandText, connection);
        foreach (var parameter in parameters)
            command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private async Task AssertKnowHowToAISchemaIsEmptyAsync(CancellationToken cancellationToken)
    {
        await using var connection = await ConnectionFactory.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM sys.tables AS tables
            INNER JOIN sys.schemas AS schemas ON schemas.schema_id = tables.schema_id
            WHERE schemas.name = N'dbo' AND tables.name LIKE N'KnowHowToAI[_]%';
            """;
        var count = (int)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
        if (count != 0)
            throw new InvalidOperationException($"Preflight-Fehler: Es existieren bereits {count} KnowHowToAI_-Tabellen; es wird nichts gelöscht.");
    }
}
