using KnowHowToAI.Server.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowHowToAI.IntegrationTests.Server.Hosting;

/// <summary>
/// Belegt, dass Shutdown, Cancellation, defekte Client-Pipe und Startfehler vom
/// Serverlauf kontrolliert in stabile Exitcodes überführt werden, statt den
/// Prozess mit unbehandelter Exception zu beenden.
/// </summary>
[Trait("Category", "Integration")]
public sealed class StdioHostRunnerTests
{
    [Fact]
    public async Task ImmediateClientDisconnect_ReturnsSuccess()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddSingleton<IHostedService>(serviceProvider =>
                new StoppingHostedService(serviceProvider.GetRequiredService<IHostApplicationLifetime>())))
            .Build();

        var exitCode = await StdioHostRunner.RunAsync(host, NullLogger.Instance);

        Assert.Equal(ServerExitCodes.Success, exitCode);
    }

    [Fact]
    public async Task BrokenClientPipeOnStart_ReturnsSuccessWithoutProcessCrash()
    {
        using var host = BuildHostWithStartFailure(new IOException("Verbindung unterbrochen"));

        var exitCode = await StdioHostRunner.RunAsync(host, NullLogger.Instance);

        Assert.Equal(ServerExitCodes.Success, exitCode);
    }

    [Fact]
    public async Task CancelledStart_ReturnsSuccessWithoutProcessCrash()
    {
        using var host = BuildHostWithStartFailure(new OperationCanceledException());

        var exitCode = await StdioHostRunner.RunAsync(host, NullLogger.Instance);

        Assert.Equal(ServerExitCodes.Success, exitCode);
    }

    [Fact]
    public async Task UnexpectedStartFailure_ReturnsStartupFailure()
    {
        using var host = BuildHostWithStartFailure(new InvalidOperationException("ungültiger Zustand"));

        var exitCode = await StdioHostRunner.RunAsync(host, NullLogger.Instance);

        Assert.Equal(ServerExitCodes.StartupFailure, exitCode);
    }

    [Fact]
    public async Task BrokenClientPipeDuringBackgroundRun_ReturnsSuccessWithoutProcessCrash()
    {
        var trigger = new TaskCompletionSource();
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
                services.AddSingleton<IHostedService>(new FailingBackgroundService(trigger.Task)))
            .Build();

        var runTask = StdioHostRunner.RunAsync(host, NullLogger.Instance);
        trigger.SetResult();

        var exitCode = await runTask;

        Assert.Equal(ServerExitCodes.Success, exitCode);
    }

    private static IHost BuildHostWithStartFailure(Exception startException) =>
        Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
                services.AddSingleton<IHostedService>(new ThrowingStartHostedService(startException)))
            .Build();

    private sealed class StoppingHostedService(IHostApplicationLifetime lifetime) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            lifetime.StopApplication();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ThrowingStartHostedService(Exception exception) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.FromException(exception);

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FailingBackgroundService(Task trigger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await trigger.WaitAsync(stoppingToken);
            throw new IOException("Client-Pipe defekt");
        }
    }
}
