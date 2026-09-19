using Bunit;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Components.Layout.Shell;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Components.Layout;

[Trait("Category", "Unit")]
public sealed class NavigationProtectionTests : ShellTestContext
{
    [Fact]
    public void MainLayout_WhenNotDirty_NavigationProceedsDirectly()
    {
        var navManager = Services.GetRequiredService<NavigationManager>();
        var pageRegions = Services.GetRequiredService<PageRegionState>();
        var workspaceState = Services.GetRequiredService<WorkspaceState>();

        var cut = RenderMainLayout();

        pageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Current,
            IsDirty: false));

        navManager.NavigateTo("/search");

        Assert.EndsWith("/search", navManager.Uri, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".confirmation-dialog"));
    }

    [Fact]
    public void MainLayout_WhenDirty_NavigationIsInterceptedAndConfirmationDialogOpened()
    {
        var navManager = Services.GetRequiredService<NavigationManager>();
        var pageRegions = Services.GetRequiredService<PageRegionState>();
        var workspaceState = Services.GetRequiredService<WorkspaceState>();

        var contextVm = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Transaction,
            ContextId: Guid.NewGuid().ToString("D"),
            IsDirty: true);

        var initialUri = navManager.Uri;

        var cut = RenderMainLayout();

        pageRegions.SetKnowledgeContext(contextVm);
        workspaceState.SetContext(contextVm, new ReadContext());
        workspaceState.SetDirty(true);

        // Navigation versuchen
        navManager.NavigateTo("/search");

        // Navigation wurde abgefangen (URL bleibt auf der ursprünglichen Adresse)
        Assert.Equal(initialUri, navManager.Uri);

        // Bestätigungsdialog ist vorhanden
        var dialog = cut.FindComponent<KnowHowToAI.Server.Web.Components.Shared.Dialogs.ConfirmationDialog>();
        Assert.NotNull(dialog);
        Assert.Equal("Ungespeicherte Änderungen", dialog.Instance.Title);
    }

    [Fact]
    public async Task MainLayout_WhenDirty_ConfirmingLeaveNavigatesAndClearsDirty()
    {
        var navManager = Services.GetRequiredService<NavigationManager>();
        var pageRegions = Services.GetRequiredService<PageRegionState>();
        var workspaceState = Services.GetRequiredService<WorkspaceState>();

        var contextVm = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Transaction,
            ContextId: Guid.NewGuid().ToString("D"),
            IsDirty: true);

        var cut = RenderMainLayout();

        pageRegions.SetKnowledgeContext(contextVm);
        workspaceState.SetContext(contextVm, new ReadContext());
        workspaceState.SetDirty(true);

        navManager.NavigateTo("/search");

        var dialog = cut.FindComponent<KnowHowToAI.Server.Web.Components.Shared.Dialogs.ConfirmationDialog>();
        await cut.InvokeAsync(async () =>
        {
            await dialog.Instance.OnConfirm.InvokeAsync();
        });

        // Nach Bestätigung wird zur Ziel-URL navigiert und IsDirty ist false
        Assert.EndsWith("/search", navManager.Uri, StringComparison.Ordinal);
        Assert.False(workspaceState.IsDirty);
        Assert.False(pageRegions.KnowledgeContext?.IsDirty);
    }

    [Fact]
    public async Task MainLayout_WhenDirty_CancellingLeaveKeepsUserOnPage()
    {
        var navManager = Services.GetRequiredService<NavigationManager>();
        var pageRegions = Services.GetRequiredService<PageRegionState>();
        var workspaceState = Services.GetRequiredService<WorkspaceState>();

        var contextVm = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Transaction,
            ContextId: Guid.NewGuid().ToString("D"),
            IsDirty: true);

        var initialUri = navManager.Uri;

        var cut = RenderMainLayout();

        pageRegions.SetKnowledgeContext(contextVm);
        workspaceState.SetContext(contextVm, new ReadContext());
        workspaceState.SetDirty(true);

        navManager.NavigateTo("/search");

        var dialog = cut.FindComponent<KnowHowToAI.Server.Web.Components.Shared.Dialogs.ConfirmationDialog>();
        await cut.InvokeAsync(async () =>
        {
            await dialog.Instance.OnCancel.InvokeAsync();
        });

        // Benutzer bleibt auf der Seite, IsDirty bleibt true
        Assert.Equal(initialUri, navManager.Uri);
        Assert.True(workspaceState.IsDirty);
        Assert.True(pageRegions.KnowledgeContext?.IsDirty);
    }

    private IRenderedComponent<MainLayout> RenderMainLayout() =>
        Render<MainLayout>(parameters => parameters.Add(p => p.Body, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Inhalt</p>"))));
}
