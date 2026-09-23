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
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.Features.Knowledge.Node;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgePageTests : BunitContext
{
    private static readonly SnapshotId DefaultSnapshotId = new(1);
    private static readonly AudienceId DefaultAudienceId = new("Developer");


    public KnowledgePageTests() =>
        JSInterop.SetupModule("./Web/Features/Content/ContentEditor.razor.js").Mode = JSRuntimeMode.Loose;

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
        Assert.Equal("Root", cut.Find("h1").TextContent.Trim());
        Assert.Single(cut.FindAll("h1"));
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
        var deepNodeId = new NodeId(Guid.NewGuid());
        harness.AddNode(new Node(DefaultSnapshotId, deepNodeId, childId, "Deep Node", null, 1, false));

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        Services.AddWebPageStates()
            .AddKnowledgePageServices(service, transactionRepository: harness.CreateRepositories().Transactions);

        var cut = Render<KnowledgePage>(parameters => parameters
            .Add(p => p.NodeId, deepNodeId.Value));

        Assert.Equal("Deep Node", cut.Find("h1").TextContent.Trim());
        Assert.Single(cut.FindAll("h1"));
        Assert.Empty(cut.FindAll("[data-testid='node-details-title']"));
        Assert.NotNull(cut.Find("[data-testid='node-details-section']"));
        Assert.Empty(cut.FindAll("[data-testid='node-details-node-id']"));
        Assert.NotNull(cut.Find($"[data-nodeid='{deepNodeId.Value:D}']"));
        Assert.Equal(deepNodeId.Value, cut.FindComponent<NodeDetails>().Instance.ViewModel?.NodeId);

        // Der direkte Deep-Link lädt und markiert den gesamten sichtbaren Ancestor-Pfad.
        var breadcrumbCurrent = cut.Find("[aria-current='page']");
        Assert.Equal("Deep Node", breadcrumbCurrent.TextContent.Trim());
        Assert.Equal(3, cut.FindAll(".breadcrumb-item").Count);
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
        var tree = cut.FindComponent<KnowledgeTree>();
        await cut.InvokeAsync(() => tree.Instance.HandleTreeSelectionAsync(rootId.Value.ToString("D")));

        Assert.Contains($"/knowledge/{rootId.Value}", navMan.Uri);
    }

    [Fact]
    public void KnowledgePage_CurrentRead_ShowsRequestedAndFallbackAudience()
    {
        var harness = new NavigationTestHarness(DefaultSnapshotId);
        var nodeId = new NodeId(Guid.NewGuid());
        var developer = new AudienceId("Developer");
        var architect = new AudienceId("Architect");
        harness.ClearAudiences(DefaultSnapshotId);
        harness.AddNode(new Node(DefaultSnapshotId, nodeId, null, "Fallback node", null, 0, false));
        harness.AddAudience(new Audience(DefaultSnapshotId, developer, "Developer", null, false));
        harness.AddAudience(new Audience(DefaultSnapshotId, architect, "Architect", null, false));
        harness.AddAudienceResolution(new AudienceResolution(DefaultSnapshotId, developer, architect, 1));
        harness.AddAudienceResolution(new AudienceResolution(DefaultSnapshotId, architect, architect, 1));
        harness.AddContent(new NodeContent(
            DefaultSnapshotId,
            nodeId,
            architect,
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            "Architect content",
            false));

        var navigationService = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        Services.AddSingleton<IAudienceStorageService>(new InMemoryAudienceStorageService("Developer"));
        Services.AddWebPageStates()
            .AddKnowledgePageServices(navigationService, transactionRepository: harness.CreateRepositories().Transactions);
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            $"/knowledge/{nodeId.Value:D}?audienceId=Developer");

        var cut = Render<KnowledgePage>(parameters => parameters.Add(page => page.NodeId, nodeId.Value));

        Assert.Equal("Developer", cut.Find("[data-testid='node-details-requested-audience']").TextContent.Trim());
        Assert.Equal("Architect", cut.Find("[data-testid='node-details-resolved-audience']").TextContent.Trim());
        Assert.Contains("Fallback-Zielgruppe", cut.Find("[data-testid='node-content-fallback-context']").TextContent);
        Assert.Equal("Architect content", cut.Find("[data-testid='node-content-markdown']").TextContent.Trim());
    }

    [Theory]
    [InlineData("current", "Eigener Inhalt", "Independent", 0, null)]
    [InlineData("working", "Eigener Inhalt", "Derived", 1, "Quelle: Veraltet")]
    public void KnowledgePage_RoutedRead_RendersResolvedProvenanceForCurrentAndWorkingContexts(
        string scenario,
        string availability,
        string contentMode,
        int sourceCount,
        string? sourceFreshness)
    {
        var rootId = new NodeId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var sourceId = new NodeId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        var workingSnapshotId = new SnapshotId(3);
        var transactionId = new TransactionId(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        var defaultAudienceId = new AudienceId("Default");
        var harness = new NavigationTestHarness(DefaultSnapshotId);

        harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Independent", null, 0, false));
        harness.AddContent(new NodeContent(DefaultSnapshotId, rootId, new AudienceId("Developer"), new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Independent content", false));
        harness.AddAudience(new Audience(DefaultSnapshotId, defaultAudienceId, "Default", null, false));
        harness.AddAudienceResolution(new AudienceResolution(DefaultSnapshotId, DefaultAudienceId, DefaultAudienceId, 1));

        var transaction = new KnowledgeTransaction(transactionId, DefaultSnapshotId, workingSnapshotId, TransactionState.Open, 7, DateTimeOffset.UtcNow, null, null, null, null, null);
        harness.SetTransaction(transaction);
        AddDerivedScenario(harness, workingSnapshotId, rootId, sourceId, new AudienceId("Developer"), sourceIsCurrent: false);

        var service = harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
        Services.AddWebPageStates()
            .AddKnowledgePageServices(service, transactionRepository: harness.CreateRepositories().Transactions);
        Services.AddSingleton(new NodeMutationApplicationService(
            new InMemoryNodeMutationRepository(new WorkingNodeMutationState(workingSnapshotId, [], [], [], [])),
            new NodeMutationService(new GuidIdentifierGenerator()),
            TestPolicies.DefaultValidation));
        Services.AddSingleton(new NodeDeletionApplicationService(
            harness.CreateRepositories().WorkingSnapshots!,
            new InMemoryNodeMutationRepository(new WorkingNodeMutationState(workingSnapshotId, [], [], [], [])),
            new NodeMutationService(new GuidIdentifierGenerator()),
            TestPolicies.DefaultValidation));
        JSInterop.SetupAppDialog();

        var query = scenario switch
        {
            "working" => $"?transactionId={transactionId.Value:D}&audienceId=Developer",
            _ => "?audienceId=Developer"
        };
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/knowledge/{rootId.Value:D}{query}");

        var cut = Render<KnowledgePage>(parameters => parameters.Add(page => page.NodeId, rootId.Value));

        var details = cut.FindComponent<NodeDetails>().Instance.ViewModel!;
        Assert.Equal(availability, details.Availability switch { "Explicit" => "Eigener Inhalt", _ => details.Availability });
        Assert.Equal(contentMode, details.ContentMode);
        Assert.Equal(sourceCount, details.SourceRevisions.Count);
        if (sourceFreshness is not null)
            Assert.Equal("Stale", Assert.Single(details.SourceRevisions).Freshness);
    }

    private static void AddDerivedScenario(
        NavigationTestHarness harness,
        SnapshotId snapshotId,
        NodeId rootId,
        NodeId sourceId,
        AudienceId contentAudienceId,
        bool sourceIsCurrent)
    {
        var storedRevision = new ContentRevisionId(Guid.NewGuid());
        var currentRevision = sourceIsCurrent ? storedRevision : new ContentRevisionId(Guid.NewGuid());
        harness.AddNode(new Node(snapshotId, rootId, null, "Derived", null, 0, false));
        harness.AddNode(new Node(snapshotId, sourceId, rootId, "Source", null, 1, false));
        harness.AddContent(new NodeContent(snapshotId, rootId, contentAudienceId, new ContentRevisionId(Guid.NewGuid()), ContentMode.Derived, "Derived content", false));
        harness.AddContent(new NodeContent(snapshotId, sourceId, contentAudienceId, currentRevision, ContentMode.Independent, "Source content", false));
        harness.AddDependency(new ContentDependency(snapshotId, rootId, contentAudienceId, sourceId, contentAudienceId, storedRevision));
    }
}
