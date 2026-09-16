using System.Text.Json;
using KnowHowToAI.Server;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Registrierungsvertragstests: die veröffentlichten V1-Tools (Transaktionen, Navigation,
/// Search, Export, Struktur-, Content- und Rollen-Mutationen sowie Historien- und
/// Release-Tools) werden über die MCP-Server-Pipeline mit stabilen Namen,
/// Beschreibungen, Input-Schemata und Annotations-Hinweisen bereitgestellt
/// (M6.3, M6.4, M6.5, M6.6).
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpTransactionToolRegistrationTests
{
    private static readonly string[] ExpectedToolNames =
    [
        "begin_transaction",
        "commit_transaction",
        "compare_snapshots",
        "create_node",
        "create_release",
        "create_role",
        "delete_content",
        "delete_node",
        "delete_role",
        "discard_transaction",
        "export_tree",
        "get_node",
        "get_root",
        "get_snapshot",
        "get_transaction",
        "get_transaction_changes",
        "list_children",
        "list_releases",
        "list_roles",
        "move_node",
        "reorder_node",
        "replace_content",
        "replace_text",
        "search",
        "set_role_resolution",
        "update_node",
        "update_role",
        "validate_transaction"
    ];

    private static readonly string[] ReadOnlyToolNames =
    [
        "compare_snapshots",
        "export_tree",
        "get_node",
        "get_root",
        "get_snapshot",
        "get_transaction",
        "get_transaction_changes",
        "list_children",
        "list_releases",
        "list_roles",
        "search",
        "validate_transaction"
    ];

    private static readonly string[] DestructiveToolNames =
    [
        "delete_content",
        "delete_node",
        "delete_role",
        "discard_transaction"
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
        Assert.All(DestructiveToolNames, name => Assert.True(annotations[name]!.DestructiveHint));
        Assert.All(
            ExpectedToolNames.Except(DestructiveToolNames),
            name => Assert.NotEqual(true, annotations[name]!.DestructiveHint));
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

    [Fact]
    public void MutationTools_InputSchemas_UseStableCamelCaseArgumentNames()
    {
        using var provider = BuildToolProvider();
        var schemas = provider.GetServices<McpServerTool>()
            .ToDictionary(
                tool => tool.ProtocolTool.Name,
                tool => JsonDocument.Parse(tool.ProtocolTool.InputSchema.GetRawText()).RootElement.Clone(),
                StringComparer.Ordinal);

        Assert.Equal(
            new[] { "description", "parentNodeId", "sortOrder", "title", "transactionId" },
            SortedPropertyNames(schemas["create_node"]));
        Assert.Equal(new[] { "title", "transactionId" }, SortedRequiredNames(schemas["create_node"]));

        Assert.Equal(
            new[] { "description", "nodeId", "title", "transactionId" },
            SortedPropertyNames(schemas["update_node"]));
        Assert.Equal(new[] { "nodeId", "title", "transactionId" }, SortedRequiredNames(schemas["update_node"]));

        Assert.Equal(
            new[] { "nodeId", "parentNodeId", "sortOrder", "transactionId" },
            SortedPropertyNames(schemas["move_node"]));
        Assert.Equal(new[] { "nodeId", "sortOrder", "transactionId" }, SortedRequiredNames(schemas["move_node"]));

        Assert.Equal(
            new[] { "nodeId", "sortOrder", "transactionId" },
            SortedPropertyNames(schemas["reorder_node"]));

        Assert.Equal(
            new[] { "deleteSubtree", "nodeId", "transactionId" },
            SortedPropertyNames(schemas["delete_node"]));
        Assert.Equal(new[] { "nodeId", "transactionId" }, SortedRequiredNames(schemas["delete_node"]));

        Assert.Equal(
            new[] { "contentMd", "contentMode", "nodeId", "roleId", "sources", "transactionId" },
            SortedPropertyNames(schemas["replace_content"]));
        Assert.Equal(
            new[] { "contentMd", "contentMode", "nodeId", "roleId", "transactionId" },
            SortedRequiredNames(schemas["replace_content"]));
        var sourcesSchema = schemas["replace_content"].GetProperty("properties").GetProperty("sources");
        Assert.Contains("array", sourcesSchema.GetRawText(), StringComparison.Ordinal);
        Assert.Contains("contentRevisionId", schemas["replace_content"].GetRawText(), StringComparison.Ordinal);

        Assert.Equal(
            new[] { "newText", "nodeId", "oldText", "roleId", "transactionId" },
            SortedPropertyNames(schemas["replace_text"]));
        Assert.Equal(new[] { "nodeId", "roleId", "transactionId" }, SortedRequiredNames(schemas["delete_content"]));

        Assert.Equal(
            new[] { "description", "name", "transactionId" },
            SortedPropertyNames(schemas["create_role"]));
        Assert.Equal(new[] { "name", "transactionId" }, SortedRequiredNames(schemas["create_role"]));

        Assert.Equal(
            new[] { "description", "name", "roleId", "transactionId" },
            SortedPropertyNames(schemas["update_role"]));

        Assert.Equal(
            new[] { "candidateRoleIds", "roleId", "transactionId" },
            SortedPropertyNames(schemas["set_role_resolution"]));
        Assert.Equal(
            new[] { "candidateRoleIds", "roleId", "transactionId" },
            SortedRequiredNames(schemas["set_role_resolution"]));
    }

    [Fact]
    public void HistoryAndReleaseTools_InputSchemas_UseStableCamelCaseArgumentNames()
    {
        using var provider = BuildToolProvider();
        var schemas = provider.GetServices<McpServerTool>()
            .ToDictionary(
                tool => tool.ProtocolTool.Name,
                tool => JsonDocument.Parse(tool.ProtocolTool.InputSchema.GetRawText()).RootElement.Clone(),
                StringComparer.Ordinal);

        // get_snapshot
        Assert.Equal(new[] { "snapshotId" }, SortedPropertyNames(schemas["get_snapshot"]));
        Assert.Equal(new[] { "snapshotId" }, SortedRequiredNames(schemas["get_snapshot"]));

        // compare_snapshots
        Assert.Equal(
            new[] { "baseSnapshotId", "cursor", "limit", "targetSnapshotId" },
            SortedPropertyNames(schemas["compare_snapshots"]));
        Assert.Equal(
            new[] { "baseSnapshotId", "targetSnapshotId" },
            SortedRequiredNames(schemas["compare_snapshots"]));

        // get_transaction_changes
        Assert.Equal(
            new[] { "cursor", "limit", "transactionId" },
            SortedPropertyNames(schemas["get_transaction_changes"]));
        Assert.Equal(new[] { "transactionId" }, SortedRequiredNames(schemas["get_transaction_changes"]));

        // create_release
        Assert.Equal(
            new[] { "description", "name", "snapshotId" },
            SortedPropertyNames(schemas["create_release"]));
        Assert.Equal(
            new[] { "name", "snapshotId" },
            SortedRequiredNames(schemas["create_release"]));

        // list_releases
        Assert.Equal(
            new[] { "cursor", "limit" },
            SortedPropertyNames(schemas["list_releases"]));
        Assert.DoesNotContain("required", schemas["list_releases"].EnumerateObject().Select(property => property.Name));
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
