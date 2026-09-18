using Bunit;
using KnowHowToAI.Server.Web.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KnowHowToAI.Web.Tests.TestSupport;

[Trait("Category", "Unit")]
public sealed class UiBasisShowcaseTests : BunitContext
{
    private const string ModulePath = "./Web/Components/Shared/AppDialog.razor.js";

    [Fact]
    public void RendersLabeledFormTableHintAndDialog()
    {
        var (cut, _) = RenderFixture();

        Assert.Equal("Name", cut.Find("label[for=showcase-name]").TextContent);
        Assert.Equal(
            "showcase-name-hint",
            cut.Find("input#showcase-name").GetAttribute("aria-describedby"));
        Assert.Equal(
            "Der Name ist Pflichtfeld und wird beim Speichern geprüft.",
            cut.Find("[data-testid=showcase-hint]").TextContent);
        Assert.Equal("Rollenübersicht", cut.Find("[data-testid=showcase-table] caption").TextContent);
        Assert.Equal(2, cut.FindAll("[data-testid=showcase-table] tbody tr").Count);
        Assert.Equal("Beispieldialog", cut.Find("dialog h2").TextContent);
    }

    [Fact]
    public void RejectsInvalidSubmitAndAcceptsValidSubmit()
    {
        var (cut, _) = RenderFixture();

        cut.Find("[data-testid=showcase-submit]").Click();

        Assert.Contains(
            "Bitte geben Sie einen Namen an.",
            cut.Find("[data-testid=showcase-name-error]").TextContent,
            StringComparison.Ordinal);

        cut.Find("#showcase-name").Change("Beispielname");
        cut.Find("[data-testid=showcase-submit]").Click();

        Assert.Contains(
            "Die Formularprüfung war erfolgreich.",
            cut.Find("[data-testid=showcase-submit-status]").TextContent,
            StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid=showcase-name-error]"));
    }

    [Fact]
    public void ShowsToastAfterTheButtonIsClicked()
    {
        var (cut, _) = RenderFixture();

        Assert.Empty(cut.FindAll("[data-testid=showcase-toast]"));

        cut.Find("[data-testid=showcase-toast-button]").Click();

        var toast = cut.Find("[data-testid=showcase-toast]");
        Assert.Equal("status", toast.GetAttribute("role"));
        Assert.Contains("Die Aktion wurde abgeschlossen.", toast.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void InitializesAndOperatesTheDialogThroughTheIsolatedModule()
    {
        var (cut, module) = RenderFixture();

        cut.Find("[data-testid=showcase-dialog-open]").Click();
        cut.Find("[data-testid=showcase-dialog-close]").Click();

        Assert.Equal(
            ["initialize", "show", "close"],
            module.Invocations.Select(invocation => invocation.Identifier).ToArray());
    }

    [Fact]
    public async Task FiresTheClosedContractWhenTheNativeCloseCallbackReachesTheComponent()
    {
        var (cut, module) = RenderFixture();

        cut.Find("[data-testid=showcase-dialog-open]").Click();
        Assert.Contains(
            "Der Dialog ist geöffnet.",
            cut.Find("[data-testid=showcase-dialog-status]").TextContent,
            StringComparison.Ordinal);

        var selfReference = GetDialogSelfReference(module);
        await cut.InvokeAsync(() => selfReference.Value.NotifyDialogClosedAsync());

        cut.WaitForAssertion(() => Assert.Contains(
            "Der Dialog wurde geschlossen.",
            cut.Find("[data-testid=showcase-dialog-status]").TextContent,
            StringComparison.Ordinal));
    }

    private DotNetObjectReference<AppDialog> GetDialogSelfReference(BunitJSModuleInterop module)
    {
        var initializeInvocation = module.Invocations["initialize"].Single();
        return Assert.IsType<DotNetObjectReference<AppDialog>>(initializeInvocation.Arguments[1]);
    }

    private (IRenderedComponent<UiBasisShowcase> Component, BunitJSModuleInterop Module) RenderFixture()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Strict;
        module.SetupVoid("initialize", _ => true);
        module.SetupVoid("show", _ => true);
        module.SetupVoid("close", _ => true);

        return (Render<UiBasisShowcase>(), module);
    }
}
