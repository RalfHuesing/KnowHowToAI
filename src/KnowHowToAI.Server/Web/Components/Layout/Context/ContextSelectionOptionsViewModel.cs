namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Zusammengehörige, bereits für die Darstellung aufbereitete Auswahlwerte des Kontextselektors.
/// </summary>
public sealed record ContextSelectionOptionsViewModel(
    IReadOnlyList<ContextSelectionReleaseOptionViewModel> Releases,
    IReadOnlyList<ContextSelectionTransactionOptionViewModel> Transactions,
    IReadOnlyList<ContextSelectionAudienceOptionViewModel> Audiences)
{
    public static ContextSelectionOptionsViewModel Empty { get; } = new([], [], []);

    public ContextSelectionOptionsViewModel WithAudiences(IReadOnlyList<ContextSelectionAudienceOptionViewModel> audiences) =>
        this with { Audiences = audiences };
}
