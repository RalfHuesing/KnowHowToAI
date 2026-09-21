using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Migrations;
using KnowHowToAI.TestSupport;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowHowToAI.IntegrationTests.TestSupport;

/// <summary>
/// Stellt eine Verbindung zu der manuell bereitgestellten Testdatenbank her. Liest die
/// Verbindung ausschließlich aus der dokumentierten Appsettings-Sektion
/// <c>BrowserTestDatabaseConnection</c>. Die Produktverbindung
/// <c>DatabaseConnection</c> ist aus dieser Testinfrastruktur nicht erreichbar.
/// Die manuelle Suite darf ausschließlich ihre exakt konfigurierte Browser-Testdatenbank
/// bereinigen; Datenbanken werden niemals erzeugt oder entfernt.
/// </summary>
public sealed class SqlTestDatabase : IAsyncDisposable
{
    private bool _cleanupOnDispose;
    public string DatabaseName { get; }
    public string ConnectionString { get; }
    internal SqlCleanupTarget CleanupTarget { get; }
    internal SqlConnectionFactory ConnectionFactory { get; }
    public SqlStorageConnectionString StorageConnectionString { get; }

    private SqlTestDatabase(string databaseName, string connectionString, SqlCleanupTarget cleanupTarget)
    {
        DatabaseName = databaseName;
        ConnectionString = connectionString;
        CleanupTarget = cleanupTarget;
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

        return new SqlTestDatabase(settings.Database, connectionString, settings.CleanupTarget);
    }

    /// <summary>
    /// Stellt für einen expliziten manuellen Integrationstest einen leeren
    /// KnowHowToAI-Schemazustand in der dedizierten Browser-Testdatenbank her.
    /// Die Bereinigung ist ausschließlich auf dbo-Objekte mit KnowHowToAI_-Präfix
    /// begrenzt; die Produktdatenbank kann über diesen Pfad nicht adressiert werden.
    /// </summary>
    public static async Task<SqlTestDatabase> ConnectFreshAsync(CancellationToken cancellationToken = default)
    {
        var database = await ConnectAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var productTarget = SqlIntegrationTestSettings.LoadProduct().CleanupTarget;
            var visualTarget = SqlIntegrationTestSettings.LoadVisual().CleanupTarget;
            SqlCleanupTargetGuard.ValidateAndDedupe(
                productTarget,
                [database.CleanupTarget, visualTarget]);
            await database.CleanupBrowserSchemaAsync(cancellationToken).ConfigureAwait(false);
            database._cleanupOnDispose = true;
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
        if (_cleanupOnDispose)
            await CleanupBrowserSchemaAsync(CancellationToken.None).ConfigureAwait(false);

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

    private async Task CleanupBrowserSchemaAsync(CancellationToken cancellationToken)
    {
        await using var connection = await ConnectionFactory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DECLARE @sql nvarchar(max);

            SELECT @sql = STRING_AGG(
                N'ALTER TABLE ' + QUOTENAME(schemaInfo.name) + N'.' + QUOTENAME(parentTable.name) + N' DROP CONSTRAINT ' + QUOTENAME(foreignKeyInfo.name) + N';',
                CHAR(10))
                WITHIN GROUP (ORDER BY foreignKeyInfo.name)
            FROM sys.foreign_keys AS foreignKeyInfo
            INNER JOIN sys.tables AS parentTable ON parentTable.object_id = foreignKeyInfo.parent_object_id
            INNER JOIN sys.tables AS referencedTable ON referencedTable.object_id = foreignKeyInfo.referenced_object_id
            INNER JOIN sys.schemas AS schemaInfo ON schemaInfo.schema_id = parentTable.schema_id
            INNER JOIN sys.schemas AS referencedSchemaInfo ON referencedSchemaInfo.schema_id = referencedTable.schema_id
            WHERE schemaInfo.name = N'dbo'
              AND referencedSchemaInfo.name = N'dbo'
              AND parentTable.name LIKE N'KnowHowToAI[_]%'
              AND referencedTable.name LIKE N'KnowHowToAI[_]%'
              AND foreignKeyInfo.name LIKE N'%KnowHowToAI[_]%';
            IF @sql IS NOT NULL EXEC sys.sp_executesql @sql;

            SELECT @sql = STRING_AGG(
                N'DROP TRIGGER ' + QUOTENAME(schemaInfo.name) + N'.' + QUOTENAME(triggerInfo.name) + N';',
                CHAR(10))
                WITHIN GROUP (ORDER BY triggerInfo.name)
            FROM sys.triggers AS triggerInfo
            INNER JOIN sys.tables AS tableInfo ON tableInfo.object_id = triggerInfo.parent_id
            INNER JOIN sys.schemas AS schemaInfo ON schemaInfo.schema_id = tableInfo.schema_id
            WHERE schemaInfo.name = N'dbo'
              AND triggerInfo.name LIKE N'KnowHowToAI[_]%'
              AND tableInfo.name LIKE N'KnowHowToAI[_]%';
            IF @sql IS NOT NULL EXEC sys.sp_executesql @sql;

            SELECT @sql = STRING_AGG(
                N'DROP TABLE ' + QUOTENAME(schemaInfo.name) + N'.' + QUOTENAME(tableInfo.name) + N';',
                CHAR(10))
                WITHIN GROUP (ORDER BY tableInfo.name DESC)
            FROM sys.tables AS tableInfo
            INNER JOIN sys.schemas AS schemaInfo ON schemaInfo.schema_id = tableInfo.schema_id
            WHERE schemaInfo.name = N'dbo'
              AND tableInfo.name LIKE N'KnowHowToAI[_]%';
            IF @sql IS NOT NULL EXEC sys.sp_executesql @sql;
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
