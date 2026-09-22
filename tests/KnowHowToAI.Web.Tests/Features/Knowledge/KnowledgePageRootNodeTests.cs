using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgePageRootNodeTests : BunitContext
{
    private static readonly SnapshotId CurrentSnapshotId = new(1);
    private static readonly SnapshotId WorkingSnapshotId = new(2);
    private static readonly TransactionId TransactionId = new(Guid.Parse("83c97baf-0382-40f1-b386-d1750134349a"));

    [Fact]
    public void KnowledgePage_EmptyTreeWithoutTransaction_DoesNotOfferRootCreation()
    {
        var cut = RenderEmptyKnowledgePage(activeTransaction: false);

        Assert.NotNull(cut.Find("[data-testid='tree-empty']"));
        Assert.Empty(cut.FindAll("[data-testid='root-node-editor']"));
    }

    [Fact]
    public void KnowledgePage_EmptyWorkingTree_ExplainsThatRootCreationIsNotAvailableInTheReadRoute()
    {
        var cut = RenderEmptyKnowledgePage(activeTransaction: true);

        Assert.NotNull(cut.Find("[data-testid='tree-empty']"));
        Assert.NotNull(cut.Find("[data-testid='knowledge-empty-root']"));
        var createAction = cut.Find("[data-testid='knowledge-create-root']");
        Assert.True(createAction.HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[data-testid='root-node-editor']"));
    }

    private IRenderedComponent<KnowledgePage> RenderEmptyKnowledgePage(bool activeTransaction)
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        if (activeTransaction)
        {
            harness.SetTransaction(new KnowledgeTransaction(
                TransactionId,
                CurrentSnapshotId,
                WorkingSnapshotId,
                TransactionState.Open,
                0,
                DateTimeOffset.UtcNow,
                null,
                "Initialer Root-Knoten",
                "Alice",
                "Web UI",
                null));
            Services.AddSingleton(new NodeMutationApplicationService(
                new InMemoryNodeMutationRepository(new WorkingNodeMutationState(WorkingSnapshotId, [], [], [], [])),
                new NodeMutationService(new FixedIdentifierGenerator()),
                TestPolicies.DefaultValidation));
        }

        var navigationService = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        Services.AddWebPageStates()
            .AddKnowledgePageServices(navigationService, transactionRepository: harness.CreateRepositories().Transactions);

        if (activeTransaction)
        {
            Services.GetRequiredService<NavigationManager>().NavigateTo(
                $"/knowledge?transactionId={TransactionId.Value:D}&audienceId=Developer");
        }

        return Render<KnowledgePage>();
    }
}
