using KnowHowToAI.Server.Configuration;
using KnowHowToAI.Server.Hosting;
using KnowHowToAI.Storage.SqlServer.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KnowHowToAI.IntegrationTests.Server.Hosting;

/// <summary>
/// Integrationstests für Konfigurationsbindung, Override-Reihenfolge,
/// Fail-fast-Validierung bei ungültigen Werten und Secret-Redaction.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ConfigurationTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static IHost BuildHostWithEnv(Dictionary<string, string?> env)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                // Nur die produktive appsettings.json laden; keine User Secrets im Test.
                // Environment-Variablen werden von CreateDefaultBuilder automatisch geladen.
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddKnowHowToAIOptions(ctx.Configuration);
                services.AddSqlStorage();
            })
            .Build();
        return host;
    }

    private static IHost BuildHostWithOverrides(Dictionary<string, string?> inMemoryConfig)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(inMemoryConfig);
            })
            .ConfigureServices((ctx, services) =>
                services.AddKnowHowToAIOptions(ctx.Configuration))
            .Build();
    }

    // ---------------------------------------------------------------------------
    // Defaults aus appsettings.json
    // ---------------------------------------------------------------------------

    [Fact]
    public void Defaults_BindCorrectly_FromAppsettings()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices((ctx, services) =>
                services.AddKnowHowToAIOptions(ctx.Configuration))
            .Build();

        var options = host.Services.GetRequiredService<IOptions<KnowHowToAIOptions>>().Value;

        // Validation
        Assert.Equal(4096, options.Validation.ContentSizeWarningBytes);
        Assert.Equal(25, options.Validation.ChildCountWarning);
        Assert.Equal(8, options.Validation.HierarchyDepthWarning);
        Assert.True(options.Validation.PossibleEmbeddedHeadingWarning);

        // Retrieval
        Assert.Equal(20, options.Retrieval.DefaultPageSize);
        Assert.Equal(100, options.Retrieval.MaximumPageSize);
        Assert.Equal(10, options.Retrieval.SearchPageSize);
        Assert.Equal(50, options.Retrieval.SearchMaximumPageSize);
        Assert.Equal(300, options.Retrieval.SnippetMaximumCharacters);

        // Storage
        Assert.Equal(30, options.Storage.CommandTimeoutSeconds);

        // Migrations
        Assert.Equal(60, options.Migrations.LockTimeoutSeconds);
        Assert.True(options.Migrations.ApplyOnStartup);
    }

    [Fact]
    public void DatabaseConnection_IsBoundAndConvertedForSqlStorage()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices((ctx, services) =>
            {
                services.AddKnowHowToAIOptions(ctx.Configuration);
                services.AddSqlStorage();
            })
            .Build();

        var connection = host.Services.GetRequiredService<SqlStorageConnectionString>();
        var builder = new SqlConnectionStringBuilder(connection.Value);

        Assert.Equal(Environment.ExpandEnvironmentVariables("%COMPUTERNAME%\\MSSQLSERVER2022"), builder.DataSource);
        Assert.Equal("KnowHowToAi", builder.InitialCatalog);
        Assert.False(builder.IntegratedSecurity);
    }

    // ---------------------------------------------------------------------------
    // Override via In-Memory-Konfiguration (entspricht Environment-Variablen)
    // ---------------------------------------------------------------------------

    [Fact]
    public void Override_MaximumPageSize_IsApplied()
    {
        using var host = BuildHostWithOverrides(new()
        {
            ["KnowHowToAI:Retrieval:MaximumPageSize"] = "200",
            // DefaultPageSize muss <= MaximumPageSize bleiben:
            ["KnowHowToAI:Retrieval:DefaultPageSize"] = "50"
        });

        var options = host.Services.GetRequiredService<IOptions<KnowHowToAIOptions>>().Value;

        Assert.Equal(200, options.Retrieval.MaximumPageSize);
        Assert.Equal(50, options.Retrieval.DefaultPageSize);
    }

    [Fact]
    public void Override_ContentSizeWarningBytes_IsApplied()
    {
        using var host = BuildHostWithOverrides(new()
        {
            ["KnowHowToAI:Validation:ContentSizeWarningBytes"] = "8192"
        });

        var options = host.Services.GetRequiredService<IOptions<KnowHowToAIOptions>>().Value;

        Assert.Equal(8192, options.Validation.ContentSizeWarningBytes);
    }

    // ---------------------------------------------------------------------------
    // Fail-fast bei ungültigen Werten
    // ---------------------------------------------------------------------------

    [Fact]
    public void InvalidContentSizeWarningBytes_TooLow_ThrowsOnValidation()
    {
        using var host = BuildHostWithOverrides(new()
        {
            ["KnowHowToAI:Validation:ContentSizeWarningBytes"] = "100" // unter Minimum 512
        });

        var ex = Assert.Throws<OptionsValidationException>(() =>
            host.Services.GetRequiredService<IOptions<KnowHowToAIOptions>>().Value);

        Assert.Contains("ContentSizeWarningBytes", ex.Message);
    }

    [Fact]
    public void InvalidDefaultPageSize_ExceedsMaximumPageSize_ThrowsOnValidation()
    {
        using var host = BuildHostWithOverrides(new()
        {
            ["KnowHowToAI:Retrieval:DefaultPageSize"] = "500",  // > MaximumPageSize 100
        });

        var ex = Assert.Throws<OptionsValidationException>(() =>
            host.Services.GetRequiredService<IOptions<KnowHowToAIOptions>>().Value);

        Assert.Contains("DefaultPageSize", ex.Message);
        Assert.Contains("MaximumPageSize", ex.Message);
    }

    [Fact]
    public void InvalidCommandTimeoutSeconds_Zero_ThrowsOnValidation()
    {
        using var host = BuildHostWithOverrides(new()
        {
            ["KnowHowToAI:Storage:CommandTimeoutSeconds"] = "0"
        });

        var ex = Assert.Throws<OptionsValidationException>(() =>
            host.Services.GetRequiredService<IOptions<KnowHowToAIOptions>>().Value);

        Assert.Contains("CommandTimeoutSeconds", ex.Message);
    }

    [Fact]
    public void InvalidLockTimeoutSeconds_TooHigh_ThrowsOnValidation()
    {
        using var host = BuildHostWithOverrides(new()
        {
            ["KnowHowToAI:Migrations:LockTimeoutSeconds"] = "999" // über Maximum 600
        });

        var ex = Assert.Throws<OptionsValidationException>(() =>
            host.Services.GetRequiredService<IOptions<KnowHowToAIOptions>>().Value);

        Assert.Contains("LockTimeoutSeconds", ex.Message);
    }

    // ---------------------------------------------------------------------------
    // Secret-Redaction: Connection String darf in Validierungsfehlern nicht erscheinen
    // ---------------------------------------------------------------------------

    [Fact]
    public void ValidationError_DoesNotContainConnectionString()
    {
        // Simuliert eine Umgebung mit Connection String + ungültigem Wert.
        // Die Fehlermeldung des Validators darf den Connection String nicht wiederholen.
        using var host = BuildHostWithOverrides(new()
        {
            ["ConnectionStrings:KnowHowToAI"] = "Server=secret-host;Database=secret-db;Password=s3cr3t",
            ["KnowHowToAI:Storage:CommandTimeoutSeconds"] = "0"
        });

        var ex = Assert.Throws<OptionsValidationException>(() =>
            host.Services.GetRequiredService<IOptions<KnowHowToAIOptions>>().Value);

        Assert.DoesNotContain("secret-host", ex.Message);
        Assert.DoesNotContain("s3cr3t", ex.Message);
        Assert.DoesNotContain("Password", ex.Message);
    }
}
