using System.Reflection;
using KnowHowToAI.Server.Configuration;
using KnowHowToAI.Server.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Belegt, dass der produktive DI-Host jede MCP-Tool-Klasse aktivieren kann –
/// exakt der Pfad, den der MCP-Server bei der Tool-Aktivierung durchläuft.
/// Fängt Konstruktor-Ambiguitäten der Service-Abhängigkeiten (z. B. mehrere
/// auflösbare Konstruktoren), die erst zur Laufzeit werfen.
/// </summary>
[Trait("Category", "Integration")]
public sealed class McpToolActivationTests
{
    [Fact]
    public void Host_CanActivateEveryMcpToolClass()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddKnowHowToAIOptions(context.Configuration);
                services.AddSqlStorage();
                services.AddApplicationServices();
                foreach (var toolType in DiscoverToolClassTypes())
                    services.AddSingleton(toolType);
            })
            .Build();

        var toolTypes = DiscoverToolClassTypes().ToList();
        Assert.NotEmpty(toolTypes);

        foreach (var toolType in toolTypes)
        {
            var instance = host.Services.GetRequiredService(toolType);
            Assert.NotNull(instance);
        }
    }

    private static IEnumerable<Type> DiscoverToolClassTypes() =>
        typeof(KnowHowToAIOptionsValidator).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && type.GetCustomAttribute<McpServerToolTypeAttribute>() is not null);
}
