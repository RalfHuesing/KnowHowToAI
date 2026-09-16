namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>
/// Stabile Fehlercodes und Details für globale Node-Löschungen.
/// </summary>
public static class NodeDeletionErrorCodes
{
    public const string NodeHasChildren = "NodeHasChildren";

    public const string ActiveChildCountDetail = "activeChildCount";
}
