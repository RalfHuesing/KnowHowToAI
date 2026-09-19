using Bunit;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.State;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.TestSupport;

/// <summary>
/// bUnit-Kontext für Tests, die die Shell mit dem Hauptlayout rendern:
/// registriert den Seitenbereichs-Slot <see cref="PageRegionState"/>, den
/// Zustand der globalen Toastregion und die schmale JS-Isolation des
/// Hauptlayouts; die Fokusübergabe übernimmt der eingebaute
/// FocusAsync-Handler.
/// </summary>
public abstract class ShellTestContext : BunitContext
{
    protected ShellTestContext()
    {
        Services.AddScoped<PageRegionState>();
        Services.AddScoped<ToastState>();
        Services.AddScoped<WorkspaceState>();
        Services.AddScoped<ContextSelectorState>();
        var module = JSInterop.SetupModule("./Web/Components/Layout/Shell/MainLayout.razor.js");
        module.Mode = JSRuntimeMode.Strict;
        module.SetupVoid("observeBreakpoint", _ => true);

        var dialogModule = JSInterop.SetupModule("./Web/Components/Shared/Dialogs/AppDialog.razor.js");
        dialogModule.Mode = JSRuntimeMode.Loose;
    }
}
