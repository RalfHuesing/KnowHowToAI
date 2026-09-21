using KnowHowToAI.TestSupport;

namespace KnowHowToAI.BrowserTests.TestSupport;

[Trait("Category", "Unit")]
public sealed class BrowserTestDatabaseCleanerTests
{
    [Fact]
    public async Task CleanSchemaAsync_RejectsProductIdentityBeforeOpeningSqlConnection()
    {
        var settings = CreateSettings("SqlHost", "KnowHowToAi");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => BrowserTestDatabaseCleaner.CleanSchemaAsync(
                settings,
                new SqlCleanupTarget("tcp:sqlhost.", " knowhowtoai ")));

        Assert.Contains("Produktverbindung", exception.Message);
    }

    [Theory]
    [InlineData("master")]
    [InlineData("MODEL")]
    [InlineData(" msdb ")]
    [InlineData("tempdb")]
    public async Task CleanSchemaAsync_RejectsSystemDatabaseBeforeOpeningSqlConnection(string database)
    {
        var settings = CreateSettings("SqlHost", database);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => BrowserTestDatabaseCleaner.CleanSchemaAsync(
                settings,
                new SqlCleanupTarget("OtherSqlHost", "KnowHowToAi")));

        Assert.Contains("Systemdatenbank", exception.Message);
    }

    private static BrowserTestDatabaseSettings CreateSettings(string server, string database) =>
        new()
        {
            Server = server,
            Database = database,
            UserName = string.Empty,
            Password = string.Empty,
            UseWindowsAuthentication = true
        };
}
