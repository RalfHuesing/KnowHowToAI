using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Runtime;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgePageTests : BunitContext
{
    private static readonly SnapshotId DefaultSnapshotId = new(1);
    private static readonly RoleId DefaultRoleId = new("Developer");

    [Fact]
    public async Task KnowledgePage_RendersHeaderWithBreadcrumbsAndTree()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        Services.AddWebPageStates()
            .AddKnowledgePageServices(service, transactionRepository: harness.CreateRepositories().Transactions);

        var cut = Render<KnowledgePage>();

        Assert.NotNull(cut.Find("[data-testid='knowledge-page']"));
        Assert.NotNull(cut.Find("[data-testid='breadcrumbs']"));
        Assert.NotNull(cut.Find("[data-testid='knowledge-tree']"));
        Assert.NotNull(cut.Find("[data-testid='empty-selection']"));
    }

    [Fact]
    public async Task KnowledgePage_RouteNodeId_SelectsNodeAndUpdatesBreadcrumbs()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        var childId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, childId, rootId, "Child", null, 1, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        Services.AddWebPageStates()
            .AddKnowledgePageServices(service, transactionRepository: harness.CreateRepositories().Transactions);

        var cut = Render<KnowledgePage>(parameters => parameters
            .Add(p => p.NodeId, childId.Value));

        Assert.NotNull(cut.Find("[data-testid='node-details-section']"));
        var selectedIdElement = cut.Find("[data-testid='node-details-node-id']");
        Assert.Contains(childId.Value.ToString(), selectedIdElement.TextContent);

        // Breadcrumbs enthalten Root und Child
        var breadcrumbCurrent = cut.Find("[aria-current='page']");
        Assert.Equal("Child", breadcrumbCurrent.TextContent.Trim());
    }

    [Fact]
    public async Task KnowledgePage_SelectNodeInTree_NavigatesToRoute()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        Services.AddWebPageStates()
            .AddKnowledgePageServices(service, transactionRepository: harness.CreateRepositories().Transactions);

        var navMan = Services.GetRequiredService<NavigationManager>();

        var cut = Render<KnowledgePage>();

        // Klick auf Root-Knoten im Baum
        var rootNode = cut.Find($"div[data-testid='treeitem-{rootId.Value}']");
        await cut.InvokeAsync(() => rootNode.Click());

        Assert.Contains($"/knowledge/{rootId.Value}", navMan.Uri);
    }

    [Theory]
    [InlineData("current", "Eigener Inhalt", "Independent", 0, null)]
    [InlineData("snapshot", "Eigener Inhalt", "Derived", 1, "Quelle: Aktuell")]
    [InlineData("working", "Eigener Inhalt", "Derived", 1, "Quelle: Veraltet")]
    [InlineData("fallback", "Fallback", "Derived", 1, "Quelle: Aktuell")]
    public void KnowledgePage_RoutedRead_RendersResolvedProvenanceForEveryReadContext(
        string scenario,
        string availability,
        string contentMode,
        int sourceCount,
        string? sourceFreshness)
    {
        var rootId = new NodeId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var sourceId = new NodeId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        var historicalSnapshotId = new SnapshotId(2);
        var workingSnapshotId = new SnapshotId(3);
        var fallbackSnapshotId = new SnapshotId(4);
        var transactionId = new TransactionId(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        var defaultRoleId = new RoleId("Default");
        var harness = new NavigationTestHarness(DefaultSnapshotId);

        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Independent", null, 0, false));
        harness.AddContent(new NodeContent(DefaultSnapshotId, rootId, new RoleId("Developer"), new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Independent content", false));
        harness.AddRole(new Role(DefaultSnapshotId, defaultRoleId, "Default", null, false));
        harness.AddRoleResolution(new RoleResolution(DefaultSnapshotId, DefaultRoleId, DefaultRoleId, 1));

        harness.AddHistoricalSnapshot(new Snapshot(historicalSnapshotId, DefaultSnapshotId, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        AddDerivedScenario(harness, historicalSnapshotId, rootId, sourceId, new RoleId("Developer"), sourceIsCurrent: true);

        var transaction = new KnowledgeTransaction(transactionId, DefaultSnapshotId, workingSnapshotId, TransactionState.Open, 7, DateTimeOffset.UtcNow, null, null, null, null, null);
        harness.SetTransaction(transaction);
        AddDerivedScenario(harness, workingSnapshotId, rootId, sourceId, new RoleId("Developer"), sourceIsCurrent: false);

        harness.AddHistoricalSnapshot(new Snapshot(fallbackSnapshotId, DefaultSnapshotId, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        harness.AddRole(new Role(fallbackSnapshotId, defaultRoleId, "Default", null, false));
        harness.AddRoleResolution(new RoleResolution(fallbackSnapshotId, defaultRoleId, defaultRoleId, 1));
        harness.AddRoleResolution(new RoleResolution(fallbackSnapshotId, new RoleId("Developer"), defaultRoleId, 2));
        AddDerivedScenario(harness, fallbackSnapshotId, rootId, sourceId, defaultRoleId, sourceIsCurrent: true);

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        Services.AddWebPageStates()
            .AddKnowledgePageServices(service, transactionRepository: harness.CreateRepositories().Transactions);
        Services.AddSingleton(new NodeMutationApplicationService(
            new InMemoryNodeMutationRepository(new WorkingNodeMutationState(workingSnapshotId, [], [], [], [])),
            new NodeMutationService(new GuidIdentifierGenerator()),
            TestPolicies.DefaultValidation));
        Services.AddSingleton(new NodeDeletionPreviewService(harness.CreateRepositories().WorkingSnapshots!));
        JSInterop.SetupAppDialog();

        var query = scenario switch
        {
            "snapshot" => $"?snapshotId={historicalSnapshotId.Value}&roleId=Developer",
            "working" => $"?transactionId={transactionId.Value:D}&roleId=Developer",
            "fallback" => $"?snapshotId={fallbackSnapshotId.Value}&roleId=Developer",
            _ => "?roleId=Developer"
        };
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/knowledge/{rootId.Value:D}{query}");

        var cut = Render<KnowledgePage>(parameters => parameters.Add(page => page.NodeId, rootId.Value));

        Assert.Equal(availability, cut.Find("[data-testid='node-details-availability']").TextContent.Trim());
        Assert.Equal(contentMode == "Derived" ? "Abgeleitet" : "Eigenständig", cut.Find("[data-testid='node-details-content-mode']").TextContent.Trim());
        Assert.Equal(sourceCount, cut.FindAll("[data-testid='node-provenance-item']").Count);
        if (sourceFreshness is not null)
            Assert.Equal(sourceFreshness, cut.Find("[data-testid='node-provenance-freshness']").TextContent.Trim());
    }

    private static void AddDerivedScenario(
        NavigationTestHarness harness,
        SnapshotId snapshotId,
        NodeId rootId,
        NodeId sourceId,
        RoleId contentRoleId,
        bool sourceIsCurrent)
    {
        var storedRevision = new ContentRevisionId(Guid.NewGuid());
        var currentRevision = sourceIsCurrent ? storedRevision : new ContentRevisionId(Guid.NewGuid());
        harness.AddNode(new Node(snapshotId, rootId, null, "Derived", null, 0, false));
        harness.AddNode(new Node(snapshotId, sourceId, rootId, "Source", null, 1, false));
        harness.AddContent(new NodeContent(snapshotId, rootId, contentRoleId, new ContentRevisionId(Guid.NewGuid()), ContentMode.Derived, "Derived content", false));
        harness.AddContent(new NodeContent(snapshotId, sourceId, contentRoleId, currentRevision, ContentMode.Independent, "Source content", false));
        harness.AddDependency(new ContentDependency(snapshotId, rootId, contentRoleId, sourceId, contentRoleId, storedRevision));
    }
}
