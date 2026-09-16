using KnowHowToAI.Server.Configuration;
using KnowHowToAI.Server.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace KnowHowToAI.Server;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var host = CreateHost(args);

        var logger = host.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(StdioHostRunner));
        return await StdioHostRunner.RunAsync(host, logger).ConfigureAwait(false);
    }

    internal static IHost CreateHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog((services, loggerConfiguration) =>
            LoggingSetup.ApplyTo(
                loggerConfiguration,
                services.GetRequiredService<IOptions<LoggingOptions>>().Value));

        builder.Services.AddKnowHowToAIOptions(builder.Configuration);
        builder.Services.AddSqlStorage();
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport();

        return builder.Build();
    }
}
