using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Components.Layout;

/// <summary>
/// Globale Kontextleiste nahe der Wortmarke: stellt Wissensstand
/// (Read-Context-Art mit optionaler ID/Bezeichnung), Rolle und
/// Änderungszustand aus genau einem <see cref="KnowledgeContextViewModel"/>
/// dar. Ohne Selektor, Links oder Mutation; nicht vorhandene Angaben
/// erscheinen nicht, statt Dummy-Daten zu zeigen.
/// </summary>
public sealed partial class KnowledgeContextBar : ComponentBase
{
    [Parameter, EditorRequired]
    public KnowledgeContextViewModel Context { get; set; } = default!;

    private string ReadContextText =>
        Context.ReadContext == KnowledgeReadContextKind.Current
            ? "Current Snapshot"
            : Context.ReadContext.ToString();

    private string? ReadContextDetail =>
        FirstNonEmpty(Context.DisplayName, Context.ContextId);

    private string RoleText =>
        string.IsNullOrWhiteSpace(Context.RoleName)
            ? "Keine Rolle ausgewählt"
            : Context.RoleName;

    private static string? FirstNonEmpty(params string?[] values) =>
        Array.Find(values, value => !string.IsNullOrWhiteSpace(value));
}
