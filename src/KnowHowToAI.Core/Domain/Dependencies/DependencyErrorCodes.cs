namespace KnowHowToAI.Core.Domain.Dependencies;

/// <summary>
/// Stabile Fehlercodes und Detailnamen für Content-Abhängigkeiten.
/// </summary>
public static class DependencyErrorCodes
{
    public const string InvalidDependency = "InvalidDependency";
    public const string DependencyCycle = "DependencyCycle";

    public const string TargetNodeIdDetail = "targetNodeId";
    public const string TargetAudienceIdDetail = "targetAudienceId";
    public const string SourceNodeIdDetail = "sourceNodeId";
    public const string SourceAudienceIdDetail = "sourceAudienceId";
}
