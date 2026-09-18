using KnowHowToAI.Server.Configuration;
using KnowHowToAI.Server.Hosting;
using KnowHowToAI.Server.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace KnowHowToAI.Server;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var application = CreateApplication(args);

        return await RunApplicationAsync(application).ConfigureAwait(false);
    }

    internal static Microsoft.AspNetCore.Builder.WebApplication CreateApplication(string[] args) =>
        CreateApplication(args, configureServices: null);

    internal static Microsoft.AspNetCore.Builder.WebApplication CreateApplication(
        string[] args,
        Action<Microsoft.Extensions.DependencyInjection.IServiceCollection>? configureServices)
    {
        var builder = CreateBuilder(args);
        configureServices?.Invoke(builder.Services);
        var application = builder.Build();

        application.UseAntiforgery();
        application.MapWebEndpoints();
        application.MapMcp("/mcp");

        return application;
    }

    internal static Microsoft.AspNetCore.Builder.WebApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

        builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog((services, loggerConfiguration) =>
            LoggingSetup.ApplyTo(
                loggerConfiguration,
                services.GetRequiredService<IOptions<LoggingOptions>>().Value));

        builder.Services.AddKnowHowToAIOptions(builder.Configuration);
        builder.Services.AddSqlStorage();
        builder.Services.AddApplicationServices();
        builder.Services.AddWebServices();
        builder.Services
            .AddMcpServer()
            .WithHttpTransport(options =>
            {
                options.SessionMode = ModelContextProtocol.AspNetCore.HttpServerSessionMode.Stateless;
#pragma warning disable MCP9004
                options.EnableLegacySse = false;
#pragma warning restore MCP9004
            })
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        return builder;
    }

    internal static Task<int> RunApplicationAsync(Microsoft.AspNetCore.Builder.WebApplication application) =>
        StdioHostRunner.RunAsync(
            application,
            () => ValidateStartupOptions(application.Services));

    private static void ValidateStartupOptions(IServiceProvider serviceProvider)
    {
        _ = serviceProvider.GetRequiredService<IOptions<KnowHowToAIOptions>>().Value;
        _ = serviceProvider.GetRequiredService<IOptions<DatabaseConnectionOptions>>().Value;
        _ = serviceProvider.GetRequiredService<IOptions<LoggingOptions>>().Value;
    }
}
