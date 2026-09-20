using Bunit;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.Features.Knowledge.Components;
using KnowHowToAI.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class NodeDetailsPaneTests : BunitContext
{
    [Fact]
    public void NodeDetailsPane_LoadsSelectedNodeAndBuildsContextPreservingDownloadUrl()
    {
        var snapshotId = new SnapshotId(1);
        var nodeId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000010"));
        var harness = new NavigationTestHarness(snapshotId);
        harness.AddNode(new Node(snapshotId, nodeId, null, "Pane-Knoten", "Details", 0, false));
        harness.AddContent(new NodeContent(
            snapshotId,
            nodeId,
            new RoleId("Developer"),
            new ContentRevisionId(Guid.Parse("10000000-0000-0000-0000-000000000011")),
            ContentMode.Independent,
            "Pane-Inhalt",
            false));
        Services.AddSingleton(harness.CreateService());

        var cut = Render<NodeDetailsPane>(parameters => parameters
            .Add(pane => pane.NodeId, nodeId.Value)
            .Add(pane => pane.ReadContext, new ReadContext())
            .Add(pane => pane.RoleId, "Developer")
            .Add(pane => pane.QuerySnapshotId, snapshotId.Value.ToString()));

        Assert.Equal("Pane-Knoten", cut.Find("[data-testid='node-details-title']").TextContent.Trim());
        var downloadUrl = cut.Find("[data-testid='node-details-markdown-download']").GetAttribute("href");
        Assert.Contains($"nodeId={nodeId.Value:D}", downloadUrl, StringComparison.Ordinal);
        Assert.Contains("roleId=Developer", downloadUrl, StringComparison.Ordinal);
        Assert.Contains("snapshotId=1", downloadUrl, StringComparison.Ordinal);
    }
}
