using Bunit;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Knowledge.Node;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class NodeDocumentTests : BunitContext
{
    [Fact]
    public void CurrentRead_LoadsTheRequestedDocumentWithoutAnEditAction()
    {
        var snapshotId = new SnapshotId(1);
        var nodeId = new NodeId(Guid.NewGuid());
        var harness = new NavigationTestHarness(snapshotId);
        harness.AddNode(new Node(snapshotId, nodeId, null, "Node-Dokument", "Lesbare Beschreibung", 0, false));
        harness.AddContent(new NodeContent(
            snapshotId,
            nodeId,
            new AudienceId("Developer"),
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            "Lesbarer Inhalt",
            false));
        var navigation = harness.CreateService();
        Services.AddSingleton(navigation);
        Services.AddSingleton(new WorkspaceState());
        Services.AddSingleton<WorkspaceEditState>();

        var cut = Render<NodeDocument>(parameters => parameters
            .Add(document => document.NodeId, nodeId.Value)
            .Add(document => document.ReadContext, new ReadContext())
            .Add(document => document.AudienceId, "Developer"));

        Assert.Equal("Lesbarer Inhalt", cut.Find("[data-testid='node-content-markdown']").TextContent.Trim());
        Assert.Equal("Developer", cut.Find("[data-testid='node-details-requested-audience']").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-testid='node-details-title']"));
        Assert.Equal("Bearbeiten", cut.Find("[data-testid='node-details-edit']").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-testid='node-details-markdown-download']"));
    }
}
