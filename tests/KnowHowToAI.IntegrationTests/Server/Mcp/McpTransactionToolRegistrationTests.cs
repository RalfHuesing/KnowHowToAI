using System.Text.Json;
using KnowHowToAI.Server;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Registrierungsvertragstests: die fünf Transaction-Tools werden über die
/// MCP-Server-Pipeline mit stabilen Namen, Beschreibungen, Input-Schemata und
/// Annotations-Hinweisen bereitgestellt (M6.3).
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpTransactionToolRegistrationTests
{
    private static readonly string[] ExpectedToolNames =
    [
        "begin_transaction",
        "commit_transaction",
        "discard_transaction",
        "get_transaction",
        "validate_transaction"
    ];

    [Fact]
    public void ServerAssembly_RegistersExactlyTheFiveTransactionTools()
    {
        using var provider = BuildToolProvider();

        var toolNames = provider.GetServices<McpServerTool>()
            .Select(tool => tool.ProtocolTool.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedToolNames, toolNames);
    }

    [Fact]
    public void TransactionTools_DeclareDescriptionsAndProtocolAnnotations()
    {
        using var provider = BuildToolProvider();
        var annotations = provider.GetServices<McpServerTool>()
            .ToDictionary(tool => tool.ProtocolTool.Name, tool => tool.ProtocolTool.Annotations, StringComparer.Ordinal);

        Assert.All(provider.GetServices<McpServerTool>(),
            tool => Assert.False(string.IsNullOrWhiteSpace(tool.ProtocolTool.Description)));

        Assert.True(annotations["get_transaction"]!.ReadOnlyHint);
        Assert.True(annotations["validate_transaction"]!.ReadOnlyHint);
        Assert.NotEqual(true, annotations["begin_transaction"]!.ReadOnlyHint);
        Assert.False(annotations["begin_transaction"]!.DestructiveHint);
        Assert.True(annotations["discard_transaction"]!.DestructiveHint);
    }

    [Fact]
    public void TransactionTools_InputSchemas_UseStableCamelCaseArgumentNames()
    {
        using var provider = BuildToolProvider();
        var schemas = provider.GetServices<McpServerTool>()
            .ToDictionary(
                tool => tool.ProtocolTool.Name,
                tool => JsonDocument.Parse(tool.ProtocolTool.InputSchema.GetRawText()).RootElement.Clone(),
                StringComparer.Ordinal);

        Assert.Equal(
            new[] { "purpose", "actor", "client" },
            PropertyNames(schemas["begin_transaction"]));
        Assert.DoesNotContain("required", schemas["begin_transaction"].EnumerateObject().Select(p => p.Name));

        Assert.Equal(
            new[] { "transactionId" },
            PropertyNames(schemas["get_transaction"]));
        Assert.Contains(
            "transactionId",
            schemas["get_transaction"].GetProperty("required").EnumerateArray().Select(value => value.GetString()));

        Assert.Equal(
            new[] { "transactionId", "commitMessage" },
            PropertyNames(schemas["commit_transaction"]));
        Assert.Equal(
            new[] { "transactionId" },
            PropertyNames(schemas["discard_transaction"]));
    }

    private static ServiceProvider BuildToolProvider() =>
        new ServiceCollection()
            .AddMcpServer()
            .WithToolsFromAssembly(typeof(Program).Assembly)
            .Services
            .BuildServiceProvider();

    private static string[] PropertyNames(JsonElement schema) =>
        schema.GetProperty("properties").EnumerateObject().Select(property => property.Name).ToArray();
}
