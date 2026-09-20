using Bunit;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.Features.Knowledge.Components;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
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
            new AudienceId("Developer"),
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

    [Fact]
    public void NodeDetailsPane_DerivedContentRemainsReadOnlyInWorkingTransaction()
    {
        var currentSnapshotId = new SnapshotId(1);
        var workingSnapshotId = new SnapshotId(2);
        var transactionId = new TransactionId(Guid.Parse("10000000-0000-0000-0000-000000000012"));
        var nodeId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000013"));
        var harness = new NavigationTestHarness(currentSnapshotId);
        harness.SetTransaction(new KnowledgeTransaction(
            transactionId,
            currentSnapshotId,
            workingSnapshotId,
            TransactionState.Open,
            4,
            DateTimeOffset.UtcNow,
            null,
            "Derived-Test",
            "Test",
            "Web",
            null));
        harness.AddNode(new Node(workingSnapshotId, nodeId, null, "Derived-Knoten", null, 0, false));
        harness.AddContent(new NodeContent(
            workingSnapshotId,
            nodeId,
            new AudienceId("Developer"),
            new ContentRevisionId(Guid.Parse("10000000-0000-0000-0000-000000000014")),
            ContentMode.Derived,
            "Abgeleiteter Inhalt",
            false));

        Services.AddSingleton(harness.CreateService());
        Services.AddSingleton(new WorkspaceState());
        Services.AddSingleton(TestNodeMutations.CreateService(
            new InMemoryNodeMutationRepository(new WorkingNodeMutationState(workingSnapshotId, [], [], [], []))));
        Services.AddSingleton(new NodeDeletionPreviewService(harness.CreateRepositories().WorkingSnapshots!));
        JSInterop.SetupAppDialog();

        var cut = Render<NodeDetailsPane>(parameters => parameters
            .Add(pane => pane.NodeId, nodeId.Value)
            .Add(pane => pane.ReadContext, new ReadContext(TransactionId: transactionId))
            .Add(pane => pane.RoleId, "Developer")
            .Add(pane => pane.TransactionId, transactionId)
            .Add(pane => pane.ChangeVersion, 4L));

        Assert.Empty(cut.FindAll("[data-testid='content-editor']"));
        Assert.Contains("Abgeleiteter Inhalt", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='content-editor-save']"));
    }
}
