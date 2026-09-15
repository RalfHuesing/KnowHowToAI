using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace KnowHowToAI.Storage.SqlServer.Migrations;

/// <summary>
/// SQL-Server-Implementierung des <see cref="ISchemaMigrator"/>-Ports.
/// Führt eingebettete Migrationsskripte in strikt numerischer Reihenfolge aus,
/// serialisiert parallele Runner per SQL-Applikationssperre und verhindert
/// Teilmigrationen durch kurze SQL-Transaktionen pro Skript.
/// </summary>
internal sealed class SqlSchemaMigrator : ISchemaMigrator
{
    private const string AppLockName = "KnowHowToAI_SchemaMigration";
    private const string AppLockOwner = "Session";
    private const int LockCommandGraceSeconds = 5;

    private readonly SqlConnectionFactory _connectionFactory;
    private readonly SqlStoragePolicy _storagePolicy;
    private readonly MigrationPolicy _policy;
    private readonly EmbeddedMigrationCatalog _catalog;
    private readonly ILogger<SqlSchemaMigrator> _logger;

    public SqlSchemaMigrator(
        SqlConnectionFactory connectionFactory,
        SqlStoragePolicy storagePolicy,
        MigrationPolicy policy,
        EmbeddedMigrationCatalog catalog,
        ILogger<SqlSchemaMigrator> logger)
    {
        _connectionFactory = connectionFactory;
        _storagePolicy = storagePolicy;
        _policy = policy;
        _catalog = catalog;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<int> MigrateAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Der Lock schützt auch das Bootstrap vor parallelem CREATE TABLE.
        await AcquireAppLockAsync(connection, cancellationToken).ConfigureAwait(false);
        try
        {
            await ExecuteBootstrapAsync(connection, cancellationToken).ConfigureAwait(false);
            var applied = await ReadAppliedMigrationsAsync(connection, cancellationToken).ConfigureAwait(false);

            var appliedCount = 0;
            foreach (var script in _catalog.Scripts)
            {
                if (applied.TryGetValue(script.Version, out var journalChecksum))
                {
                    // Bereits angewendet – Checksum-Integrität prüfen
                    if (!journalChecksum.SequenceEqual(script.ChecksumSha256))
                    {
                        _logger.LogError(
                            "Migration {Version} '{Name}' ist mit Fehlercode {ErrorCode} fehlgeschlagen.",
                            script.Version,
                            script.Name,
                            MigrationChecksumMismatchException.ErrorCode);
                        throw new MigrationChecksumMismatchException(script.Name, script.Version);
                    }
                    continue;
                }

                // Skript noch nicht angewendet – in kurzem BEGIN TRAN ausführen
                await ApplyMigrationAsync(connection, script, cancellationToken).ConfigureAwait(false);
                appliedCount++;
            }

            if (appliedCount == 0)
                _logger.LogInformation("Schema-Migrationen: keine ausstehenden Migrationen.");
            else
                _logger.LogInformation("Schema-Migrationen: {Count} Skript(e) erfolgreich angewendet.", appliedCount);

            return appliedCount;
        }
        finally
        {
            await ReleaseAppLockAsync(connection).ConfigureAwait(false);
        }
    }

    private Task ExecuteBootstrapAsync(SqlConnection connection, CancellationToken cancellationToken) =>
        ExecuteScriptAsync(connection, transaction: null, _catalog.BootstrapScript.Content, cancellationToken);

    private async Task AcquireAppLockAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var timeoutMs = _policy.LockTimeoutSeconds * 1000;
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            DECLARE @result INT;
            EXEC @result = sp_getapplock
                @Resource    = @resource,
                @LockMode    = 'Exclusive',
                @LockOwner   = @owner,
                @LockTimeout = @timeout;
            SELECT @result;
            """;
        cmd.Parameters.Add("@resource", System.Data.SqlDbType.NVarChar, 255).Value = AppLockName;
        cmd.Parameters.Add("@owner", System.Data.SqlDbType.NVarChar, 32).Value = AppLockOwner;
        cmd.Parameters.AddWithValue("@timeout", timeoutMs);
        cmd.CommandTimeout = _policy.LockTimeoutSeconds + LockCommandGraceSeconds;

        var result = (int)(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
        if (result < 0)
        {
            throw new InvalidOperationException(
                $"sp_getapplock für '{AppLockName}' fehlgeschlagen (Ergebnis: {result}). " +
                "Timeout oder Deadlock beim Warten auf den Migration-Lock.");
        }

        _logger.LogDebug("Migration-Lock '{LockName}' erworben.", AppLockName);
    }

    private async Task ReleaseAppLockAsync(SqlConnection connection)
    {
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "EXEC sp_releaseapplock @Resource = @resource, @LockOwner = @owner;";
            cmd.CommandTimeout = _storagePolicy.CommandTimeoutSeconds;
            cmd.Parameters.Add("@resource", System.Data.SqlDbType.NVarChar, 255).Value = AppLockName;
            cmd.Parameters.Add("@owner", System.Data.SqlDbType.NVarChar, 32).Value = AppLockOwner;
            await cmd.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception releaseEx)
        {
            // Lock-Freigabe darf keine ursprüngliche Exception überschreiben; Fehler nur loggen
            _logger.LogWarning(
                "Migration-Lock '{LockName}' konnte nicht freigegeben werden (SQL-Fehlercode {DatabaseErrorNumber}).",
                AppLockName,
                (releaseEx as SqlException)?.Number);
        }
    }

    private async Task<Dictionary<int, byte[]>> ReadAppliedMigrationsAsync(
        SqlConnection connection, CancellationToken cancellationToken)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandTimeout = _storagePolicy.CommandTimeoutSeconds;
        cmd.CommandText = "SELECT Version, ChecksumSha256 FROM dbo.KnowHowToAI_SchemaMigration;";

        var result = new Dictionary<int, byte[]>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var version = reader.GetInt32(0);
            var checksum = (byte[])reader.GetValue(1);
            result[version] = checksum;
        }
        return result;
    }

    private async Task ApplyMigrationAsync(
        SqlConnection connection, MigrationScript script, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Wende Migration {Version} '{Name}' an...", script.Version, script.Name);

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Ein fehlerhaftes Skript muss die gesamte kurzen Migrations-Transaction
            // zurücksetzen, auch wenn das Skript selbst diese Session-Option vergisst.
            await ExecuteScriptAsync(connection, transaction, "SET XACT_ABORT ON;", cancellationToken).ConfigureAwait(false);

            // SQL-Skript ausführen
            await ExecuteScriptAsync(connection, transaction, script.Content, cancellationToken).ConfigureAwait(false);

            // Journal-Eintrag einfügen
            await InsertJournalEntryAsync(connection, transaction, script, cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Migration {Version} '{Name}' erfolgreich abgeschlossen.", script.Version, script.Name);
        }
        catch (Exception ex)
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogWarning(
                    "Rollback für Migration {Version} '{Name}' konnte nicht ausgeführt werden (SQL-Fehlercode {DatabaseErrorNumber}).",
                    script.Version,
                    script.Name,
                    (rollbackEx as SqlException)?.Number);
            }

            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
                throw;

            var databaseErrorNumber = (ex as SqlException)?.Number;
            _logger.LogError(
                "Migration {Version} '{Name}' ist mit Fehlercode {ErrorCode} und SQL-Fehlercode {DatabaseErrorNumber} fehlgeschlagen.",
                script.Version,
                script.Name,
                MigrationFailedException.ErrorCode,
                databaseErrorNumber);
            throw new MigrationFailedException(script.Name, script.Version, databaseErrorNumber);
        }
    }

    private async Task ExecuteScriptAsync(
        SqlConnection connection, SqlTransaction? transaction, string content, CancellationToken cancellationToken)
    {
        // GO-Batches aufteilen (SQL Server erlaubt GO nicht in ExecuteNonQuery)
        var batches = SplitBatches(content);
        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
                continue;

            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = batch;
            cmd.CommandTimeout = _storagePolicy.CommandTimeoutSeconds;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task InsertJournalEntryAsync(
        SqlConnection connection, SqlTransaction transaction, MigrationScript script, CancellationToken cancellationToken)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandTimeout = _storagePolicy.CommandTimeoutSeconds;
        cmd.CommandText = """
            INSERT INTO dbo.KnowHowToAI_SchemaMigration (Version, Name, ChecksumSha256)
            VALUES (@version, @name, @checksum);
            """;
        cmd.Parameters.AddWithValue("@version", script.Version);
        cmd.Parameters.AddWithValue("@name", script.Name);
        cmd.Parameters.Add("@checksum", System.Data.SqlDbType.Binary, 32).Value = script.ChecksumSha256;
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Teilt SQL-Skript an GO-Anweisungen in Batches auf.
    /// GO ist kein SQL-Schlüsselwort, sondern eine SSMS/sqlcmd-Direktive.
    /// </summary>
    private static IEnumerable<string> SplitBatches(string sql)
    {
        // Einfache zeilenbasierte GO-Erkennung (alleinstehend auf einer Zeile, case-insensitiv)
        var lines = sql.Split('\n');
        var batch = new System.Text.StringBuilder();
        foreach (var line in lines)
        {
            if (line.TrimEnd('\r').Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                yield return batch.ToString();
                batch.Clear();
            }
            else
            {
                batch.AppendLine(line);
            }
        }
        if (batch.Length > 0)
            yield return batch.ToString();
    }
}
