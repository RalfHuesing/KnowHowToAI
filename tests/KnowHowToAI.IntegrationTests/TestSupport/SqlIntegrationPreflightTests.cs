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
}
