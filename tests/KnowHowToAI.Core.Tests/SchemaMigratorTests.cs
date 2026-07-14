using DbUp.Engine.Output;
using KnowHowToAI.Core.Migrations;

namespace KnowHowToAI.Core.Tests;

public class SchemaMigratorTests
{
    // GetDiscoveredScripts liest nur die embedded Resources aus, ohne die DB-Verbindung zu öffnen -
    // die Connection-String hier wird daher nie tatsächlich benutzt.
    private const string UnusedConnectionString = "Server=unused;Database=unused;";

    [Fact]
    public void DiscoverScripts_FindsEmbeddedScript()
    {
        var scripts = SchemaMigrator.DiscoverScripts(UnusedConnectionString, new NoOpUpgradeLog());

        Assert.Collection(scripts,
            script => Assert.EndsWith("0001_create_documents_table.sql", script.Name, StringComparison.Ordinal));
    }

    [Fact]
    public void DiscoverScripts_EmbedsExpectedSqlContent()
    {
        var scripts = SchemaMigrator.DiscoverScripts(UnusedConnectionString, new NoOpUpgradeLog());

        Assert.Contains("CREATE TABLE dbo.documents", scripts[0].Contents, StringComparison.Ordinal);
    }

    private sealed class NoOpUpgradeLog : IUpgradeLog
    {
        public void LogDebug(string format, params object[] args) { }
        public void LogInformation(string format, params object[] args) { }
        public void LogTrace(string format, params object[] args) { }
        public void LogWarning(string format, params object[] args) { }
        public void LogError(string format, params object[] args) { }
        public void LogError(Exception ex, string format, params object[] args) { }
    }
}
