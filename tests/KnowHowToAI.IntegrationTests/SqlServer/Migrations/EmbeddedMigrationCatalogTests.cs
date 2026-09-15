using KnowHowToAI.Storage.SqlServer.Migrations;
using System.Text;

namespace KnowHowToAI.IntegrationTests.SqlServer.Migrations;

/// <summary>
/// FastTests (SQL-frei) für EmbeddedMigrationCatalog und SqlSchemaMigrator-Hilfsmethoden.
/// Testen deterministische Logik: Normalisierung, Checksum, Sortierung und Duplikatschutz.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EmbeddedMigrationCatalogTests
{
    // ─── NormalizeLineEndings ─────────────────────────────────────────────────

    [Fact]
    public void NormalizeLineEndings_CrLf_IsConvertedToLf()
    {
        var input = "line1\r\nline2\r\nline3";
        var result = EmbeddedMigrationCatalog.NormalizeLineEndings(input);
        Assert.Equal("line1\nline2\nline3", result);
    }

    [Fact]
    public void NormalizeLineEndings_Cr_IsConvertedToLf()
    {
        var input = "line1\rline2";
        var result = EmbeddedMigrationCatalog.NormalizeLineEndings(input);
        Assert.Equal("line1\nline2", result);
    }

    [Fact]
    public void NormalizeLineEndings_Lf_IsUnchanged()
    {
        var input = "line1\nline2";
        var result = EmbeddedMigrationCatalog.NormalizeLineEndings(input);
        Assert.Equal("line1\nline2", result);
    }

    [Fact]
    public void NormalizeLineEndings_Empty_IsUnchanged()
    {
        var result = EmbeddedMigrationCatalog.NormalizeLineEndings(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    // ─── ComputeSha256 ────────────────────────────────────────────────────────

    [Fact]
    public void ComputeSha256_SameInput_ProducesSameHash()
    {
        var content = "SELECT 1;\nSELECT 2;";
        var hash1 = EmbeddedMigrationCatalog.ComputeSha256(content);
        var hash2 = EmbeddedMigrationCatalog.ComputeSha256(content);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeSha256_DifferentInput_ProducesDifferentHash()
    {
        var hash1 = EmbeddedMigrationCatalog.ComputeSha256("SELECT 1;");
        var hash2 = EmbeddedMigrationCatalog.ComputeSha256("SELECT 2;");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeSha256_Returns32Bytes()
    {
        var hash = EmbeddedMigrationCatalog.ComputeSha256("test");
        Assert.Equal(32, hash.Length);
    }

    [Fact]
    public void ComputeSha256_CrLfAndLf_ProduceDifferentHashes_BeforeNormalization()
    {
        // Zeigt, warum Normalisierung vor Checksum-Berechnung nötig ist
        var withCrLf = EmbeddedMigrationCatalog.ComputeSha256("line1\r\nline2");
        var withLf = EmbeddedMigrationCatalog.ComputeSha256("line1\nline2");
        Assert.NotEqual(withCrLf, withLf);
    }

    [Fact]
    public void ComputeSha256_NormalizedCrLfAndLf_ProduceSameHash()
    {
        var content1 = EmbeddedMigrationCatalog.NormalizeLineEndings("line1\r\nline2");
        var content2 = EmbeddedMigrationCatalog.NormalizeLineEndings("line1\nline2");
        var hash1 = EmbeddedMigrationCatalog.ComputeSha256(content1);
        var hash2 = EmbeddedMigrationCatalog.ComputeSha256(content2);
        Assert.Equal(hash1, hash2);
    }

    // ─── EmbeddedMigrationCatalog – echte Assembly ────────────────────────────

    [Fact]
    public void Catalog_LoadsBootstrapScript()
    {
        var catalog = new EmbeddedMigrationCatalog();
        Assert.NotNull(catalog.BootstrapScript);
        Assert.Equal(0, catalog.BootstrapScript.Version);
        Assert.Contains("KnowHowToAI_SchemaMigration", catalog.BootstrapScript.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_LoadsVersionedScriptsInOrder()
    {
        var catalog = new EmbeddedMigrationCatalog();
        Assert.NotEmpty(catalog.Scripts);

        // Strikt aufsteigend sortiert
        for (var i = 1; i < catalog.Scripts.Count; i++)
        {
            Assert.True(catalog.Scripts[i].Version > catalog.Scripts[i - 1].Version,
                $"Skripte müssen strikt aufsteigend sortiert sein: {catalog.Scripts[i - 1].Version} vor {catalog.Scripts[i].Version}");
        }
    }

    [Fact]
    public void Catalog_BootstrapNotInVersionedList()
    {
        var catalog = new EmbeddedMigrationCatalog();
        Assert.DoesNotContain(catalog.Scripts, s => s.Version == 0);
    }

    [Fact]
    public void Catalog_DuplicateVersion_IsRejected()
    {
        var bootstrap = MigrationScript.Create(0, "0000_bootstrap.sql", "SELECT 1;");
        var first = MigrationScript.Create(1, "0001_first.sql", "SELECT 1;");
        var second = MigrationScript.Create(1, "0001_second.sql", "SELECT 2;");

        var exception = Assert.Throws<InvalidOperationException>(
            () => new EmbeddedMigrationCatalog([bootstrap, first, second]));

        Assert.Contains("Doppelte Migrationsversionsnummer 1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_AllScriptsHave32ByteChecksum()
    {
        var catalog = new EmbeddedMigrationCatalog();
        foreach (var script in catalog.Scripts)
        {
            Assert.Equal(32, script.ChecksumSha256.Length);
        }
        Assert.Equal(32, catalog.BootstrapScript.ChecksumSha256.Length);
    }

    [Fact]
    public void Catalog_ChecksumIsDeterministic()
    {
        var catalog1 = new EmbeddedMigrationCatalog();
        var catalog2 = new EmbeddedMigrationCatalog();

        for (var i = 0; i < catalog1.Scripts.Count; i++)
        {
            Assert.Equal(catalog1.Scripts[i].ChecksumSha256, catalog2.Scripts[i].ChecksumSha256);
        }
    }

    [Fact]
    public void Catalog_ContentIsLfNormalized()
    {
        var catalog = new EmbeddedMigrationCatalog();
        foreach (var script in catalog.Scripts)
        {
            Assert.DoesNotContain("\r\n", script.Content, StringComparison.Ordinal);
            Assert.DoesNotContain('\r', script.Content);
        }
    }
}
