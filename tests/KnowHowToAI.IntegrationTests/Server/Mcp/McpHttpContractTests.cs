using System.Text.Json;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Retrieval.Export;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;
using KnowHowToAI.IntegrationTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModelContextProtocol;
using ModelContextProtocol.Client;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// MCP-Vertragsregression über die HTTP-Boundary (offizieller SDK-McpClient gegen
/// reales Kestrel `/mcp`, StreamableHttp) — lesender Teil der Vertragsmatrix. Je
/// Vertragsform ein repräsentativer Erfolgs-/Fehlerfall; fachliche Varianten
/// bleiben in den bestehenden Tool-/Core-Detailtests:
///
/// - Toolmenge und Schemas: McpHttpTransportTests.StreamableHttpClient_DiscoversEveryRegisteredToolWithAnInputSchema
///   (vollständiges Inventar + Inputschema) sowie ListTools_ExposesRequiredInputFieldsForRepresentativeTools (Pflichtfelder).
/// - Read-Context Current/Snapshot/Transaction: GetRoot_ResolvesCurrentSnapshotHistoricalSnapshotAndWorkingTransactionReadContexts.
/// - Release-Read-Context: Read-Tools nehmen keinen Release-Selektor entgegen (Selektor-Felder
///   sind ausschließlich transactionId/snapshotId); Release-Kontext ist über get_snapshot,
///   list_releases und create_release in McpHttpWriteContractTests.HistoryTools_ReturnSnapshotDiffAndReleaseContracts abgedeckt.
/// - Paging und ungültige Cursor: ListChildren_PaginatesStablyAndRejectsInvalidCursor (weitere Paging-Varianten: McpPagingMapperTests).
/// - Root/Node/Children/Rollen: GetRoot_ReturnsNodeContractWithResolvedContent, GetNode_WithUnknownNodeId_ReturnsStableNodeNotFound,
///   ListChildren_PaginatesStablyAndRejectsInvalidCursor, ListRoles_ReturnsRoleMetadataItems.
/// - Suche und Markdownexport: Search_ReturnsHitsWithQueryAndMetadataFirst, ExportTree_BuildsMarkdownFromHierarchyAndContent
///   (weitere Export-/Suchvarianten: McpRetrievalToolsTests).
/// - Stabile Fehlercodes: Parameter → InvalidNodeId (McpHttpTransportTests.StreamableHttpClient_ReportsParameterAndDomainErrorsWithStableCodesAndDetails)
///   und InvalidReadContext (Read-Context-Test); Not-found → NodeNotFound (GetNode_WithUnknownNodeId_ReturnsStableNodeNotFound).
/// - Cancellation: McpHttpTransportTests.ClientAbort_CancelsTheBlockedCallWithoutEndingTheParallelSuccessCall.
/// </summary>
[Trait("Category", "Integration")]
public sealed class McpHttpContractTests
{
    private static readonly DateTimeOffset FixedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly TransactionId TransactionId =
        new(Guid.Parse("0e3af35a-0e85-4f24-8ae9-7dd2b3d124b3"));

    private static readonly SnapshotId WorkingSnapshotId = new(42);
    private static readonly NodeId RootNodeId = new(Guid.Parse("30000000-0000-0000-0000-000000000000"));
    private static readonly NodeId FirstChildNodeId = new(Guid.Parse("30000000-0000-0000-0000-000000000001"));
    private static readonly NodeId SecondChildNodeId = new(Guid.Parse("30000000-0000-0000-0000-000000000002"));
    private static readonly NodeId ThirdChildNodeId = new(Guid.Parse("30000000-0000-0000-0000-000000000003"));
    private static readonly NodeId UnknownNodeId = new(Guid.Parse("30000000-0000-0000-0000-000000009999"));
    private static readonly AudienceId RoleDeveloper = new("Developer");

    // ── Toolmenge und Schemas ─────────────────────────────────────────────────

    [Fact]
    public async Task ListTools_ExposesRequiredInputFieldsForRepresentativeTools()
    {
        await using var host = await McpHttpHost.StartAsync();
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        var tools = await client.ListToolsAsync();

        var required = RequiredArgumentsOf(tools, "get_node");
        Assert.Subset(new[] { "nodeId", "roleId" }.ToHashSet(StringComparer.Ordinal), required);

        required = RequiredArgumentsOf(tools, "replace_content");
        Assert.Subset(
            new[] { "transactionId", "nodeId", "roleId", "contentMode", "contentMd", "expectedChangeVersion" }.ToHashSet(StringComparer.Ordinal),
            required);
    }

    // ── Read-Context Current/Snapshot/Transaction ─────────────────────────────

    [Fact]
    public async Task GetRoot_ResolvesCurrentSnapshotHistoricalSnapshotAndWorkingTransactionReadContexts()
    {
        var harness = new NavigationTestHarness(new SnapshotId(1));
        harness.AddNode(Node(new SnapshotId(1), "Aktueller Titel"));
        harness.AddHistoricalSnapshot(new Snapshot(new SnapshotId(10), null, SnapshotState.Committed, FixedTimestamp, FixedTimestamp));
        harness.AddNode(Node(new SnapshotId(10), "Historischer Titel"));
        harness.SetTransaction(OpenTransaction());
        harness.AddNode(Node(WorkingSnapshotId, "Working-Titel"));
        await using var host = await StartWithNavigationAsync(harness);
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        using var current = await McpHttpToolCalls.CallAsync(client, "get_root", new Dictionary<string, object?> { ["roleId"] = "Developer" });
        using var historical = await McpHttpToolCalls.CallAsync(client, "get_root", new Dictionary<string, object?>
        {
            ["roleId"] = "Developer",
            ["snapshotId"] = "10"
        });
        using var working = await McpHttpToolCalls.CallAsync(client, "get_root", new Dictionary<string, object?>
        {
            ["roleId"] = "Developer",
            ["transactionId"] = TransactionId.ToString()
        });
        using var ambiguous = await McpHttpToolCalls.CallAsync(client, "get_root", new Dictionary<string, object?>
        {
            ["roleId"] = "Developer",
            ["transactionId"] = TransactionId.ToString(),
            ["snapshotId"] = "10"
        });

        Assert.Equal("Success", current.RootElement.GetProperty("code").GetString());
        Assert.Equal(RootNodeId.ToString(), current.RootElement.GetProperty("data").GetProperty("nodeId").GetString());
        Assert.Equal("Aktueller Titel", current.RootElement.GetProperty("data").GetProperty("title").GetString());
        Assert.Equal("Historischer Titel",
            historical.RootElement.GetProperty("data").GetProperty("title").GetString());
        Assert.Equal("Working-Titel", working.RootElement.GetProperty("data").GetProperty("title").GetString());
        Assert.Equal("InvalidReadContext", ambiguous.RootElement.GetProperty("code").GetString());
        Assert.Equal(
            TransactionId.ToString(),
            ambiguous.RootElement.GetProperty("details").GetProperty("transactionId").GetString());
        Assert.Equal("10", ambiguous.RootElement.GetProperty("details").GetProperty("snapshotId").GetString());
    }

    // ── Paging und ungültige Cursor (list_children) ───────────────────────────

    [Fact]
    public async Task ListChildren_PaginatesStablyAndRejectsInvalidCursor()
    {
        var harness = CreateHarnessWithThreeChildren();
        await using var host = await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<NavigationService>();
            services.AddSingleton(harness.CreateService(maximumPageSize: 3));
            services.RemoveAll<RetrievalPolicy>();
            services.AddSingleton(new RetrievalPolicy
            {
                DefaultPageSize = 2,
                MaximumPageSize = 3,
                SearchPageSize = 2,
                SearchMaximumPageSize = 3,
                SnippetMaximumCharacters = 100
            });
        });
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));
        var arguments = new Dictionary<string, object?> { ["roleId"] = "Developer", ["parentNodeId"] = RootNodeId.ToString() };

        using var firstPage = await McpHttpToolCalls.CallAsync(client, "list_children", arguments);
        var nextCursor = firstPage.RootElement.GetProperty("data").GetProperty("nextCursor").GetString();
        using var secondPage = await McpHttpToolCalls.CallAsync(client, "list_children", new Dictionary<string, object?>(arguments)
        {
            ["cursor"] = nextCursor
        });
        using var invalidCursor = await McpHttpToolCalls.CallAsync(client, "list_children", new Dictionary<string, object?>(arguments)
        {
            ["cursor"] = "kaputter-cursor"
        });

        Assert.Equal("Success", firstPage.RootElement.GetProperty("code").GetString());
        Assert.Equal(2, firstPage.RootElement.GetProperty("data").GetProperty("items").GetArrayLength());
        Assert.Equal(
            new[] { FirstChildNodeId.ToString(), SecondChildNodeId.ToString() },
            NodeIdsOf(firstPage));
        Assert.Equal(ThirdChildNodeId.ToString(), Assert.Single(NodeIdsOf(secondPage)));
        Assert.False(secondPage.RootElement.GetProperty("data").TryGetProperty("nextCursor", out _));
        Assert.Equal("InvalidCursor", invalidCursor.RootElement.GetProperty("code").GetString());
        Assert.Equal("kaputter-cursor",
            invalidCursor.RootElement.GetProperty("details").GetProperty("cursor").GetString());
    }

    // ── Root/Node/Rollen ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetRoot_ReturnsNodeContractWithResolvedContent()
    {
        var harness = CreateHarnessWithRootAndContent();
        await using var host = await StartWithNavigationAsync(harness);
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        using var envelope = await McpHttpToolCalls.CallAsync(client, "get_root", new Dictionary<string, object?> { ["roleId"] = "Developer" });

        var data = envelope.RootElement.GetProperty("data");
        Assert.Equal("Success", envelope.RootElement.GetProperty("code").GetString());
        Assert.Equal(RootNodeId.ToString(), data.GetProperty("nodeId").GetString());
        Assert.Equal("Hauptkapitel", data.GetProperty("title").GetString());
        Assert.Equal("Developer", data.GetProperty("requestedRole").GetString());
        Assert.Equal("Developer", data.GetProperty("resolvedRole").GetString());
        Assert.False(data.GetProperty("fallbackUsed").GetBoolean());
        Assert.Equal("Explicit", data.GetProperty("availability").GetString());
        Assert.Equal("Current", data.GetProperty("freshness").GetString());
        Assert.Equal("Inhalt Hauptkapitel.", data.GetProperty("content").GetString());
        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("contentRevisionId").GetString()));
    }

    [Fact]
    public async Task GetNode_WithUnknownNodeId_ReturnsStableNodeNotFound()
    {
        var harness = CreateHarnessWithRootAndContent();
        await using var host = await StartWithNavigationAsync(harness);
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        using var envelope = await McpHttpToolCalls.CallAsync(client, "get_node", new Dictionary<string, object?>
        {
            ["nodeId"] = UnknownNodeId.ToString(),
            ["roleId"] = "Developer"
        });

        Assert.Equal("NodeNotFound", envelope.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(envelope.RootElement.GetProperty("message").GetString()));
        Assert.Equal(UnknownNodeId.ToString(),
            envelope.RootElement.GetProperty("details").GetProperty("nodeId").GetString());
        Assert.False(envelope.RootElement.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task ListRoles_ReturnsRoleMetadataItems()
    {
        var harness = new NavigationTestHarness(new SnapshotId(1));
        harness.AddAudience(new Audience(new SnapshotId(1), new AudienceId("Admin"), "Admin", "Verwaltung", false));
        await using var host = await StartWithNavigationAsync(harness);
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        using var envelope = await McpHttpToolCalls.CallAsync(client, "list_roles");

        Assert.Equal("Success", envelope.RootElement.GetProperty("code").GetString());
        Assert.Equal(2, envelope.RootElement.GetProperty("data").GetProperty("items").GetArrayLength());
        Assert.Equal("Admin", envelope.RootElement.GetProperty("data").GetProperty("items")[0].GetProperty("roleId").GetString());
        Assert.Equal("Verwaltung",
            envelope.RootElement.GetProperty("data").GetProperty("items")[0].GetProperty("description").GetString());
    }

    // ── Suche und Markdownexport ──────────────────────────────────────────────

    [Fact]
    public async Task Search_ReturnsHitsWithQueryAndMetadataFirst()
    {
        var repository = new ScriptedRetrievalRepository
        {
            Response = Result<SearchRepositoryResult>.Success(new SearchRepositoryResult(
            [
                new SearchHit(RootNodeId, "Auftragserfassung", null, null, "Title", Availability.Explicit, RoleDeveloper, Freshness.Current),
                new SearchHit(FirstChildNodeId, "Preisfindung", null, "... Auftrag ...", "Content", Availability.Explicit, RoleDeveloper, Freshness.Current)
            ]))
        };
        var harness = new NavigationTestHarness(new SnapshotId(1));
        await using var host = await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<SearchService>();
            services.AddSingleton(harness.CreateSearchService(repository));
        });
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        using var envelope = await McpHttpToolCalls.CallAsync(client, "search", new Dictionary<string, object?> { ["text"] = "Auftrag" });

        var data = envelope.RootElement.GetProperty("data");
        Assert.Equal("Success", envelope.RootElement.GetProperty("code").GetString());
        Assert.Equal("Auftrag", data.GetProperty("query").GetString());
        Assert.Equal(2, data.GetProperty("items").GetArrayLength());
        Assert.Equal(RootNodeId.ToString(), data.GetProperty("items")[0].GetProperty("nodeId").GetString());
        Assert.Equal("Title", data.GetProperty("items")[0].GetProperty("hitField").GetString());
        Assert.Equal("Content", data.GetProperty("items")[1].GetProperty("hitField").GetString());
        Assert.False(data.TryGetProperty("nextCursor", out _));
    }

    [Fact]
    public async Task ExportTree_BuildsMarkdownFromHierarchyAndContent()
    {
        var harness = CreateExportHarness(withContent: true, chainLength: 2);
        await using var host = await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<MarkdownExportService>();
            services.AddSingleton(harness.CreateExportService());
        });
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        using var envelope = await McpHttpToolCalls.CallAsync(client, "export_tree", new Dictionary<string, object?>
        {
            ["rootNodeId"] = RootNodeId.ToString(),
            ["roleId"] = "Developer"
        });

        Assert.Equal("Success", envelope.RootElement.GetProperty("code").GetString());
        var markdown = envelope.RootElement.GetProperty("data").GetProperty("markdown").GetString();
        Assert.StartsWith("# Hauptkapitel", markdown, StringComparison.Ordinal);
        Assert.Contains("## Unterabschnitt 2", markdown, StringComparison.Ordinal);
        Assert.False(envelope.RootElement.TryGetProperty("warnings", out _));
    }

    // ── Unbekanntes Tool ──────────────────────────────────────────────────────

    [Fact]
    public async Task UnknownTool_IsRejectedWithProtocolErrorWithoutEndingTheTransport()
    {
        await using var host = await McpHttpHost.StartAsync();
        await using var client = await McpClient.CreateAsync(McpHttpHost.CreateTransport(host.Address));

        await Assert.ThrowsAsync<McpProtocolException>(() =>
            client.CallToolAsync("nonexistent_tool_xyz").AsTask());

        // Der Transport bedient nach dem Protokollfehler weiterhin gültige Requests.
        using var followUp = await McpHttpToolCalls.CallAsync(client, "get_transaction", new Dictionary<string, object?>
        {
            ["transactionId"] = "not-a-guid"
        });
        Assert.Equal("TransactionNotFound", followUp.RootElement.GetProperty("code").GetString());
    }

    // ── Hilfsfabriken und Test-Doubles ────────────────────────────────────────

    private static async Task<McpHttpHost> StartWithNavigationAsync(NavigationTestHarness harness) =>
        await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<NavigationService>();
            services.AddSingleton(harness.CreateService());
        });

    private static HashSet<string> RequiredArgumentsOf(IList<McpClientTool> tools, string toolName)
    {
        var schema = Assert.Single(tools, tool => tool.Name == toolName).JsonSchema;
        Assert.True(schema.TryGetProperty("required", out var required), $"Kein 'required' im Inputschema von {toolName}.");
        return required.EnumerateArray()
            .Select(value => value.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string[] NodeIdsOf(JsonDocument page) =>
        page.RootElement.GetProperty("data").GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("nodeId").GetString()!)
            .ToArray();

    private static NavigationTestHarness CreateHarnessWithRootAndContent()
    {
        var harness = new NavigationTestHarness(new SnapshotId(1));
        harness.AddNode(Node(new SnapshotId(1), "Hauptkapitel"));
        harness.AddContent(new NodeContent(
            new SnapshotId(1), RootNodeId, RoleDeveloper, new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent, "Inhalt Hauptkapitel.", false));
        return harness;
    }

    private static NavigationTestHarness CreateHarnessWithThreeChildren()
    {
        var harness = new NavigationTestHarness(new SnapshotId(1));
        harness.AddNode(Node(new SnapshotId(1), "Hauptkapitel"));
        harness.AddNode(new Node(new SnapshotId(1), FirstChildNodeId, RootNodeId, "Unterabschnitt 1", null, 1, false));
        harness.AddNode(new Node(new SnapshotId(1), SecondChildNodeId, RootNodeId, "Unterabschnitt 2", null, 2, false));
        harness.AddNode(new Node(new SnapshotId(1), ThirdChildNodeId, RootNodeId, "Unterabschnitt 3", null, 3, false));
        return harness;
    }

    private static NavigationTestHarness CreateExportHarness(bool withContent, int chainLength)
    {
        var harness = new NavigationTestHarness(new SnapshotId(1));
        var parentIds = new List<NodeId?> { null };
        for (var depth = 1; depth <= chainLength; depth++)
        {
            var nodeId = depth == 1 ? RootNodeId : new NodeId(new Guid(depth, 0, 0, new byte[8]));
            var title = depth == 1 ? "Hauptkapitel" : $"Unterabschnitt {depth}";
            harness.AddNode(new Node(new SnapshotId(1), nodeId, parentIds[^1], title, null, depth, false));
            if (withContent)
            {
                harness.AddContent(new NodeContent(
                    new SnapshotId(1), nodeId, RoleDeveloper, new ContentRevisionId(Guid.NewGuid()),
                    ContentMode.Independent,
                    depth == 1 ? "Inhalt Hauptkapitel." : $"Inhalt Unterabschnitt {depth}.", false));
            }

            parentIds.Add(nodeId);
        }

        return harness;
    }

    private static Node Node(SnapshotId snapshotId, string title) =>
        new(snapshotId, RootNodeId, null, title, null, 0, false);

    private static KnowledgeTransaction OpenTransaction() => new(
        TransactionId,
        new SnapshotId(1),
        WorkingSnapshotId,
        TransactionState.Open,
        ChangeVersion: 0,
        CreatedAtUtc: FixedTimestamp,
        CommittedAtUtc: null,
        Purpose: null,
        Actor: null,
        Client: null,
        CommitMessage: null);

    private sealed class ScriptedRetrievalRepository : IRetrievalRepository
    {
        public Result<SearchRepositoryResult> Response { get; init; } =
            Result<SearchRepositoryResult>.Success(new SearchRepositoryResult([]));

        public Task<Result<SearchRepositoryResult>> SearchAsync(
            SearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Response);
    }
}
