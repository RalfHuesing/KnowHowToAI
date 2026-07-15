using KnowHowToAI.Core.Migrations;

namespace KnowHowToAI.Core.Tests;

public class SchemaMigratorTests
{
    [Fact]
    public void DiscoverScripts_FindsEmbeddedScript()
    {
        var scripts = SchemaMigrator.DiscoverScripts("documents");

        Assert.Collection(scripts,
            script => Assert.EndsWith("0001_create_documents_table.sql", script.Name, StringComparison.Ordinal));
    }

    [Fact]
    public void DiscoverScripts_SubstitutesConfiguredTableName()
    {
        var scripts = SchemaMigrator.DiscoverScripts("sage100_documents");

        Assert.Contains("CREATE TABLE dbo.sage100_documents", scripts[0].Sql, StringComparison.Ordinal);
        Assert.DoesNotContain("{{DocumentsTableName}}", scripts[0].Sql, StringComparison.Ordinal);
    }
}
