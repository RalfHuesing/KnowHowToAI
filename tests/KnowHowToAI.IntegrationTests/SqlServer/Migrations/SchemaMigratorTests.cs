using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Migrations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowHowToAI.IntegrationTests.SqlServer.Migrations;

/// <summary>
/// Integrationsnachweise für M1.3 (Migration Runner) und M1.5 (Integrationsnachweise)
/// gegen einen echten SQL Server. Verbindung ausschließlich über
/// <see cref="SqlTestDatabase.ConnectionStringEnvVar"/>.
/// </summary>
[Trait("Category", "Integration")]
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
        await using var db = await SqlTestDatabase.CreateAsync();
        var (migrator, catalog) = BuildMigrator(db);

        var applied = await migrator.MigrateAsync();

        Assert.Equal(catalog.Scripts.Count, applied);
        await AssertJournalHasEntriesAsync(db, catalog.Scripts.Count);
        await AssertSeedTableExistsAsync(db);
    }

    // ─── M1.5 Nachweis 2: Zweiter Lauf ist ohne Schemaänderung erfolgreich ────

    [Fact]
    public async Task SecondRun_IsIdempotent()
    {
        await using var db = await SqlTestDatabase.CreateAsync();
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
        await using var db = await SqlTestDatabase.CreateAsync();
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
        await using var db = await SqlTestDatabase.CreateAsync();

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
        await using var db = await SqlTestDatabase.CreateAsync();
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

    private static async Task AssertSeedTableExistsAsync(SqlTestDatabase db)
    {
        // Prüft, dass das Seed-Skript ausgeführt wurde (SystemState-Tabelle vorhanden und befüllt)
        await using var conn = await db.ConnectionFactory.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM dbo.KnowHowToAI_SystemState;";
        var count = (int)(await cmd.ExecuteScalarAsync())!;
        Assert.True(count > 0, "Seed-Skript muss mindestens einen SystemState-Eintrag erzeugt haben.");
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
        Assert.Null(objectId);
    }
}
