using KnowHowToAI.Core.Application.Navigation;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Rein darstellender Eingabeteil für die Wahl des Lesekontexts.
/// </summary>
public sealed partial class ContextReadContextOptions : ComponentBase
{
    [Parameter, EditorRequired]
    public ContextSelectionDraft Draft { get; set; } = default!;

    [Parameter, EditorRequired]
    public ContextSelectionOptionsViewModel Options { get; set; } = default!;

    [Parameter, EditorRequired]
    public EventCallback<KnowledgeReadContextKind> OnKindChanged { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnSelectionChanged { get; set; }

    private Task SelectKindAsync(KnowledgeReadContextKind kind) => OnKindChanged.InvokeAsync(kind);

    private Task NotifySelectionChangedAsync() => OnSelectionChanged.InvokeAsync();
}
