using System.Reflection;
using KnowHowToAI.Server.Configuration;
using KnowHowToAI.Server.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Exploration.Hosting;

/// <summary>
/// Baut denselben DI-Host wie der produktive Server – inklusive Options-Bindung,
/// SQL-Storage (mit Schema-Migration beim Start) und Application-Services – nur
/// ohne stdio-Transport. Statt <c>WithToolsFromAssembly</c> registriert das Harness
/// die originalen, internen Tool-Klassen per Reflection selbst.
/// </summary>
internal static class ExplorationHostFactory
{
    public static IHost CreateHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddKnowHowToAIOptions(builder.Configuration);
        builder.Services.AddSqlStorage();
        builder.Services.AddApplicationServices();
        RegisterMcpToolClasses(builder.Services);
        return builder.Build();
    }

    /// <summary>MCP-Tool-Namen, wie sie der stdio-Server annoncieren würde.</summary>
    public static IReadOnlyList<string> DiscoverToolNames() =>
    [
        .. DiscoverToolClassTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>()?.Name)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
    ];

    private static void RegisterMcpToolClasses(IServiceCollection services)
    {
        foreach (var type in DiscoverToolClassTypes())
            services.AddSingleton(type);
    }

    private static IEnumerable<Type> DiscoverToolClassTypes() =>
        typeof(KnowHowToAIOptions).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && type.GetCustomAttribute<McpServerToolTypeAttribute>() is not null);
}
