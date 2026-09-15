namespace KnowHowToAI.IntegrationTests.TestSupport;

/// <summary>
/// Belegt vor den eigentlichen SQL-Tests, dass die manuell bereitgestellte Datenbank
/// mit der dokumentierten App-Konfiguration erreichbar ist.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SqlIntegrationPreflightTests
{
    [Fact]
    public async Task DatabaseConnection_ConnectsToTheManuallyProvisionedDatabase()
    {
        await using var database = await SqlTestDatabase.ConnectAsync();

        Assert.Equal("KnowHowToAi", database.DatabaseName);
    }

    [Fact]
    public async Task DatabaseConnection_TargetsSqlServer2019OrLater()
    {
        await using var database = await SqlTestDatabase.ConnectAsync();

        var majorVersion = await database.GetSqlServerMajorVersionAsync();

        Assert.True(majorVersion >= 15, $"Preflight-Fehler: SQL Server >= 2019 wird benötigt; gefunden wurde Hauptversion {majorVersion}.");
    }
}
