using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgePageContextSelectorTests : BunitContext
{
    private static readonly SnapshotId DefaultSnapshotId = new(1);
    private static readonly SnapshotId HistoricalSnapshotId = new(42);

    private readonly NavigationTestHarness _harness;
    private readonly NavigationService _navigationService;
    private readonly KnowledgeTreeState _treeState;
    private readonly WorkspaceState _workspaceState;
    private readonly PageRegionState _pageRegions;
    private readonly InMemoryReleaseRepository _releaseRepo;
    private readonly WebReadContextResolver _contextResolver;
    private readonly InMemoryAudienceStorageService _audienceStorage;
    private readonly ContextSelectorState _contextSelector;
    private readonly NodeId _historicalRootId;

    private static readonly RenderFragment<RouteData> RenderFoundRoute = routeData => builder =>
    {
        builder.OpenComponent<RouteView>(0);
        builder.AddAttribute(1, nameof(RouteView.RouteData), routeData);
        builder.CloseComponent();
    };

    private sealed class EmptyContextSelectionCatalog : IContextSelectionCatalog
    {
        public Task<ContextSelectionOptionsViewModel> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ContextSelectionOptionsViewModel.Empty);
    }

    public KnowledgePageContextSelectorTests()
    {
        _harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        _harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));
        _harness.AddAudience(new Audience(DefaultSnapshotId, new AudienceId("Architect"), "Architect", null, false));

        // Historischen Snapshot mit eigenem Root anlegen
        _harness.AddHistoricalSnapshot(new Snapshot(
            HistoricalSnapshotId,
            DefaultSnapshotId,
            SnapshotState.Committed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));
        _historicalRootId = new NodeId(Guid.NewGuid());
        _harness.AddNode(new Node(HistoricalSnapshotId, _historicalRootId, null, "Historical Root", null, 0, false));

        _navigationService = _harness.CreateService();
        _treeState = new KnowledgeTreeState(_navigationService);
        _workspaceState = new WorkspaceState();
        _pageRegions = new PageRegionState();
        _releaseRepo = new InMemoryReleaseRepository();
        _contextResolver = new WebReadContextResolver(
            _releaseRepo,
            _harness.CreateRepositories().Transactions,
            _harness.CreateRepositories().Snapshots);
        _audienceStorage = new InMemoryAudienceStorageService("Developer");
        _contextSelector = new ContextSelectorState();

        Services.AddWebPageStates(_pageRegions, _workspaceState, _contextSelector)
            .AddSingleton(_navigationService)
            .AddKnowledgeTreeWorkspace(_treeState)
            .AddSingleton(_contextResolver)
            .AddSingleton<IAudienceStorageService>(_audienceStorage)
            .AddSingleton<IContextSelectionCatalog, EmptyContextSelectionCatalog>()
            .AddSingleton<IContextSelectionAudienceCatalog>(new ContextSelectionAudienceCatalog(_navigationService));

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void SnapshotQuery_DoesNotOverrideTheCurrentKnowledgeRoute()
    {
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo($"/knowledge?snapshotId={HistoricalSnapshotId.Value}&audienceId=Developer");

        var cut = Render<KnowledgePage>();

        Assert.Null(_workspaceState.CurrentReadContext.SnapshotId);
        Assert.Equal(KnowledgeReadContextKind.Current, _workspaceState.CurrentContext.ReadContext);
        Assert.Equal("Developer", _workspaceState.CurrentAudienceId);
        Assert.NotNull(cut.Find("[data-testid='knowledge-page']"));
    }

    [Fact]
    public void Reload_WithExplicitDraftInNodeUrl_RestoresThatDraft()
    {
        var transactionId = new TransactionId(Guid.Parse("efdf88ae-9b6f-4dcc-92fd-2a7d507fb4ef"));
        _harness.SetTransaction(new KnowledgeTransaction(
            transactionId,
            DefaultSnapshotId,
            new SnapshotId(DefaultSnapshotId.Value + 1),
            TransactionState.Open,
            3,
            DateTimeOffset.UnixEpoch,
            null,
            "Wissenspflege",
            "System",
            "Web UI",
            null));
        Services.AddSingleton(new NodeMutationApplicationService(
            new InMemoryNodeMutationRepository(new WorkingNodeMutationState(
                new SnapshotId(DefaultSnapshotId.Value + 1), [], [], [], [])),
            new NodeMutationService(new FixedIdentifierGenerator()),
            TestPolicies.DefaultValidation));
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo($"/knowledge?audienceId=Developer&transactionId={transactionId.Value:D}");

        var cut = Render<KnowledgePage>();

        Assert.Equal(transactionId, _workspaceState.ActiveTransactionId);
        Assert.Equal(DefaultSnapshotId.Value, _workspaceState.LoadedSnapshotId);
        Assert.Equal("Developer", _workspaceState.CurrentAudienceId);
        Assert.Equal(KnowledgeReadContextKind.Transaction, _workspaceState.CurrentContext.ReadContext);
        Assert.NotNull(cut.Find("[data-testid='knowledge-page']"));
    }

    [Fact]
    public void WorkingNode_MapsAudienceAndTransactionToMarkdownDownload()
    {
        var transactionId = new TransactionId(Guid.NewGuid());
        var workingSnapshotId = new SnapshotId(DefaultSnapshotId.Value + 1);
        _harness.SetTransaction(new KnowledgeTransaction(
            transactionId,
            DefaultSnapshotId,
            workingSnapshotId,
            TransactionState.Open,
            4,
            DateTimeOffset.UtcNow,
            null,
            "Working",
            "Test",
            "Web UI",
            null));
        _harness.AddNode(new Node(workingSnapshotId, _historicalRootId, null, "Working Root", null, 0, false));
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo($"/knowledge/{_historicalRootId.Value:D}?transactionId={transactionId.Value:D}&audienceId=Developer");

        var cut = Render<KnowledgePage>(parameters => parameters.Add(page => page.NodeId, _historicalRootId.Value));

        var downloadUrl = new Uri($"https://localhost{cut.Find("[data-testid='node-details-markdown-download']").GetAttribute("href")!}");
        var query = QueryHelpers.ParseQuery(downloadUrl.Query);
        Assert.Equal(_historicalRootId.Value.ToString("D"), query["nodeId"]);
        Assert.Equal("Developer", query["audienceId"]);
        Assert.Equal(transactionId.Value.ToString("D"), query["transactionId"]);
    }

    [Fact]
    public void ReleaseQuery_DoesNotOverrideTheCurrentKnowledgeRoute()
    {
        var releaseId = new ReleaseId(1);
        _releaseRepo.Add(new Release(releaseId, HistoricalSnapshotId, "v1.0.0", "Release 1", DateTimeOffset.UtcNow));

        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo($"/knowledge?releaseId=1&audienceId=Developer");

        var cut = Render<KnowledgePage>();

        Assert.Null(_workspaceState.CurrentReadContext.SnapshotId);
        Assert.Equal(KnowledgeReadContextKind.Current, _workspaceState.CurrentContext.ReadContext);
    }

    [Fact]
    public void UnknownSnapshotQuery_DoesNotChangeTheCurrentKnowledgeRead()
    {
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo("/knowledge?snapshotId=99999&audienceId=Developer");

        var cut = Render<KnowledgePage>();

        Assert.NotNull(cut.Find("[data-testid='knowledge-page']"));
        Assert.Empty(cut.FindAll("[data-testid='knowledge-error']"));
        Assert.Null(_workspaceState.CurrentReadContext.SnapshotId);
    }

    [Fact]
    public void TransactionQuery_IsTheSupportedKnowledgeReadContext()
    {
        var navMan = Services.GetRequiredService<NavigationManager>();
        var transactionId = new TransactionId(Guid.NewGuid());
        var workingSnapshotId = new SnapshotId(DefaultSnapshotId.Value + 1);
        _harness.SetTransaction(new KnowledgeTransaction(
            transactionId,
            DefaultSnapshotId,
            workingSnapshotId,
            TransactionState.Open,
            4,
            DateTimeOffset.UtcNow,
            null,
            "Working",
            "Test",
            "Web UI",
            null));
        navMan.NavigateTo($"/knowledge?snapshotId=1&transactionId={transactionId.Value:D}&audienceId=Developer");

        var cut = Render<KnowledgePage>();

        Assert.NotNull(cut.Find("[data-testid='knowledge-page']"));
        Assert.Equal(transactionId, _workspaceState.CurrentReadContext.TransactionId);
    }

    [Fact]
    public void O008_MissingAudienceInQueryAndStorage_TriggersMandatoryAudienceselector()
    {
        _audienceStorage.LastAudienceId = null;
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo("/knowledge");

        var cut = Render<KnowledgePage>();

        Assert.True(_contextSelector.IsOpen);
        Assert.Equal(ContextSelectorMode.MandatoryAudience, _contextSelector.Mode);
        Assert.Null(_workspaceState.CurrentAudienceId);
    }

    [Fact]
    public void SingleAvailableAudience_IsSelectedAndWrittenToTheKnowledgeUrl()
    {
        _harness.ClearAudiences(DefaultSnapshotId);
        var developer = new AudienceId("Developer");
        _harness.AddAudience(new Audience(DefaultSnapshotId, developer, "Developer", null, false));
        _harness.AddAudienceResolution(new AudienceResolution(DefaultSnapshotId, developer, developer, 1));
        _audienceStorage.LastAudienceId = null;
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/knowledge");

        var cut = Render<KnowledgePage>();

        Assert.False(_contextSelector.IsOpen);
        Assert.Equal("Developer", _workspaceState.CurrentAudienceId);
        Assert.Contains("audienceId=Developer", navigationManager.Uri, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Developer", cut.Find("[data-testid='knowledge-audience-context'] strong").TextContent.Trim());
    }

    [Fact]
    public void O008_InvalidAudienceInStorage_TriggersMandatoryAudienceselector()
    {
        _audienceStorage.LastAudienceId = "NonExistentAudience";
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo("/knowledge");

        var cut = Render<KnowledgePage>();

        Assert.True(_contextSelector.IsOpen);
        Assert.Equal(ContextSelectorMode.MandatoryAudience, _contextSelector.Mode);
        Assert.Null(_workspaceState.CurrentAudienceId);
    }

    [Fact]
    public void O008_AudienceQueryAddedAfterMandatorySelection_InitializesKnowledgePage()
    {
        _audienceStorage.LastAudienceId = null;
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo("/knowledge");

        var cut = Render<Router>(parameters => parameters
            .Add(router => router.AppAssembly, typeof(KnowledgePage).Assembly)
            .Add(router => router.Found, RenderFoundRoute));

        cut.WaitForAssertion(() => Assert.True(_contextSelector.IsOpen));
        Assert.Empty(cut.FindAll("[data-testid='knowledge-sidebar']"));

        // Der Dialog schließt seinen Circuit-State vor der Navigation nach erfolgreicher Auswahl.
        _contextSelector.Close();
        navMan.NavigateTo("/knowledge?audienceId=Developer");

        cut.WaitForAssertion(() =>
        {
            Assert.False(_contextSelector.IsOpen);
            Assert.Equal("Developer", _workspaceState.CurrentAudienceId);
            Assert.Single(cut.FindAll("[data-testid='knowledge-sidebar']"));
        });
    }

    [Fact]
    public async Task O008_MandatoryDialogSelection_ReinitializesTheRoutedKnowledgePage()
    {
        _audienceStorage.LastAudienceId = null;
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo("/knowledge");

        var dialog = Render<ContextSelectorDialog>();
        var router = Render<Router>(parameters => parameters
            .Add(component => component.AppAssembly, typeof(KnowledgePage).Assembly)
            .Add(component => component.Found, RenderFoundRoute));

        dialog.WaitForState(() => dialog.FindAll("[data-testid='audience-option-Developer']").Count == 1);
        var Audience = dialog.Find("[data-testid='audience-option-Developer'] input");
        await dialog.InvokeAsync(() => Audience.Change(true));
        var apply = dialog.Find("[data-testid='selector-apply-button']");
        await dialog.InvokeAsync(() => apply.Click());

        router.WaitForAssertion(() =>
        {
            Assert.Equal("Developer", _workspaceState.CurrentAudienceId);
            Assert.Single(router.FindAll("[data-testid='knowledge-sidebar']"));
        });
    }

    [Fact]
    public void Reconnect_PreservesContextAndAudience()
    {
        _audienceStorage.LastAudienceId = "Developer";
        var transactionId = new TransactionId(Guid.NewGuid());
        var workingSnapshotId = new SnapshotId(DefaultSnapshotId.Value + 1);
        _harness.SetTransaction(new KnowledgeTransaction(
            transactionId,
            DefaultSnapshotId,
            workingSnapshotId,
            TransactionState.Open,
            4,
            DateTimeOffset.UtcNow,
            null,
            "Working",
            "Test",
            "Web UI",
            null));
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo($"/knowledge?transactionId={transactionId.Value:D}&audienceId=Developer");

        var cut = Render<KnowledgePage>();

        Assert.Equal("Developer", _workspaceState.CurrentAudienceId);
        Assert.Equal(transactionId, _workspaceState.CurrentReadContext.TransactionId);

        // Zweite Komponente mit denselben Services rendern (simuliert Reconnect / neuen Circuit)
        var reconnectCut = Render<KnowledgePage>();

        Assert.Equal("Developer", _workspaceState.CurrentAudienceId);
        Assert.Equal(transactionId, _workspaceState.CurrentReadContext.TransactionId);
        Assert.NotNull(reconnectCut.Find("[data-testid='knowledge-page']"));
    }
}
