using System.Text.Json;
using KnowHowToAI.Server;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Registrierungsvertragstests: die veröffentlichten V1-Tools (Transaktionen, Navigation,
/// Search, Export, Struktur-, Content- und Zielgruppen-Mutationen sowie Historien- und
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
        "create_audience",
        "create_node",
        "create_release",
        "delete_audience",
        "delete_content",
        "delete_node",
        "discard_transaction",
        "export_tree",
        "get_node",
        "get_root",
        "get_snapshot",
        "get_transaction",
        "get_transaction_changes",
        "list_audiences",
        "list_children",
        "list_releases",
        "move_node",
        "reorder_node",
        "replace_content",
        "replace_text",
        "search",
        "set_audience_resolution",
        "update_audience",
        "update_node",
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
        "list_audiences",
        "search",
        "validate_transaction"
    ];

    private static readonly string[] DestructiveToolNames =
    [
        "delete_content",
        "delete_node",
        "delete_audience",
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
    public void ServerAssembly_ExcludesLegacyRoleToolsAndJsonFields()
    {
        using var provider = BuildToolProvider();
        var tools = provider.GetServices<McpServerTool>().ToArray();
        var toolNames = tools.Select(tool => tool.ProtocolTool.Name).ToArray();

        Assert.DoesNotContain("list_roles", toolNames);
        Assert.DoesNotContain("create_role", toolNames);
        Assert.DoesNotContain("update_role", toolNames);
        Assert.DoesNotContain("delete_role", toolNames);
        Assert.DoesNotContain("set_role_resolution", toolNames);

        foreach (var tool in tools)
        {
            var schema = tool.ProtocolTool.InputSchema.GetRawText();
            Assert.DoesNotContain("roleId", schema, StringComparison.Ordinal);
            Assert.DoesNotContain("requestedRole", schema, StringComparison.Ordinal);
            Assert.DoesNotContain("resolvedRole", schema, StringComparison.Ordinal);
            Assert.DoesNotContain("candidateRole", schema, StringComparison.Ordinal);
            Assert.DoesNotContain("sourceRole", schema, StringComparison.Ordinal);
        }
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
            new[] { "audienceId", "includeDeleted", "snapshotId", "transactionId" },
            SortedPropertyNames(schemas["get_root"]));
        Assert.Equal(new[] { "audienceId" }, SortedRequiredNames(schemas["get_root"]));

        Assert.Equal(
            new[] { "audienceId", "includeDeleted", "nodeId", "snapshotId", "transactionId" },
            SortedPropertyNames(schemas["get_node"]));
        Assert.Equal(new[] { "audienceId", "nodeId" }, SortedRequiredNames(schemas["get_node"]));

        Assert.Equal(
            new[] { "audienceId", "cursor", "includeDeleted", "limit", "parentNodeId", "snapshotId", "transactionId" },
            SortedPropertyNames(schemas["list_children"]));
        Assert.Equal(new[] { "audienceId" }, SortedRequiredNames(schemas["list_children"]));

        Assert.Equal(
            new[] { "cursor", "includeDeleted", "limit", "snapshotId", "transactionId" },
            SortedPropertyNames(schemas["list_audiences"]));
        Assert.DoesNotContain("required", schemas["list_audiences"].EnumerateObject().Select(property => property.Name));

        Assert.Equal(
            new[] { "audienceId", "cursor", "includeDeleted", "limit", "snapshotId", "text", "transactionId" },
            SortedPropertyNames(schemas["search"]));
        Assert.Equal(new[] { "text" }, SortedRequiredNames(schemas["search"]));

        Assert.Equal(
            new[] { "audienceId", "includeDeleted", "rootNodeId", "snapshotId", "transactionId" },
            SortedPropertyNames(schemas["export_tree"]));
        Assert.Equal(new[] { "audienceId", "rootNodeId" }, SortedRequiredNames(schemas["export_tree"]));
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
            new[] { "audienceId", "contentMd", "contentMode", "description", "expectedChangeVersion", "parentNodeId", "sortOrder", "sources", "title", "transactionId" },
            SortedPropertyNames(schemas["create_node"]));
        Assert.Equal(new[] { "title", "transactionId" }, SortedRequiredNames(schemas["create_node"]));

        Assert.Equal(
            new[] { "description", "expectedChangeVersion", "nodeId", "title", "transactionId" },
            SortedPropertyNames(schemas["update_node"]));
        Assert.Equal(new[] { "nodeId", "title", "transactionId" }, SortedRequiredNames(schemas["update_node"]));

        Assert.Equal(
            new[] { "expectedChangeVersion", "nodeId", "parentNodeId", "sortOrder", "transactionId" },
            SortedPropertyNames(schemas["move_node"]));
        Assert.Equal(new[] { "nodeId", "sortOrder", "transactionId" }, SortedRequiredNames(schemas["move_node"]));

        Assert.Equal(
            new[] { "expectedChangeVersion", "nodeId", "sortOrder", "transactionId" },
            SortedPropertyNames(schemas["reorder_node"]));

        Assert.Equal(
            new[] { "deleteSubtree", "expectedChangeVersion", "nodeId", "transactionId" },
            SortedPropertyNames(schemas["delete_node"]));
        Assert.Equal(new[] { "nodeId", "transactionId" }, SortedRequiredNames(schemas["delete_node"]));

        Assert.Equal(
            new[] { "audienceId", "contentMd", "contentMode", "expectedChangeVersion", "nodeId", "sources", "transactionId" },
            SortedPropertyNames(schemas["replace_content"]));
        Assert.Equal(
            new[] { "audienceId", "contentMd", "contentMode", "expectedChangeVersion", "nodeId", "transactionId" },
            SortedRequiredNames(schemas["replace_content"]));
        var sourcesSchema = schemas["replace_content"].GetProperty("properties").GetProperty("sources");
        Assert.Contains("array", sourcesSchema.GetRawText(), StringComparison.Ordinal);
        Assert.Contains("contentRevisionId", schemas["replace_content"].GetRawText(), StringComparison.Ordinal);

        Assert.Equal(
            new[] { "audienceId", "expectedChangeVersion", "newText", "nodeId", "oldText", "transactionId" },
            SortedPropertyNames(schemas["replace_text"]));
        Assert.Equal(
            new[] { "audienceId", "expectedChangeVersion", "newText", "nodeId", "oldText", "transactionId" },
            SortedRequiredNames(schemas["replace_text"]));
        Assert.Equal(
            new[] { "audienceId", "expectedChangeVersion", "nodeId", "transactionId" },
            SortedRequiredNames(schemas["delete_content"]));

        Assert.Equal(
            new[] { "description", "expectedChangeVersion", "name", "transactionId" },
            SortedPropertyNames(schemas["create_audience"]));
        Assert.Equal(new[] { "expectedChangeVersion", "name", "transactionId" }, SortedRequiredNames(schemas["create_audience"]));

        Assert.Equal(
            new[] { "audienceId", "description", "expectedChangeVersion", "name", "transactionId" },
            SortedPropertyNames(schemas["update_audience"]));
        Assert.Equal(
            new[] { "audienceId", "expectedChangeVersion", "name", "transactionId" },
            SortedRequiredNames(schemas["update_audience"]));

        Assert.Equal(
            new[] { "audienceId", "candidateAudienceIds", "expectedChangeVersion", "transactionId" },
            SortedPropertyNames(schemas["set_audience_resolution"]));
        Assert.Equal(
            new[] { "audienceId", "candidateAudienceIds", "expectedChangeVersion", "transactionId" },
            SortedRequiredNames(schemas["set_audience_resolution"]));

        Assert.Equal(
            new[] { "audienceId", "expectedChangeVersion", "transactionId" },
            SortedRequiredNames(schemas["delete_audience"]));
    }

    [Fact]
    public void AudiencesAndContentMutationSchemas_RejectMissingExpectedChangeVersion()
    {
        using var provider = BuildToolProvider();
        var schemas = provider.GetServices<McpServerTool>()
            .ToDictionary(
                tool => tool.ProtocolTool.Name,
                tool => JsonDocument.Parse(tool.ProtocolTool.InputSchema.GetRawText()).RootElement.Clone(),
                StringComparer.Ordinal);

        foreach (var toolName in new[]
        {
            "create_audience", "update_audience", "delete_audience", "set_audience_resolution",
            "replace_content", "replace_text", "delete_content"
        })
        {
            Assert.Contains("expectedChangeVersion", SortedRequiredNames(schemas[toolName]));
        }
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
