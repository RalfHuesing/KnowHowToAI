namespace KnowHowToAI.IntegrationTests.TestSupport;

/// <summary>
/// Belegt vor den eigentlichen SQL-Tests die vollständige Voraussetzung einschließlich
/// der Berechtigung zum Erzeugen und sicheren Entfernen einer isolierten Testdatenbank.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SqlIntegrationPreflightTests
{
    [Fact]
    public async Task DatabaseConnection_CreatesAndRemovesAnIsolatedTestDatabase()
    {
        await using var database = await SqlTestDatabase.CreateAsync();

        Assert.StartsWith(SqlTestDatabase.DatabasePrefix, database.DatabaseName, StringComparison.Ordinal);
    }
}
