using System.Reflection;
using DbUp;
using DbUp.Engine;
using DbUp.Engine.Output;

namespace KnowHowToAI.Core.Migrations;

// Führt sql-scripts/*.sql (embedded, siehe .csproj) gegen den konfigurierten SQL Server aus.
// IUpgradeLog statt eigener Logging-Abhängigkeit: docs/04-Datenmodell-Validierung-Edgecases.md, Abschnitt 1.
public static class SchemaMigrator
{
    public static DatabaseUpgradeResult Migrate(string connectionString, IUpgradeLog upgradeLog)
    {
        var upgrader = BuildUpgrader(connectionString, upgradeLog);
        return upgrader.PerformUpgrade();
    }

    public static IReadOnlyList<SqlScript> DiscoverScripts(string connectionString, IUpgradeLog upgradeLog)
    {
        var upgrader = BuildUpgrader(connectionString, upgradeLog);
        return upgrader.GetDiscoveredScripts();
    }

    private static UpgradeEngine BuildUpgrader(string connectionString, IUpgradeLog upgradeLog) =>
        DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogTo(upgradeLog)
            .Build();
}
