using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

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
    private readonly FakeReleaseRepository _releaseRepo;
    private readonly WebReadContextResolver _contextResolver;
    private readonly InMemoryRoleStorageService _roleStorage;
    private readonly ContextSelectorState _contextSelector;

    private sealed class FakeReleaseRepository : IReleaseRepository
    {
        private readonly Dictionary<ReleaseId, Release> _releases = new();

        public void Add(Release release) => _releases[release.ReleaseId] = release;

        public Task<Release?> FindAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        {
            _releases.TryGetValue(releaseId, out var release);
            return Task.FromResult(release);
        }
    }

    public KnowledgePageContextSelectorTests()
    {
        _harness = new NavigationTestHarness(DefaultSnapshotId);
        var rootId = new NodeId(Guid.NewGuid());
        _harness.AddNode(new Node(DefaultSnapshotId, rootId, null, "Root", null, 0, false));

        // Historischen Snapshot mit eigenem Root anlegen
        _harness.AddHistoricalSnapshot(new Snapshot(
            HistoricalSnapshotId,
            DefaultSnapshotId,
            SnapshotState.Committed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));
        var histRootId = new NodeId(Guid.NewGuid());
        _harness.AddNode(new Node(HistoricalSnapshotId, histRootId, null, "Historical Root", null, 0, false));

        _navigationService = _harness.CreateService();
        _treeState = new KnowledgeTreeState(_navigationService);
        _workspaceState = new WorkspaceState();
        _pageRegions = new PageRegionState();
        _releaseRepo = new FakeReleaseRepository();
        _contextResolver = new WebReadContextResolver(_releaseRepo);
        _roleStorage = new InMemoryRoleStorageService("Developer");
        _contextSelector = new ContextSelectorState();

        Services.AddSingleton(_navigationService);
        Services.AddSingleton(_treeState);
        Services.AddSingleton(_workspaceState);
        Services.AddSingleton(_pageRegions);
        Services.AddSingleton(_contextResolver);
        Services.AddSingleton<IRoleStorageService>(_roleStorage);
        Services.AddSingleton(_contextSelector);

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void ContextSwitch_ToHistoricalSnapshot_ResolvesSnapshotContext()
    {
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo($"/knowledge?snapshotId={HistoricalSnapshotId.Value}&roleId=Developer");

        var cut = Render<KnowledgePage>();

        Assert.Equal(HistoricalSnapshotId, _workspaceState.CurrentReadContext.SnapshotId);
        Assert.Equal(KnowledgeReadContextKind.Snapshot, _workspaceState.CurrentContext.ReadContext);
        Assert.Equal("Developer", _workspaceState.CurrentRoleId);
        Assert.NotNull(cut.Find("[data-testid='knowledge-page']"));
    }

    [Fact]
    public void ContextSwitch_ToRelease_ResolvesReleaseContext()
    {
        var releaseId = new ReleaseId(1);
        _releaseRepo.Add(new Release(releaseId, HistoricalSnapshotId, "v1.0.0", "Release 1", DateTimeOffset.UtcNow));

        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo($"/knowledge?releaseId=1&roleId=Developer");

        var cut = Render<KnowledgePage>();

        Assert.Equal(HistoricalSnapshotId, _workspaceState.CurrentReadContext.SnapshotId);
        Assert.Equal(KnowledgeReadContextKind.Release, _workspaceState.CurrentContext.ReadContext);
        Assert.Equal("v1.0.0", _workspaceState.CurrentContext.DisplayName);
    }

    [Fact]
    public void NonExistentSnapshot_ShowsErrorAndDoesNotFallBackToCurrent()
    {
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo("/knowledge?snapshotId=99999&roleId=Developer");

        var cut = Render<KnowledgePage>();

        var error = cut.Find("[data-testid='knowledge-error']");
        Assert.NotNull(error);
        Assert.Contains("Snapshot", error.TextContent);
        // Kein stiller Fallback auf Current
        Assert.NotEqual(DefaultSnapshotId, _workspaceState.CurrentReadContext.SnapshotId);
    }

    [Fact]
    public void MultipleContextParameters_ShowsMutualExclusionError()
    {
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo("/knowledge?snapshotId=1&releaseId=2&roleId=Developer");

        var cut = Render<KnowledgePage>();

        var error = cut.Find("[data-testid='knowledge-error']");
        Assert.NotNull(error);
        Assert.Contains("höchstens einer der Parameter", error.TextContent);
    }

    [Fact]
    public void O008_MissingRoleInQueryAndStorage_TriggersMandatoryRoleSelector()
    {
        _roleStorage.LastRoleId = null;
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo("/knowledge");

        var cut = Render<KnowledgePage>();

        Assert.True(_contextSelector.IsOpen);
        Assert.Equal(ContextSelectorMode.MandatoryRole, _contextSelector.Mode);
        Assert.Null(_workspaceState.CurrentRoleId);
    }

    [Fact]
    public void O008_InvalidRoleInStorage_TriggersMandatoryRoleSelector()
    {
        _roleStorage.LastRoleId = "NonExistentRole";
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo("/knowledge");

        var cut = Render<KnowledgePage>();

        Assert.True(_contextSelector.IsOpen);
        Assert.Equal(ContextSelectorMode.MandatoryRole, _contextSelector.Mode);
        Assert.Null(_workspaceState.CurrentRoleId);
    }

    [Fact]
    public void Reconnect_PreservesContextAndRole()
    {
        _roleStorage.LastRoleId = "Developer";
        var navMan = Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo($"/knowledge?snapshotId={HistoricalSnapshotId.Value}&roleId=Developer");

        var cut = Render<KnowledgePage>();

        Assert.Equal("Developer", _workspaceState.CurrentRoleId);
        Assert.Equal(HistoricalSnapshotId, _workspaceState.CurrentReadContext.SnapshotId);

        // Zweite Komponente mit denselben Services rendern (simuliert Reconnect / neuen Circuit)
        var reconnectCut = Render<KnowledgePage>();

        Assert.Equal("Developer", _workspaceState.CurrentRoleId);
        Assert.Equal(HistoricalSnapshotId, _workspaceState.CurrentReadContext.SnapshotId);
        Assert.NotNull(reconnectCut.Find("[data-testid='knowledge-page']"));
    }
}
