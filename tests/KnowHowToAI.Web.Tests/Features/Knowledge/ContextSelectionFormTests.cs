using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.Features.Knowledge.Audiences;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class ContextSelectionFormTests : BunitContext
{
    private static readonly SnapshotId DefaultSnapshotId = new(1);

    private readonly NavigationTestHarness _harness;
    private readonly NavigationService _navigationService;
    private readonly KnowledgeTreeState _treeState;
    private readonly WorkspaceState _workspaceState;
    private readonly PageRegionState _pageRegions;
    private readonly KnowledgePageContextResolver _contextResolver;
    private readonly InMemoryAudienceStorageService _audienceStorage;
    private readonly ContextSelectorState _contextSelector;

    private static readonly RenderFragment<RouteData> RenderFoundRoute = routeData => builder =>
    {
        builder.OpenComponent<RouteView>(0);
        builder.AddAttribute(1, nameof(RouteView.RouteData), routeData);
        builder.CloseComponent();
    };

    public ContextSelectionFormTests()
    {
        _harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        _harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));
        _harness.AddAudience(new Audience(DefaultSnapshotId, new AudienceId("Architect"), "Architect", null, false));

        _navigationService = _harness.CreateService();
        _treeState = new KnowledgeTreeState(_navigationService);
        _workspaceState = new WorkspaceState();
        _pageRegions = new PageRegionState();
        _contextResolver = new KnowledgePageContextResolver(
            _harness.CreateRepositories().Snapshots,
            _harness.CreateRepositories().Transactions);
        _audienceStorage = new InMemoryAudienceStorageService("Developer");
        _contextSelector = new ContextSelectorState();

        Services.AddWebPageStates(_pageRegions, _workspaceState, _contextSelector)
            .AddSingleton(_navigationService)
            .AddKnowledgeTreeWorkspace(_treeState)
            .AddSingleton(_contextResolver)
            .AddSingleton<IAudienceStorageService>(_audienceStorage)
            .AddSingleton<IContextSelectionAudienceCatalog>(new ContextSelectionAudienceCatalog(_navigationService));

        JSInterop.Mode = JSRuntimeMode.Loose;
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
    public void O008_MandatoryDialogSelection_ReinitializesTheRoutedKnowledgePage()
    {
        _audienceStorage.LastAudienceId = null;
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo("/knowledge");

        var router = Render<Router>(parameters => parameters
            .Add(component => component.AppAssembly, typeof(KnowledgePage).Assembly)
            .Add(component => component.Found, RenderFoundRoute));

        var form = router.FindComponent<ContextSelectionForm>();
        form.WaitForState(() => form.FindAll("[data-testid='audience-option-Developer']").Count == 1);
        form.Find("[data-testid='audience-option-Developer'] input").Change("Developer");
        form.WaitForAssertion(() => Assert.False(form.Find("[data-testid='selector-apply-button']").HasAttribute("disabled")));
        form.Find("[data-testid='selector-apply-button']").Click();

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
