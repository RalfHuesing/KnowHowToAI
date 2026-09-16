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
    public async Task UnexpectedStartFailure_ReturnsStartupFailureWithoutLoggingSensitiveExceptionText()
    {
        const string secret = "correct-horse-battery-staple";
        using var host = BuildHostWithStartFailure(new InvalidOperationException($"Password={secret}"));
        var logger = new RecordingLogger();

        var exitCode = await StdioHostRunner.RunAsync(host, logger);

        Assert.Equal(ServerExitCodes.StartupFailure, exitCode);
        Assert.DoesNotContain(secret, logger.RenderedEvents, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", logger.RenderedEvents, StringComparison.Ordinal);
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

    private sealed class RecordingLogger : ILogger
    {
        private readonly List<string> _renderedEvents = [];

        public string RenderedEvents => string.Join(Environment.NewLine, _renderedEvents);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            _renderedEvents.Add($"{formatter(state, exception)}{Environment.NewLine}{exception}");
    }
}
