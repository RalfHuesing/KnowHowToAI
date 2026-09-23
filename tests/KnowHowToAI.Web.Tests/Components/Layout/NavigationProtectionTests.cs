using Bunit;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
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
    public void NavigationProtection_WhenNotDirty_NavigationProceedsDirectly()
    {
        var navManager = Services.GetRequiredService<NavigationManager>();
        var pageRegions = Services.GetRequiredService<PageRegionState>();
        var workspaceState = Services.GetRequiredService<WorkspaceState>();

        var cut = RenderNavigationProtection();

        pageRegions.SetKnowledgeContext(new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Current,
            IsDirty: false));

        navManager.NavigateTo("/search");

        Assert.EndsWith("/search", navManager.Uri, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".confirmation-dialog"));
    }

    [Fact]
    public void NavigationProtection_WhenDirty_NavigationIsInterceptedAndConfirmationDialogOpened()
    {
        var navManager = Services.GetRequiredService<NavigationManager>();
        var pageRegions = Services.GetRequiredService<PageRegionState>();
        var workspaceState = Services.GetRequiredService<WorkspaceState>();

        var contextVm = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Transaction,
            ContextId: Guid.NewGuid().ToString("D"),
            IsDirty: true);

        var initialUri = navManager.Uri;

        var cut = RenderNavigationProtection();

        pageRegions.SetKnowledgeContext(contextVm);
        workspaceState.SetContext(contextVm, new ReadContext());
        Services.GetRequiredService<WorkspaceEditState>().SetDirty(true);

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
    public void NavigationProtection_AllowsMatchingDraftContextSynchronizationWithoutLosingDirtyInput()
    {
        var navManager = Services.GetRequiredService<NavigationManager>();
        var workspaceState = Services.GetRequiredService<WorkspaceState>();
        navManager.NavigateTo("/knowledge?audienceId=Default");
        var transactionId = new TransactionId(Guid.NewGuid());
        var context = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Transaction,
            ContextId: transactionId.Value.ToString("D"),
            IsDirty: true);
        var cut = RenderNavigationProtection();
        workspaceState.SetContext(context, new ReadContext(TransactionId: transactionId));
        Services.GetRequiredService<WorkspaceEditState>().SetDirty(true);

        navManager.NavigateTo($"/knowledge?audienceId=Default&transactionId={transactionId.Value:D}");

        Assert.Contains($"transactionId={transactionId.Value:D}", navManager.Uri, StringComparison.OrdinalIgnoreCase);
        Assert.True(Services.GetRequiredService<WorkspaceEditState>().IsDirty);
        Assert.Empty(cut.FindAll(".confirmation-dialog"));
    }

    [Fact]
    public async Task NavigationProtection_WhenDirty_ConfirmingLeaveNavigatesAndClearsDirty()
    {
        var navManager = Services.GetRequiredService<NavigationManager>();
        var pageRegions = Services.GetRequiredService<PageRegionState>();
        var workspaceState = Services.GetRequiredService<WorkspaceState>();

        var contextVm = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Transaction,
            ContextId: Guid.NewGuid().ToString("D"),
            IsDirty: true);

        var cut = RenderNavigationProtection();

        pageRegions.SetKnowledgeContext(contextVm);
        workspaceState.SetContext(contextVm, new ReadContext());
        Services.GetRequiredService<WorkspaceEditState>().SetDirty(true);

        navManager.NavigateTo("/search");

        var dialog = cut.FindComponent<KnowHowToAI.Server.Web.Components.Shared.Dialogs.ConfirmationDialog>();
        await cut.InvokeAsync(async () =>
        {
            await dialog.Instance.OnConfirm.InvokeAsync();
        });

        // Nach Bestätigung wird zur Ziel-URL navigiert und IsDirty ist false
        Assert.EndsWith("/search", navManager.Uri, StringComparison.Ordinal);
        Assert.False(Services.GetRequiredService<WorkspaceEditState>().IsDirty);
        Assert.False(pageRegions.KnowledgeContext?.IsDirty);
    }

    [Fact]
    public async Task NavigationProtection_WhenDirty_CancellingLeaveKeepsUserOnPage()
    {
        var navManager = Services.GetRequiredService<NavigationManager>();
        var pageRegions = Services.GetRequiredService<PageRegionState>();
        var workspaceState = Services.GetRequiredService<WorkspaceState>();

        var contextVm = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Transaction,
            ContextId: Guid.NewGuid().ToString("D"),
            IsDirty: true);

        var initialUri = navManager.Uri;

        var cut = RenderNavigationProtection();

        pageRegions.SetKnowledgeContext(contextVm);
        workspaceState.SetContext(contextVm, new ReadContext());
        Services.GetRequiredService<WorkspaceEditState>().SetDirty(true);

        navManager.NavigateTo("/search");

        var dialog = cut.FindComponent<KnowHowToAI.Server.Web.Components.Shared.Dialogs.ConfirmationDialog>();
        await cut.InvokeAsync(async () =>
        {
            await dialog.Instance.OnCancel.InvokeAsync();
        });

        // Benutzer bleibt auf der Seite, IsDirty bleibt true
        Assert.Equal(initialUri, navManager.Uri);
        Assert.True(Services.GetRequiredService<WorkspaceEditState>().IsDirty);
        Assert.True(pageRegions.KnowledgeContext?.IsDirty);
    }

    private IRenderedComponent<NavigationProtection> RenderNavigationProtection() =>
        Render<NavigationProtection>();
}
