using Bunit;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Components.Layout;

[Trait("Category", "Unit")]
public sealed class ContextSelectorDialogTests : BunitContext
{
    private static readonly SnapshotId DefaultSnapshotId = new(1);

    private readonly NavigationTestHarness _harness;
    private readonly NavigationService _navigationService;
    private readonly ContextSelectorState _selectorState;
    private readonly InMemoryAudienceStorageService _audienceStorage;

    private sealed class EmptyContextSelectionCatalog : IContextSelectionCatalog
    {
        public Task<ContextSelectionOptionsViewModel> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ContextSelectionOptionsViewModel.Empty);
    }

    public ContextSelectorDialogTests()
    {
        _harness = new NavigationTestHarness(DefaultSnapshotId);
        _navigationService = _harness.CreateService();
        _selectorState = new ContextSelectorState();
        _audienceStorage = new InMemoryAudienceStorageService();

        Services.AddSingleton(_navigationService);
        Services.AddSingleton(_selectorState);
        Services.AddSingleton(new WorkspaceState());
        Services.AddSingleton<IAudienceStorageService>(_audienceStorage);
        Services.AddSingleton<IContextSelectionCatalog, EmptyContextSelectionCatalog>();
        Services.AddSingleton<IContextSelectionAudienceCatalog>(new ContextSelectionAudienceCatalog(_navigationService));

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task RendersInFullMode_ShowsAllContextOptionsAndCancelButton()
    {
        var cut = Render<ContextSelectorDialog>();

        await cut.InvokeAsync(() => _selectorState.Open(ContextSelectorMode.Full));
        cut.WaitForState(() => cut.FindAll("[data-testid='context-selector-dialog']").Count > 0);

        Assert.Equal("Wissenskontext und Zielgruppe anpassen", cut.Find("h2").TextContent);
        Assert.NotNull(cut.Find("[data-testid='selector-cancel-button']"));
        Assert.NotNull(cut.Find("[data-testid='selector-apply-button']"));
        Assert.Equal("Übernehmen", cut.Find("[data-testid='selector-apply-button']").TextContent.Trim());
        var purpose = cut.Find("[data-testid='selector-purpose']").TextContent;
        Assert.Contains("Zielgruppe", purpose);
        Assert.Contains("keine Zugriffsberechtigung", purpose);

        var options = cut.FindAll(".context-selector-dialog__option");
        Assert.Equal(4, options.Count);
    }

    [Fact]
    public async Task RendersInMandatoryAudienceMode_ShowsOnlyAudienceSelectionWithoutCancelButton()
    {
        var cut = Render<ContextSelectorDialog>();

        await cut.InvokeAsync(() => _selectorState.Open(ContextSelectorMode.MandatoryAudience));
        cut.WaitForState(() => cut.FindAll("[data-testid='context-selector-dialog']").Count > 0);

        Assert.Equal("Zielgruppe auswählen", cut.Find("h2").TextContent);
        Assert.Empty(cut.FindAll("[data-testid='selector-cancel-button']"));
        var applyButton = cut.Find("[data-testid='selector-apply-button']");
        Assert.NotNull(applyButton);
        Assert.Equal("Auswählen", applyButton.TextContent.Trim());
        var purpose = cut.Find("[data-testid='selector-purpose']").TextContent;
        Assert.Contains("in dieser Perspektive geöffnet", purpose);
        Assert.Contains("keine Zugriffsberechtigung", purpose);
        Assert.Empty(cut.FindAll(".context-selector-dialog__options"));
    }

    [Fact]
    public async Task SelectingSnapshot_ShowsInputAndValidatesPositiveInteger()
    {
        var cut = Render<ContextSelectorDialog>();

        await cut.InvokeAsync(() => _selectorState.Open(ContextSelectorMode.Full));
        cut.WaitForState(() => cut.FindAll("[data-testid='context-selector-dialog']").Count > 0);

        var snapshotRadio = cut.Find("input[type='radio'][value='Snapshot']");
        await cut.InvokeAsync(() => snapshotRadio.Change(true));

        var input = cut.Find("[data-testid='snapshot-id-input']");
        Assert.NotNull(input);

        // Klick auf Übernehmen ohne ID führt zu Fehlermeldung
        var applyButton = cut.Find("[data-testid='selector-apply-button']");
        await cut.InvokeAsync(() => applyButton.Click());

        var error = cut.Find("[data-testid='selector-error']");
        Assert.Contains("positive Snapshot-ID", error.TextContent);
    }

    [Fact]
    public async Task SelectingTransaction_ShowsInputAndValidatesGuidFormat()
    {
        var cut = Render<ContextSelectorDialog>();

        await cut.InvokeAsync(() => _selectorState.Open(ContextSelectorMode.Full));
        cut.WaitForState(() => cut.FindAll("[data-testid='context-selector-dialog']").Count > 0);

        var txRadio = cut.Find("input[type='radio'][value='Transaction']");
        await cut.InvokeAsync(() => txRadio.Change(true));

        var input = cut.Find("[data-testid='transaction-id-input']");
        Assert.NotNull(input);

        // Klick auf Übernehmen mit ungültiger GUID führt zu Fehlermeldung
        await cut.InvokeAsync(() => input.Change("not-a-guid"));
        var applyButton = cut.Find("[data-testid='selector-apply-button']");
        await cut.InvokeAsync(() => applyButton.Click());

        var error = cut.Find("[data-testid='selector-error']");
        Assert.Contains("Transaktions-ID", error.TextContent);
    }

    [Fact]
    public async Task SelectingAudienceAndApplying_SavesToAudienceStorageAndNavigates()
    {
        var cut = Render<ContextSelectorDialog>();

        await cut.InvokeAsync(() => _selectorState.Open(ContextSelectorMode.Full));
        cut.WaitForState(() => cut.FindAll("[data-testid='audience-option-Developer']").Count > 0);

        var audienceOption = cut.Find("[data-testid='audience-option-Developer'] input");
        await cut.InvokeAsync(() => audienceOption.Change(true));

        var applyButton = cut.Find("[data-testid='selector-apply-button']");
        await cut.InvokeAsync(() => applyButton.Click());

        Assert.Equal("Developer", _audienceStorage.LastAudienceId);
        var navMan = Services.GetRequiredService<NavigationManager>();
        Assert.Contains("audienceId=Developer", navMan.Uri);
        Assert.False(_selectorState.IsOpen);
    }

    [Fact]
    public async Task EmptyAudienceList_DisplaysEmptyMessage()
    {
        var emptySnapshotId = new SnapshotId(999);
        var emptyHarness = new NavigationTestHarness(emptySnapshotId);
        // Zielgruppe für den Snapshot löschen
        var emptyService = emptyHarness.CreateService();

        Services.AddSingleton(emptyService);

        var cut = Render<ContextSelectorDialog>();
        await cut.InvokeAsync(() => _selectorState.Open(
            ContextSelectorMode.MandatoryAudience,
            new ReadContext(SnapshotId: new SnapshotId(404))));

        cut.WaitForState(() => cut.FindAll("[data-testid='context-selector-dialog']").Count > 0);

        // Da Snapshot 404 nicht existiert / keine Zielgruppen hat
        var emptyMessage = cut.Find("[data-testid='empty-audiences-message']");
        Assert.NotNull(emptyMessage);
    }

    [Fact]
    public async Task Cancel_ClosesDialogWithoutNavigation()
    {
        var navMan = Services.GetRequiredService<NavigationManager>();
        var initialUri = navMan.Uri;

        var cut = Render<ContextSelectorDialog>();

        await cut.InvokeAsync(() => _selectorState.Open(ContextSelectorMode.Full));
        cut.WaitForState(() => cut.FindAll("[data-testid='context-selector-dialog']").Count > 0);

        var cancelButton = cut.Find("[data-testid='selector-cancel-button']");
        await cut.InvokeAsync(() => cancelButton.Click());

        Assert.False(_selectorState.IsOpen);
        Assert.Equal(initialUri, navMan.Uri);
    }

    [Fact]
    public async Task AudienceCatalog_LoadsAudiencesBeyondTheFirstOpaquePage()
    {
        for (var index = 0; index < 101; index++)
        {
            var id = new AudienceId($"audience-{index:D3}");
            _harness.AddAudience(new Audience(DefaultSnapshotId, id, id.Value, null, false));
        }

        var catalog = new ContextSelectionAudienceCatalog(_navigationService);
        var result = await catalog.LoadAsync(new ReadContext());

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Audiences, Audience => Audience.Id == "audience-100");
        Assert.Equal(102, result.Audiences.Count);
    }
}
