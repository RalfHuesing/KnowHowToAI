using Microsoft.Data.SqlClient;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.IntegrationTests.TestSupport;

[Trait("Category", "Unit")]
public sealed class SqlIntegrationTestSettingsTests
{
    [Fact]
    public void Load_BindsBrowserTestDatabaseConnectionAndResolvesComputerName()
    {
        var settings = SqlIntegrationTestSettings.Load();

        Assert.Equal($"{Environment.MachineName}\\MSSQLSERVER2022", settings.Server);
        Assert.Equal("KnowHowToAi_BrowserTests", settings.Database);
        Assert.Equal("KnowHowToAi", settings.UserName);
        Assert.False(settings.UseWindowsAuthentication);
    }

    [Fact]
    public void ResolveServer_WithUnknownPlaceholder_ReportsPreflightErrorWithoutConfigurationValues()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => SqlIntegrationTestSettings.ResolveServer("%KHTOAI_UNKNOWN_COMPUTER%\\MSSQLSERVER2022"));

        Assert.Contains("Preflight-Fehler", exception.Message);
        Assert.DoesNotContain("KHTOAI_UNKNOWN_COMPUTER", exception.Message);
    }

    [Fact]
    public void CreateDatabaseConnectionString_UsesConfiguredDatabaseAndTrustsTheLocalDevelopmentCertificate()
    {
        var connectionString = SqlIntegrationTestSettings.Load().CreateDatabaseConnectionString();
        var builder = new SqlConnectionStringBuilder(connectionString);

        Assert.Equal("KnowHowToAi_BrowserTests", builder.InitialCatalog);
        Assert.True(builder.TrustServerCertificate);
    }

    [Fact]
    public void ManualDatabaseIntegrationCannotUseTheProductDatabaseSection()
    {
        Assert.Equal("BrowserTestDatabaseConnection", SqlIntegrationTestSettings.SectionName);
        Assert.NotEqual("DatabaseConnection", SqlIntegrationTestSettings.SectionName);
    }

    [Fact]
    public void CleanupGuardRejectsAProductTargetEvenWithNormalizedServerAndDatabaseNames()
    {
        var product = new SqlCleanupTarget("tcp:SqlHost.", "KnowHowToAi");
        var browser = new SqlCleanupTarget("SQLHOST", " knowhowtoai ");

        var exception = Assert.Throws<InvalidOperationException>(
            () => SqlCleanupTargetGuard.ValidateAndDedupe(product, [browser]));

        Assert.Contains("Produktverbindung", exception.Message);
    }

    [Theory]
    [InlineData("master")]
    [InlineData("MODEL")]
    [InlineData(" msdb ")]
    [InlineData("tempdb")]
    public void CleanupGuardRejectsSystemDatabases(string database)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => SqlCleanupTargetGuard.ValidateAndDedupe(
                new SqlCleanupTarget("SqlHost", "KnowHowToAi"),
                [new SqlCleanupTarget("SqlHost", database)]));

        Assert.Contains("Systemdatenbank", exception.Message);
    }

    [Fact]
    public void CleanupGuardDeduplicatesIdenticalBrowserTargets()
    {
        var targets = SqlCleanupTargetGuard.ValidateAndDedupe(
            new SqlCleanupTarget("SqlHost", "KnowHowToAi"),
            [
                new SqlCleanupTarget("SqlHost", "KnowHowToAi_BrowserTests"),
                new SqlCleanupTarget(" tcp:SQLHOST. ", "knowhowtoai_browsertests ")
            ]);

        var target = Assert.Single(targets);
        Assert.Equal("KnowHowToAi_BrowserTests", target.Database);
    }
}
