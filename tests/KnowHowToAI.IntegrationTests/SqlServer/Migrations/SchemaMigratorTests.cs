using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Migrations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowHowToAI.IntegrationTests.SqlServer.Migrations;

/// <summary>
/// Integrationsnachweise für M1.3 (Migration Runner) und M1.5 (Integrationsnachweise)
/// gegen einen echten SQL Server mit der Appsettings-Sektion <c>DatabaseConnection</c>.
/// </summary>
[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed class SqlSchemaMigratorTests
{
    // ─── Helper ────────────────────────────────────────────────────────────────

    private static (SqlSchemaMigrator migrator, EmbeddedMigrationCatalog catalog) BuildMigrator(
        SqlTestDatabase db,
        EmbeddedMigrationCatalog? catalog = null)
    {
        catalog ??= new EmbeddedMigrationCatalog();
        var storagePolicy = new SqlStoragePolicy { CommandTimeoutSeconds = 30 };
        var policy = new MigrationPolicy { LockTimeoutSeconds = 30, ApplyOnStartup = true };
        var migrator = new SqlSchemaMigrator(
            db.ConnectionFactory,
            storagePolicy,
            policy,
            catalog,
            NullLogger<SqlSchemaMigrator>.Instance);
        return (migrator, catalog);
    }

    // ─── M1.5 Nachweis 1: Frische Datenbank wird vollständig erstellt und geseedet ───

    [Fact]
    public async Task FreshDatabase_AppliesAllMigrations()
    {
        await using var db = await SqlTestDatabase.ConnectFreshAsync();
        var (migrator, catalog) = BuildMigrator(db);

        var applied = await migrator.MigrateAsync();

        Assert.Equal(catalog.Scripts.Count, applied);
        await AssertJournalHasEntriesAsync(db, catalog.Scripts.Count);
        await AssertExpectedSchemaAsync(db);
        await AssertInitialStateIsSeededAsync(db);
    }

    // ─── M1.5 Nachweis 2: Zweiter Lauf ist ohne Schemaänderung erfolgreich ────

    [Fact]
    public async Task SecondRun_IsIdempotent()
    {
        await using var db = await SqlTestDatabase.ConnectFreshAsync();
        var (migrator, _) = BuildMigrator(db);

        var firstRun = await migrator.MigrateAsync();
        var secondRun = await migrator.MigrateAsync();

        Assert.True(firstRun > 0, "Erster Lauf muss Migrationen anwenden.");
        Assert.Equal(0, secondRun);
    }

    // ─── M1.5 Nachweis 3: Geänderte Checksum wird abgelehnt ─────────────────

    [Fact]
    public async Task ModifiedChecksum_IsRejected()
    {
        await using var db = await SqlTestDatabase.ConnectFreshAsync();
        var (migrator, catalog) = BuildMigrator(db);

        // Ersten Lauf erfolgreich abschließen
        await migrator.MigrateAsync();

        // Einen Journal-Eintrag mit falscher Checksum korrumpieren
        var firstScript = catalog.Scripts[0];
        var corruptedChecksum = new byte[32]; // Null-Bytes ≠ echte Checksum
        await CorruptJournalChecksumAsync(db, firstScript.Version, corruptedChecksum);

        // Zweiter Lauf muss die Abweichung erkennen
        var ex = await Assert.ThrowsAsync<MigrationChecksumMismatchException>(
            () => migrator.MigrateAsync());

        Assert.Equal(firstScript.Version, ex.Version);
        Assert.Contains(firstScript.Name, ex.Message, StringComparison.Ordinal);
    }

    // ─── M1.5 Nachweis 4: Parallele Runner wenden jede Migration genau einmal an ───

    [Fact]
    public async Task ParallelRunners_ApplyEachMigrationExactlyOnce()
    {
        await using var db = await SqlTestDatabase.ConnectFreshAsync();

        // Mehrere Runner parallel starten
        const int runnerCount = 4;
        var tasks = Enumerable.Range(0, runnerCount)
            .Select(_ =>
            {
                var (migrator, _) = BuildMigrator(db);
                return migrator.MigrateAsync();
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Gesamtanzahl angewandter Migrationen über alle Runner = Anzahl der Skripte
        var catalog = new EmbeddedMigrationCatalog();
        Assert.Equal(catalog.Scripts.Count, results.Sum());

        // Journal enthält jede Version genau einmal
        await AssertJournalHasEntriesAsync(db, catalog.Scripts.Count);
        await AssertNoDuplicateVersionsInJournalAsync(db);
    }

    // ─── M1.5 Nachweis 5: Fehler in einer Migration hinterlässt weder Journal noch Teilschema ───

    [Fact]
    public async Task FailedMigration_LeavesNoJournalEntryOrPartialSchema()
    {
        await using var db = await SqlTestDatabase.ConnectFreshAsync();
        var bootstrap = new EmbeddedMigrationCatalog().BootstrapScript;
        var failingScript = MigrationScript.Create(
            1,
            "0001_create_then_fail.sql",
            """
            CREATE TABLE dbo.KnowHowToAI_RollbackProbe (Id INT NOT NULL);
            GO
            THROW 50001, 'Absichtlicher Migrationstestfehler.', 1;
            """);
        var catalog = new EmbeddedMigrationCatalog([bootstrap, failingScript]);
        var (migrator, _) = BuildMigrator(db, catalog);

        var exception = await Assert.ThrowsAsync<MigrationFailedException>(() => migrator.MigrateAsync());

        Assert.Equal(failingScript.Name, exception.ScriptName);
        Assert.Equal(failingScript.Version, exception.Version);
        Assert.Equal(50001, exception.DatabaseErrorNumber);
        await AssertJournalHasEntriesAsync(db, expectedCount: 0);
        await AssertTableDoesNotExistAsync(db, "KnowHowToAI_RollbackProbe");
    }

    // ─── Private Helpers ───────────────────────────────────────────────────────

    private static async Task AssertJournalHasEntriesAsync(SqlTestDatabase db, int expectedCount)
    {
        await using var conn = await db.ConnectionFactory.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM dbo.KnowHowToAI_SchemaMigration;";
        var actual = (int)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(expectedCount, actual);
    }

    private static async Task AssertExpectedSchemaAsync(SqlTestDatabase db)
    {
        await using var conn = await db.ConnectionFactory.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT tableInfo.name
            FROM sys.tables AS tableInfo
            INNER JOIN sys.schemas AS schemaInfo ON schemaInfo.schema_id = tableInfo.schema_id
            WHERE schemaInfo.name = N'dbo' AND tableInfo.name LIKE N'KnowHowToAI[_]%'
            ORDER BY tableInfo.name;
            """;

        var tables = await ReadStringsAsync(cmd);
        Assert.Equal(
            [
                "KnowHowToAI_ContentDependency",
                "KnowHowToAI_Node",
                "KnowHowToAI_NodeContent",
                "KnowHowToAI_Release",
                "KnowHowToAI_Role",
                "KnowHowToAI_RoleResolution",
                "KnowHowToAI_SchemaMigration",
                "KnowHowToAI_Snapshot",
                "KnowHowToAI_SystemState",
                "KnowHowToAI_Transaction"
            ],
            tables);

        cmd.CommandText = """
            SELECT indexInfo.name
            FROM sys.indexes AS indexInfo
            INNER JOIN sys.tables AS tableInfo ON tableInfo.object_id = indexInfo.object_id
            INNER JOIN sys.schemas AS schemaInfo ON schemaInfo.schema_id = tableInfo.schema_id
            WHERE schemaInfo.name = N'dbo'
              AND tableInfo.name LIKE N'KnowHowToAI[_]%'
              AND indexInfo.name LIKE N'%[_]KnowHowToAI[_]%'
              AND indexInfo.is_primary_key = 0
              AND indexInfo.is_unique_constraint = 0
            ORDER BY indexInfo.name;
            """;

        var indexes = await ReadStringsAsync(cmd);
        Assert.Equal(
            [
                "IX_KnowHowToAI_ContentDependency_Source",
                "IX_KnowHowToAI_Node_NodeId",
                "IX_KnowHowToAI_NodeContent_Revision",
                "IX_KnowHowToAI_NodeContent_Role",
                "IX_KnowHowToAI_Release_Snapshot",
                "IX_KnowHowToAI_Role_RoleId",
                "IX_KnowHowToAI_RoleResolution_Candidate",
                "IX_KnowHowToAI_Snapshot_BaseSnapshot",
                "IX_KnowHowToAI_Snapshot_State",
                "IX_KnowHowToAI_Transaction_State",
                "UQ_KnowHowToAI_Node_ActiveRoot",
                "UQ_KnowHowToAI_Node_ActiveSiblingSortOrder"
            ],
            indexes);
    }

    private static async Task AssertInitialStateIsSeededAsync(SqlTestDatabase db)
    {
        await using var conn = await db.ConnectionFactory.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*)
            FROM dbo.KnowHowToAI_SystemState AS systemState
            INNER JOIN dbo.KnowHowToAI_Snapshot AS snapshot
                ON snapshot.SnapshotId = systemState.CurrentSnapshotId
            INNER JOIN dbo.KnowHowToAI_Role AS roleInfo
                ON roleInfo.SnapshotId = snapshot.SnapshotId
            INNER JOIN dbo.KnowHowToAI_RoleResolution AS resolution
                ON resolution.SnapshotId = snapshot.SnapshotId
                AND resolution.RequestedRoleId = roleInfo.RoleId
            WHERE systemState.Id = 1
              AND snapshot.State = 'Committed'
              AND snapshot.BaseSnapshotId IS NULL
              AND snapshot.CommittedAtUtc IS NOT NULL
              AND roleInfo.RoleId = N'Default'
              AND roleInfo.Name = N'Default'
              AND roleInfo.IsDeleted = 0
              AND resolution.CandidateRoleId = N'Default'
              AND resolution.Priority = 1;
            """;
        var matchingSeedStates = (int)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(1, matchingSeedStates);
    }

    private static async Task AssertNoDuplicateVersionsInJournalAsync(SqlTestDatabase db)
    {
        await using var conn = await db.ConnectionFactory.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM (
                SELECT Version FROM dbo.KnowHowToAI_SchemaMigration
                GROUP BY Version HAVING COUNT(*) > 1
            ) AS Dups;
            """;
        var duplicates = (int)(await cmd.ExecuteScalarAsync())!;
        Assert.Equal(0, duplicates);
    }

    private static async Task CorruptJournalChecksumAsync(SqlTestDatabase db, int version, byte[] corruptedChecksum)
    {
        await using var conn = await db.ConnectionFactory.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE dbo.KnowHowToAI_SchemaMigration SET ChecksumSha256 = @cs WHERE Version = @v;";
        cmd.Parameters.Add("@cs", System.Data.SqlDbType.Binary, 32).Value = corruptedChecksum;
        cmd.Parameters.AddWithValue("@v", version);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task AssertTableDoesNotExistAsync(SqlTestDatabase db, string tableName)
    {
        await using var conn = await db.ConnectionFactory.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT OBJECT_ID(@tableName, N'U');";
        cmd.Parameters.AddWithValue("@tableName", $"dbo.{tableName}");
        var objectId = await cmd.ExecuteScalarAsync();
        Assert.True(objectId is null or DBNull, $"Tabelle '{tableName}' darf nach dem Rollback nicht bestehen.");
    }

    private static async Task<List<string>> ReadStringsAsync(Microsoft.Data.SqlClient.SqlCommand command)
    {
        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            values.Add(reader.GetString(0));
        return values;
    }
}
