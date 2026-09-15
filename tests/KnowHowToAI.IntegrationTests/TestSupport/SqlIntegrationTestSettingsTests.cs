using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.TestSupport;

[Trait("Category", "Unit")]
public sealed class SqlIntegrationTestSettingsTests
{
    [Fact]
    public void Load_BindsDatabaseConnectionAndResolvesComputerName()
    {
        var settings = SqlIntegrationTestSettings.Load();

        Assert.Equal($"{Environment.MachineName}\\MSSQLSERVER2022", settings.Server);
        Assert.Equal("KnowHowToAi", settings.Database);
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
    public void CreateMasterConnectionString_TrustsTheLocalDevelopmentCertificate()
    {
        var connectionString = SqlIntegrationTestSettings.Load().CreateMasterConnectionString();
        var builder = new SqlConnectionStringBuilder(connectionString);

        Assert.True(builder.TrustServerCertificate);
    }
}
