using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgePageTests : BunitContext
{
    private static readonly SnapshotId DefaultSnapshotId = new(1);
    private static readonly RoleId DefaultRoleId = new("Developer");

    private sealed class FakeReleaseRepository : IReleaseRepository
    {
        public Task<Release?> FindAsync(ReleaseId releaseId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Release?>(null);
    }

    [Fact]
    public async Task KnowledgePage_RendersHeaderWithBreadcrumbsAndTree()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        var treeState = new KnowledgeTreeState(service);
        var workspaceState = new WorkspaceState();
        var pageRegions = new PageRegionState();
        var releaseRepo = new FakeReleaseRepository();
        var contextResolver = new WebReadContextResolver(releaseRepo, harness.CreateRepositories().Transactions);

        Services.AddSingleton(service);
        Services.AddSingleton(treeState);
        Services.AddSingleton<IKnowledgeTreeWorkspace>(treeState);
        Services.AddSingleton(workspaceState);
        Services.AddSingleton(pageRegions);
        Services.AddSingleton(contextResolver);
        Services.AddSingleton<IRoleStorageService>(new KnowHowToAI.Web.Tests.TestSupport.InMemoryRoleStorageService("Developer"));
        Services.AddSingleton(new ContextSelectorState());

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
        var treeState = new KnowledgeTreeState(service);
        var workspaceState = new WorkspaceState();
        var pageRegions = new PageRegionState();
        var releaseRepo = new FakeReleaseRepository();
        var contextResolver = new WebReadContextResolver(releaseRepo, harness.CreateRepositories().Transactions);

        Services.AddSingleton(service);
        Services.AddSingleton(treeState);
        Services.AddSingleton<IKnowledgeTreeWorkspace>(treeState);
        Services.AddSingleton(workspaceState);
        Services.AddSingleton(pageRegions);
        Services.AddSingleton(contextResolver);
        Services.AddSingleton<IRoleStorageService>(new KnowHowToAI.Web.Tests.TestSupport.InMemoryRoleStorageService("Developer"));
        Services.AddSingleton(new ContextSelectorState());

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
        var treeState = new KnowledgeTreeState(service);
        var workspaceState = new WorkspaceState();
        var pageRegions = new PageRegionState();
        var releaseRepo = new FakeReleaseRepository();
        var contextResolver = new WebReadContextResolver(releaseRepo, harness.CreateRepositories().Transactions);

        Services.AddSingleton(service);
        Services.AddSingleton(treeState);
        Services.AddSingleton<IKnowledgeTreeWorkspace>(treeState);
        Services.AddSingleton(workspaceState);
        Services.AddSingleton(pageRegions);
        Services.AddSingleton(contextResolver);
        Services.AddSingleton<IRoleStorageService>(new KnowHowToAI.Web.Tests.TestSupport.InMemoryRoleStorageService("Developer"));
        Services.AddSingleton(new ContextSelectorState());

        var navMan = Services.GetRequiredService<NavigationManager>();

        var cut = Render<KnowledgePage>();

        // Klick auf Root-Knoten im Baum
        var rootNode = cut.Find($"div[data-testid='treeitem-{rootId.Value}']");
        await cut.InvokeAsync(() => rootNode.Click());

        Assert.Contains($"/knowledge/{rootId.Value}", navMan.Uri);
    }
}
