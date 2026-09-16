using System.Text.Json;
using KnowHowToAI.Server;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Registrierungsvertragstests: die veröffentlichten V1-Tools (Transaktionen, Navigation,
/// Search und Export) werden über die MCP-Server-Pipeline mit stabilen Namen,
/// Beschreibungen, Input-Schemata und Annotations-Hinweisen bereitgestellt (M6.3, M6.4).
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpTransactionToolRegistrationTests
{
    private static readonly string[] ExpectedToolNames =
    [
        "begin_transaction",
        "commit_transaction",
        "discard_transaction",
        "export_tree",
        "get_node",
        "get_root",
        "get_transaction",
        "list_children",
        "list_roles",
        "search",
        "validate_transaction"
    ];

    private static readonly string[] ReadOnlyToolNames =
    [
        "export_tree",
        "get_node",
        "get_root",
        "get_transaction",
        "list_children",
        "list_roles",
        "search",
        "validate_transaction"
    ];

    [Fact]
    public void ServerAssembly_RegistersExactlyTheReleasedV1Tools()
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

        Assert.All(ReadOnlyToolNames, name => Assert.True(annotations[name]!.ReadOnlyHint));
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

    [Fact]
    public void NavigationAndRetrievalTools_InputSchemas_UseStableCamelCaseArgumentNames()
    {
        using var provider = BuildToolProvider();
        var schemas = provider.GetServices<McpServerTool>()
            .ToDictionary(
                tool => tool.ProtocolTool.Name,
                tool => JsonDocument.Parse(tool.ProtocolTool.InputSchema.GetRawText()).RootElement.Clone(),
                StringComparer.Ordinal);

        Assert.Equal(
            new[] { "includeDeleted", "roleId", "snapshotId", "transactionId" },
            SortedPropertyNames(schemas["get_root"]));
        Assert.Equal(new[] { "roleId" }, SortedRequiredNames(schemas["get_root"]));

        Assert.Equal(
            new[] { "includeDeleted", "nodeId", "roleId", "snapshotId", "transactionId" },
            SortedPropertyNames(schemas["get_node"]));
        Assert.Equal(new[] { "nodeId", "roleId" }, SortedRequiredNames(schemas["get_node"]));

        Assert.Equal(
            new[] { "cursor", "includeDeleted", "limit", "parentNodeId", "roleId", "snapshotId", "transactionId" },
            SortedPropertyNames(schemas["list_children"]));
        Assert.Equal(new[] { "roleId" }, SortedRequiredNames(schemas["list_children"]));

        Assert.Equal(
            new[] { "cursor", "includeDeleted", "limit", "snapshotId", "transactionId" },
            SortedPropertyNames(schemas["list_roles"]));
        Assert.DoesNotContain("required", schemas["list_roles"].EnumerateObject().Select(property => property.Name));

        Assert.Equal(
            new[] { "cursor", "includeDeleted", "limit", "roleId", "snapshotId", "text", "transactionId" },
            SortedPropertyNames(schemas["search"]));
        Assert.Equal(new[] { "text" }, SortedRequiredNames(schemas["search"]));

        Assert.Equal(
            new[] { "includeDeleted", "roleId", "rootNodeId", "snapshotId", "transactionId" },
            SortedPropertyNames(schemas["export_tree"]));
        Assert.Equal(new[] { "roleId", "rootNodeId" }, SortedRequiredNames(schemas["export_tree"]));
    }

    private static ServiceProvider BuildToolProvider() =>
        new ServiceCollection()
            .AddMcpServer()
            .WithToolsFromAssembly(typeof(Program).Assembly)
            .Services
            .BuildServiceProvider();

    private static string[] PropertyNames(JsonElement schema) =>
        schema.GetProperty("properties").EnumerateObject().Select(property => property.Name).ToArray();

    private static string[] RequiredNames(JsonElement schema) =>
        schema.GetProperty("required").EnumerateArray().Select(value => value.GetString()!).ToArray();

    private static string[] SortedPropertyNames(JsonElement schema) =>
        PropertyNames(schema).OrderBy(name => name, StringComparer.Ordinal).ToArray();

    private static string[] SortedRequiredNames(JsonElement schema) =>
        RequiredNames(schema).OrderBy(name => name, StringComparer.Ordinal).ToArray();
}
