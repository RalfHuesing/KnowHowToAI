using KnowHowToAI.TestSupport;
using Microsoft.Extensions.Configuration;

namespace KnowHowToAI.BrowserTests.TestSupport;

[Trait("Category", "Integration")]
public sealed class BrowserTestDatabaseSettingsTests
{
    [Fact]
    public void Load_ReadsTheDedicatedManuallyProvisionedBrowserDatabase()
    {
        var settings = BrowserTestDatabaseSettings.Load(TestRepositoryRoot.Resolve());

        Assert.Equal("KnowHowToAi_Test", settings.Database);
        Assert.False(settings.UseWindowsAuthentication);
        Assert.False(string.IsNullOrWhiteSpace(settings.Server));
        Assert.False(string.IsNullOrWhiteSpace(settings.UserName));
        Assert.False(string.IsNullOrWhiteSpace(settings.Password));
    }

    [Fact]
    public void FromConfiguration_RejectsMissingSqlUserWithoutExposingConfiguredPassword()
    {
        const string password = "browser-test-secret";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{BrowserTestDatabaseSettings.SectionName}:Server"] = "sqlserver",
                [$"{BrowserTestDatabaseSettings.SectionName}:Database"] = "KnowHowToAi_Test",
                [$"{BrowserTestDatabaseSettings.SectionName}:UserName"] = string.Empty,
                [$"{BrowserTestDatabaseSettings.SectionName}:Password"] = password,
                [$"{BrowserTestDatabaseSettings.SectionName}:UseWindowsAuthentication"] = "false"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => BrowserTestDatabaseSettings.FromConfiguration(configuration));

        Assert.Contains("UserName", exception.Message);
        Assert.DoesNotContain(password, exception.Message);
    }
}
