namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Zusammengehörige, bereits für die Darstellung aufbereitete Auswahlwerte des Kontextselektors.
/// </summary>
public sealed record ContextSelectionOptionsViewModel(
    IReadOnlyList<ContextSelectionReleaseOptionViewModel> Releases,
    IReadOnlyList<ContextSelectionTransactionOptionViewModel> Transactions,
    IReadOnlyList<ContextSelectionRoleOptionViewModel> Roles)
{
    public static ContextSelectionOptionsViewModel Empty { get; } = new([], [], []);

    public ContextSelectionOptionsViewModel WithRoles(IReadOnlyList<ContextSelectionRoleOptionViewModel> roles) =>
        this with { Roles = roles };
}
