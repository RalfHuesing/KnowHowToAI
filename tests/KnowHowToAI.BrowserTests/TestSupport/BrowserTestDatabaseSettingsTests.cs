using KnowHowToAI.TestSupport;
using Microsoft.Extensions.Configuration;

namespace KnowHowToAI.BrowserTests.TestSupport;

[Trait("Category", "Integration")]
public sealed class BrowserTestDatabaseSettingsTests
{
    [Fact]
    public void LoadWorkflow_ReadsTheDedicatedManuallyProvisionedWorkflowDatabase()
    {
        var settings = BrowserTestDatabaseSettings.LoadWorkflow(TestRepositoryRoot.Resolve());

        Assert.Equal("KnowHowToAi_BrowserTests", settings.Database);
        Assert.False(settings.UseWindowsAuthentication);
        Assert.False(string.IsNullOrWhiteSpace(settings.Server));
        Assert.False(string.IsNullOrWhiteSpace(settings.UserName));
        Assert.False(string.IsNullOrWhiteSpace(settings.Password));
    }

    [Fact]
    public void LoadVisualShell_ReadsTheDedicatedMinimalVisualDatabase()
    {
        var settings = BrowserTestDatabaseSettings.LoadVisualShell(TestRepositoryRoot.Resolve());

        Assert.Equal("KnowHowToAi_Test", settings.Database);
        Assert.False(settings.UseWindowsAuthentication);
        Assert.False(string.IsNullOrWhiteSpace(settings.Server));
        Assert.False(string.IsNullOrWhiteSpace(settings.UserName));
        Assert.False(string.IsNullOrWhiteSpace(settings.Password));
    }

    [Fact]
    public void BrowserCleanupSectionsCannotResolveTheProductDatabaseSection()
    {
        Assert.NotEqual("DatabaseConnection", BrowserTestDatabaseSettings.WorkflowSectionName);
        Assert.NotEqual("DatabaseConnection", BrowserTestDatabaseSettings.VisualShellSectionName);
        Assert.NotEqual(
            BrowserTestDatabaseSettings.WorkflowSectionName,
            BrowserTestDatabaseSettings.VisualShellSectionName);
    }

    [Fact]
    public void FromConfiguration_RejectsMissingSqlUserWithoutExposingConfiguredPassword()
    {
        const string password = "browser-test-secret";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{BrowserTestDatabaseSettings.WorkflowSectionName}:Server"] = "sqlserver",
                [$"{BrowserTestDatabaseSettings.WorkflowSectionName}:Database"] = "KnowHowToAi_BrowserTests",
                [$"{BrowserTestDatabaseSettings.WorkflowSectionName}:UserName"] = string.Empty,
                [$"{BrowserTestDatabaseSettings.WorkflowSectionName}:Password"] = password,
                [$"{BrowserTestDatabaseSettings.WorkflowSectionName}:UseWindowsAuthentication"] = "false"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => BrowserTestDatabaseSettings.FromConfiguration(
                configuration,
                BrowserTestDatabaseSettings.WorkflowSectionName));

        Assert.Contains("UserName", exception.Message);
        Assert.DoesNotContain(password, exception.Message);
    }
}
