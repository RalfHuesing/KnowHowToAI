using Bunit;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
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
    public void CurrentRead_LoadsTheDocumentAndOffersAllFourViews()
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
        Assert.Equal(4, cut.FindAll("[data-testid^='node-view-']").Count(element => element.TagName == "BUTTON"));
        Assert.Empty(cut.FindAll("[data-testid='node-details-description']"));
        Assert.Empty(cut.FindAll("[data-testid='node-details-markdown-download']"));
    }

    [Fact]
    public void DerivedDocument_ShowsSourceNodeTitleWithoutItsIdentifier()
    {
        var snapshotId = new SnapshotId(1);
        var targetNodeId = new NodeId(Guid.NewGuid());
        var sourceNodeId = new NodeId(Guid.NewGuid());
        var sourceRevisionId = new ContentRevisionId(Guid.NewGuid());
        var harness = new NavigationTestHarness(snapshotId);
        harness.AddNode(new Node(snapshotId, targetNodeId, null, "Abgeleiteter Knoten", null, 0, false));
        harness.AddNode(new Node(snapshotId, sourceNodeId, targetNodeId, "Lesbarer Quellknoten", null, 0, false));
        harness.AddContent(new NodeContent(
            snapshotId,
            sourceNodeId,
            new AudienceId("Developer"),
            sourceRevisionId,
            ContentMode.Independent,
            "Quellinhalt",
            false));
        harness.AddContent(new NodeContent(
            snapshotId,
            targetNodeId,
            new AudienceId("Developer"),
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Derived,
            "Abgeleiteter Inhalt",
            false));
        harness.AddDependency(new ContentDependency(
            snapshotId,
            targetNodeId,
            new AudienceId("Developer"),
            sourceNodeId,
            new AudienceId("Developer"),
            sourceRevisionId));
        Services.AddSingleton(harness.CreateService());
        Services.AddSingleton(new WorkspaceState());
        Services.AddSingleton<WorkspaceEditState>();

        var cut = Render<NodeDocument>(parameters => parameters
            .Add(document => document.NodeId, targetNodeId.Value)
            .Add(document => document.ReadContext, new ReadContext())
            .Add(document => document.AudienceId, "Developer"));

        var viewModel = cut.FindComponent<NodeDetails>().Instance.ViewModel!;
        var sourceRevision = Assert.Single(viewModel.SourceRevisions);
        Assert.Equal("Lesbarer Quellknoten", sourceRevision.SourceNodeTitle);
        Assert.Equal("Developer", sourceRevision.SourceAudienceId);
        Assert.DoesNotContain(sourceNodeId.Value.ToString(), cut.Find("[data-testid='node-details']").TextContent);
        Assert.DoesNotContain(sourceNodeId.Value.ToString("N")[..8], cut.Find("[data-testid='node-details']").TextContent);
    }

    [Fact]
    public void DerivedDocument_WithUnavailableSource_ShowsExplanationWithoutIdentifier()
    {
        var snapshotId = new SnapshotId(1);
        var targetNodeId = new NodeId(Guid.NewGuid());
        var sourceNodeId = new NodeId(Guid.NewGuid());
        var harness = new NavigationTestHarness(snapshotId);
        harness.AddNode(new Node(snapshotId, targetNodeId, null, "Abgeleiteter Knoten", null, 0, false));
        harness.AddContent(new NodeContent(
            snapshotId,
            targetNodeId,
            new AudienceId("Developer"),
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Derived,
            "Abgeleiteter Inhalt",
            false));
        harness.AddDependency(new ContentDependency(
            snapshotId,
            targetNodeId,
            new AudienceId("Developer"),
            sourceNodeId,
            new AudienceId("Developer"),
            new ContentRevisionId(Guid.NewGuid())));
        Services.AddSingleton(harness.CreateService());
        Services.AddSingleton(new WorkspaceState());
        Services.AddSingleton<WorkspaceEditState>();

        var cut = Render<NodeDocument>(parameters => parameters
            .Add(document => document.NodeId, targetNodeId.Value)
            .Add(document => document.ReadContext, new ReadContext())
            .Add(document => document.AudienceId, "Developer"));

        var viewModel = cut.FindComponent<NodeDetails>().Instance.ViewModel!;
        var sourceRevision = Assert.Single(viewModel.SourceRevisions);
        Assert.Equal("Quellknoten nicht verfügbar", sourceRevision.SourceNodeTitle);
        Assert.DoesNotContain(sourceNodeId.Value.ToString(), cut.Find("[data-testid='node-details']").TextContent);
        Assert.DoesNotContain(sourceNodeId.Value.ToString("N")[..8], cut.Find("[data-testid='node-details']").TextContent);
    }
}
