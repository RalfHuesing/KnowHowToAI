using System.Net;
using System.Text.Json;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Retrieval.Export;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.IntegrationTests.Server.Mcp;
using KnowHowToAI.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowHowToAI.IntegrationTests.Server.Hosting;

[Trait("Category", "Integration")]
public sealed class MarkdownDownloadEndpointTests
{
    private const int LargeContentLength = 131_072;
    private static readonly SnapshotId CurrentSnapshotId = new(1);
    private static readonly SnapshotId HistoricalSnapshotId = new(2);
    private static readonly NodeId RootNodeId = new(Guid.Parse("40000000-0000-0000-0000-000000000000"));
    private static readonly NodeId ChildNodeId = new(Guid.Parse("40000000-0000-0000-0000-000000000001"));
    private static readonly RoleId DeveloperRoleId = new("Developer");
    private static readonly RoleId EndUserRoleId = new("EndUser");

    [Fact]
    public async Task DownloadMarkdown_ExportsRequestedSubtreeWithFallbackAndAttachmentHeaders()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        harness.AddRole(new Role(CurrentSnapshotId, EndUserRoleId, "Endanwender", null, false));
        harness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, EndUserRoleId, DeveloperRoleId, 1));
        harness.AddNode(new Node(CurrentSnapshotId, RootNodeId, null, "Wissensbasis", null, 0, false));
        harness.AddNode(new Node(CurrentSnapshotId, ChildNodeId, RootNodeId, "Teilbaum:/--", null, 1, false));
        harness.AddContent(Content(CurrentSnapshotId, ChildNodeId, "Fallback-Inhalt"));

        await using var host = await StartWithHarnessAsync(harness);
        using var client = new HttpClient();
        using var response = await client.GetAsync(
            $"{host.Address}/downloads/markdown?nodeId={ChildNodeId}&roleId={EndUserRoleId.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/markdown", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        var disposition = response.Content.Headers.ContentDisposition
            ?? throw new InvalidOperationException("Der Download enthält keinen Content-Disposition-Header.");
        Assert.Equal("attachment", disposition.DispositionType);
        Assert.Contains("Teilbaum-EndUser.md", disposition.ToString(), StringComparison.Ordinal);
        Assert.Equal("# Teilbaum:/--\n\nFallback-Inhalt\n", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DownloadMarkdown_UsesExplicitSnapshotContext()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootNodeId, null, "Aktuell", null, 0, false));
        harness.AddContent(Content(CurrentSnapshotId, RootNodeId, "Aktueller Inhalt"));
        harness.AddHistoricalSnapshot(new Snapshot(
            HistoricalSnapshotId,
            null,
            SnapshotState.Committed,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch));
        harness.AddNode(new Node(HistoricalSnapshotId, RootNodeId, null, "Historisch", null, 0, false));
        harness.AddContent(Content(HistoricalSnapshotId, RootNodeId, "Historischer Inhalt"));

        await using var host = await StartWithHarnessAsync(harness);
        using var client = new HttpClient();
        using var response = await client.GetAsync(
            $"{host.Address}/downloads/markdown?nodeId={RootNodeId}&roleId={DeveloperRoleId.Value}&snapshotId={HistoricalSnapshotId.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Historisch-Developer.md", response.Content.Headers.ContentDisposition!.ToString(), StringComparison.Ordinal);
        Assert.Equal("# Historisch\n\nHistorischer Inhalt\n", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DownloadMarkdown_DoesNotTruncateLargeMarkdown()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        var largeContent = new string('x', LargeContentLength);
        harness.AddNode(new Node(CurrentSnapshotId, RootNodeId, null, "Großer Export", null, 0, false));
        harness.AddContent(Content(CurrentSnapshotId, RootNodeId, largeContent));

        await using var host = await StartWithHarnessAsync(harness);
        using var client = new HttpClient();
        using var response = await client.GetAsync(
            $"{host.Address}/downloads/markdown?nodeId={RootNodeId}&roleId={DeveloperRoleId.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var markdown = await response.Content.ReadAsStringAsync();
        Assert.Contains(largeContent, markdown, StringComparison.Ordinal);
        Assert.True(markdown.Length > LargeContentLength);
    }

    [Fact]
    public async Task DownloadMarkdown_UnknownNodeReturnsProblemDetailsWithoutAttachment()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        var unknownNodeId = Guid.Parse("40000000-0000-0000-0000-000000009999");

        await using var host = await StartWithHarnessAsync(harness);
        using var client = new HttpClient();
        using var response = await client.GetAsync(
            $"{host.Address}/downloads/markdown?nodeId={unknownNodeId:D}&roleId={DeveloperRoleId.Value}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Null(response.Content.Headers.ContentDisposition);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.Equal(NavigationErrorCodes.NodeNotFound, document.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("correlationId").GetString()));
    }

    private static async Task<McpHttpHost> StartWithHarnessAsync(NavigationTestHarness harness) =>
        await McpHttpHost.StartAsync(services =>
        {
            services.RemoveAll<NavigationService>();
            services.AddSingleton(harness.CreateService());
            services.RemoveAll<MarkdownExportService>();
            services.AddSingleton(harness.CreateExportService());
        });

    private static NodeContent Content(SnapshotId snapshotId, NodeId nodeId, string markdown) => new(
        snapshotId,
        nodeId,
        DeveloperRoleId,
        new ContentRevisionId(Guid.NewGuid()),
        ContentMode.Independent,
        markdown,
        IsDeleted: false);
}
