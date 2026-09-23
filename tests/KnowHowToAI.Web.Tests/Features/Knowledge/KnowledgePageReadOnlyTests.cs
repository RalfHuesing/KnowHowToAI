using Bunit;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgePageReadOnlyTests : BunitContext
{
    private static readonly SnapshotId CurrentSnapshotId = new(1);
    private static readonly SnapshotId WorkingSnapshotId = new(2);
    private static readonly TransactionId TransactionId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly NodeId RootNodeId = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    [Fact]
    public void WorkingRead_RendersTheNodeDocumentWithExplicitDirectEditAction()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        harness.SetTransaction(new KnowledgeTransaction(
            TransactionId,
            CurrentSnapshotId,
            WorkingSnapshotId,
            TransactionState.Open,
            0,
            DateTimeOffset.UtcNow,
            null,
            "Mutation-Synchronisation",
            "Test",
            "Web UI",
            null));
        harness.AddNode(new Node(WorkingSnapshotId, RootNodeId, null, "Root", "Beschreibung", 0, false));
        harness.AddContent(new NodeContent(
            WorkingSnapshotId,
            RootNodeId,
            new AudienceId("Developer"),
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            "Root content",
            false));

        var navigationService = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        Services.AddWebPageStates()
            .AddKnowledgePageServices(navigationService, transactionRepository: harness.CreateRepositories().Transactions);
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            $"/knowledge/{RootNodeId.Value:D}?transactionId={TransactionId.Value:D}&audienceId=Developer");

        var cut = Render<KnowledgePage>(parameters => parameters.Add(page => page.NodeId, RootNodeId.Value));

        Assert.Equal("Root", cut.Find("h1").TextContent.Trim());
        Assert.Equal("Root content", cut.Find("[data-testid='node-content-markdown']").TextContent.Trim());
        Assert.Equal("Developer", cut.Find("[data-testid='node-details-requested-audience']").TextContent.Trim());
        Assert.Single(cut.FindAll("[data-testid='node-details-edit']"));
        Assert.Empty(cut.FindAll("[data-testid='node-metadata-editor']"));
        Assert.Empty(cut.FindAll("[data-testid='node-deletion-editor']"));
        Assert.Empty(cut.FindAll("[data-testid='root-node-editor']"));
        Assert.Equal(TransactionId, Services.GetRequiredService<WorkspaceState>().CurrentReadContext.TransactionId);
    }
}
