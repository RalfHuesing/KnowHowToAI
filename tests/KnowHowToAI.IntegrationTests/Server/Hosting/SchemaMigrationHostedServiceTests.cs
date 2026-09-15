using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Server.Configuration;
using KnowHowToAI.Server.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace KnowHowToAI.IntegrationTests.Server.Hosting;

/// <summary>
/// Belegt die Startreihenfolge: Schema-Migrationen laufen genau einmal vor der
/// Betriebsbereitschaft oder werden bei deaktivierter Policy vollständig übersprungen.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SchemaMigrationHostedServiceTests
{
    [Fact]
    public async Task StartAsync_AppliesOutstandingMigrationsExactlyOnce()
    {
        var migrator = new RecordingSchemaMigrator();
        using var host = BuildHost(migrator, applyOnStartup: true);

        await host.StartAsync();

        Assert.Equal(1, migrator.CallCount);
        await host.StopAsync();
    }

    [Fact]
    public async Task StartAsync_SkipsMigrationsWhenDisabled()
    {
        var migrator = new RecordingSchemaMigrator();
        using var host = BuildHost(migrator, applyOnStartup: false);

        await host.StartAsync();

        Assert.Equal(0, migrator.CallCount);
        await host.StopAsync();
    }

    [Fact]
    public async Task StartAsync_RedactsCredentialsFromMigrationFailure()
    {
        const string secret = "correct-horse-battery-staple";
        var migrator = new RecordingSchemaMigrator(new InvalidOperationException($"Password={secret}"));
        using var host = BuildHost(migrator, applyOnStartup: true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

        Assert.Equal(1, migrator.CallCount);
        Assert.DoesNotContain(secret, exception.Message);
        Assert.DoesNotContain("Password", exception.Message);
        Assert.DoesNotContain(secret, exception.ToString());
    }

    private static IHost BuildHost(RecordingSchemaMigrator migrator, bool applyOnStartup) =>
        Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["KnowHowToAI:Migrations:ApplyOnStartup"] = applyOnStartup.ToString()
                }))
            .ConfigureServices((context, services) =>
            {
                services.AddKnowHowToAIOptions(context.Configuration);
                services.AddSqlStorage();
                services.RemoveAll<ISchemaMigrator>();
                services.AddSingleton<ISchemaMigrator>(migrator);
            })
            .Build();

    private sealed class RecordingSchemaMigrator(Exception? failure = null) : ISchemaMigrator
    {
        public int CallCount { get; private set; }

        public Task<int> MigrateAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return failure is null
                ? Task.FromResult(2)
                : Task.FromException<int>(failure);
        }
    }
}
