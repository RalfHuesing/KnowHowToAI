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
    private bool _resetOnDispose;

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
    /// Stellt für einen expliziten manuellen Migrationstest einen leeren
    /// KnowHowToAI-Schemazustand bereit. Die Datenbank selbst bleibt unverändert.
    /// </summary>
    public static async Task<SqlTestDatabase> ConnectFreshAsync(CancellationToken cancellationToken = default)
    {
        var database = await ConnectAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await database.ResetKnowHowToAISchemaAsync(cancellationToken).ConfigureAwait(false);
            database._resetOnDispose = true;
            return database;
        }
        catch
        {
            await database.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    internal static SqlSchemaMigrator CreateMigrator(SqlTestDatabase database) => new(
        database.ConnectionFactory,
        new SqlStoragePolicy { CommandTimeoutSeconds = 30 },
        new MigrationPolicy { LockTimeoutSeconds = 30, ApplyOnStartup = true },
        new EmbeddedMigrationCatalog(),
        NullLogger<SqlSchemaMigrator>.Instance);

    public async ValueTask DisposeAsync()
    {
        if (_resetOnDispose)
            await ResetKnowHowToAISchemaAsync(CancellationToken.None).ConfigureAwait(false);
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

    private async Task ResetKnowHowToAISchemaAsync(CancellationToken cancellationToken)
    {
        await using var connection = await ConnectionFactory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.Transaction = (SqlTransaction)transaction;
        command.CommandText = """
            DROP TABLE IF EXISTS dbo.KnowHowToAI_RollbackProbe;
            DROP TABLE IF EXISTS dbo.KnowHowToAI_ContentDependency;
            DROP TABLE IF EXISTS dbo.KnowHowToAI_NodeContent;
            DROP TABLE IF EXISTS dbo.KnowHowToAI_RoleResolution;
            DROP TABLE IF EXISTS dbo.KnowHowToAI_Node;
            DROP TABLE IF EXISTS dbo.KnowHowToAI_Role;
            DROP TABLE IF EXISTS dbo.KnowHowToAI_Release;
            DROP TABLE IF EXISTS dbo.KnowHowToAI_Transaction;
            DROP TABLE IF EXISTS dbo.KnowHowToAI_SystemState;
            DROP TABLE IF EXISTS dbo.KnowHowToAI_Snapshot;
            DROP TABLE IF EXISTS dbo.KnowHowToAI_SchemaMigration;
            """;

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }
}
