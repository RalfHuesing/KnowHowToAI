using Bunit;
using KnowHowToAI.Server.Web.Components.Shared.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KnowHowToAI.Web.Tests.Components.Shared;

[Trait("Category", "Unit")]
public sealed class ConfirmationDialogTests : BunitContext
{
    private const string ModulePath = "./Web/Components/Shared/Dialogs/AppDialog.razor.js";

    private sealed class DialogCallbacks
    {
        private readonly bool _gateConfirm;

        public DialogCallbacks(bool gateConfirm) => _gateConfirm = gateConfirm;

        public int ConfirmCount { get; private set; }

        public int CancelCount { get; private set; }

        public TaskCompletionSource ConfirmGate { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ConfirmAsync()
        {
            ConfirmCount++;
            return _gateConfirm ? ConfirmGate.Task : Task.CompletedTask;
        }

        public void Cancel() => CancelCount++;
    }

    [Fact]
    public async Task OpensThroughTheIsolatedModuleWithTheSafeActionAsFirstElement()
    {
        var (cut, module, _) = RenderDialog();

        await cut.Instance.OpenAsync();

        Assert.Equal(
            ["initialize", "show"],
            module.Invocations.Select(invocation => invocation.Identifier).ToArray());
        var actions = cut.FindAll(".confirmation-dialog__actions button").ToArray();
        Assert.Equal(2, actions.Length);
        Assert.Equal("dialog", cut.Find("dialog").GetAttribute("role"));
        Assert.Equal("Abbrechen", actions[0].TextContent.Trim());
        Assert.Equal("Verwerfen", actions[1].TextContent.Trim());
        Assert.Equal("Transaktion verwerfen", cut.Find("dialog h2").TextContent);
    }

    [Fact]
    public async Task DescribesTheConsequenceAndNeedsNoTextConfirmation()
    {
        var (cut, _, _) = RenderDialog(isDestructive: true);

        var message = cut.Find(".confirmation-dialog__message");
        var confirmButton = cut.Find(".confirmation-dialog__actions button:nth-child(2)");

        Assert.Contains(
            "Alle Änderungen der Working Transaction gehen verloren.",
            message.TextContent,
            StringComparison.Ordinal);
        Assert.Equal(message.GetAttribute("id"), confirmButton.GetAttribute("aria-describedby"));
        Assert.Empty(cut.FindAll(".confirmation-dialog__actions input"));
    }

    [Fact]
    public async Task ConfirmsExactlyOnceOnADoubleClickWhileTheRequestIsRunning()
    {
        var (cut, module, callbacks) = RenderDialog(gateConfirm: true);
        await cut.Instance.OpenAsync();

        cut.Find(".confirmation-dialog__button--primary").Click();
        cut.Find(".confirmation-dialog__button--primary").Click();

        Assert.Equal(1, callbacks.ConfirmCount);
        Assert.Equal(0, callbacks.CancelCount);
        Assert.Empty(module.Invocations.Where(invocation => invocation.Identifier == "close"));

        callbacks.ConfirmGate.SetResult();
        cut.WaitForAssertion(() => Assert.Empty(
            cut.FindAll(".confirmation-dialog__actions button[disabled]")));
    }

    [Fact]
    public async Task BlocksBothActionsWhileTheConfirmRequestIsRunning()
    {
        var (cut, _, callbacks) = RenderDialog(gateConfirm: true);
        await cut.Instance.OpenAsync();

        cut.Find(".confirmation-dialog__button--primary").Click();

        Assert.Equal(2, cut.FindAll(".confirmation-dialog__actions button[disabled]").Count);

        callbacks.ConfirmGate.SetResult();
        cut.WaitForAssertion(() => Assert.Empty(
            cut.FindAll(".confirmation-dialog__actions button[disabled]")));
        Assert.Equal(1, callbacks.ConfirmCount);
    }

    [Fact]
    public async Task TreatsEscapeLikeCancelExactlyOnce()
    {
        var (cut, module, callbacks) = RenderDialog();
        await cut.Instance.OpenAsync();
        var selfReference = GetDialogSelfReference(module);

        await cut.InvokeAsync(() => selfReference.Value.NotifyDialogClosedAsync());

        cut.WaitForAssertion(() => Assert.Equal(1, callbacks.CancelCount));
        Assert.Equal(0, callbacks.ConfirmCount);
        Assert.Empty(module.Invocations.Where(invocation => invocation.Identifier == "close"));
    }

    [Fact]
    public async Task CancelsExactlyOnceOnADoubleClickOfTheCancelButton()
    {
        var (cut, module, callbacks) = RenderDialog();
        await cut.Instance.OpenAsync();

        cut.Find(".confirmation-dialog__button--secondary").Click();
        cut.Find(".confirmation-dialog__button--secondary").Click();

        Assert.Equal(1, callbacks.CancelCount);
        Assert.Equal(0, callbacks.ConfirmCount);
        Assert.Single(module.Invocations.Where(invocation => invocation.Identifier == "close"));
    }

    [Fact]
    public async Task ClosesProgrammaticallyWithoutTriggeringCancel()
    {
        var (cut, module, callbacks) = RenderDialog();
        await cut.Instance.OpenAsync();

        await cut.Instance.CloseAsync();
        await cut.InvokeAsync(() => GetDialogSelfReference(module).Value.NotifyDialogClosedAsync());

        Assert.Equal(0, callbacks.CancelCount);
    }

    [Theory]
    [InlineData(true, "confirmation-dialog__button--destructive", 1)]
    [InlineData(false, "confirmation-dialog__button--primary", 0)]
    public async Task MarksOnlyTheDestructiveActionUnambiguouslyAsDestructive(
        bool isDestructive, string expectedClass, int expectedWarningIconCount)
    {
        var (cut, _, _) = RenderDialog(isDestructive: isDestructive);
        await cut.Instance.OpenAsync();

        var confirmButton = cut.Find(".confirmation-dialog__actions button:nth-child(2)");

        Assert.Contains(
            expectedClass,
            confirmButton.GetAttribute("class")!,
            StringComparison.Ordinal);
        Assert.Equal(
            expectedWarningIconCount,
            confirmButton.QuerySelectorAll(".confirmation-dialog__button-icon").Count());
    }

    private static DotNetObjectReference<AppDialog> GetDialogSelfReference(
        BunitJSModuleInterop module) =>
        Assert.IsType<DotNetObjectReference<AppDialog>>(
            module.Invocations["initialize"].Single().Arguments[1]);

    private (IRenderedComponent<ConfirmationDialog> Component, BunitJSModuleInterop Module, DialogCallbacks Callbacks) RenderDialog(
        bool isDestructive = false, bool gateConfirm = false)
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Strict;
        module.SetupVoid("initialize", _ => true).SetVoidResult();
        module.SetupVoid("show", _ => true).SetVoidResult();
        module.SetupVoid("close", _ => true).SetVoidResult();

        var callbacks = new DialogCallbacks(gateConfirm);

        var component = Render<ConfirmationDialog>(parameters =>
        {
            parameters.Add(dialog => dialog.Title, "Transaktion verwerfen");
            parameters.Add(
                dialog => dialog.Message,
                "Alle Änderungen der Working Transaction gehen verloren.");
            parameters.Add(dialog => dialog.ConfirmText, "Verwerfen");
            parameters.Add(dialog => dialog.IsDestructive, isDestructive);
            parameters.Add(dialog => dialog.OnConfirm, EventCallback.Factory.Create(
                this,
                callbacks.ConfirmAsync));
            parameters.Add(dialog => dialog.OnCancel, EventCallback.Factory.Create(
                this,
                callbacks.Cancel));
        });

        return (component, module, callbacks);
    }
}
