namespace KnowHowToAI.Core.Domain.Content;

/// <summary>
/// Stabile Fehlercodes und Details für punktuelle Content-Änderungen.
/// </summary>
public static class TextOperationCodes
{
    public const string ContentNotFound = "ContentNotFound";
    public const string MultipleTextMatches = "MultipleTextMatches";
    public const string TextNotFound = "TextNotFound";

    public const string MatchCountDetail = "matchCount";
    public const string NodeIdDetail = "nodeId";
    public const string RoleIdDetail = "roleId";
}
