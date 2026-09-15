namespace KnowHowToAI.Core.Domain.Dependencies;

/// <summary>
/// Stabile Fehlercodes und Detailnamen für Content-Abhängigkeiten.
/// </summary>
public static class DependencyErrorCodes
{
    public const string InvalidDependency = "InvalidDependency";
    public const string DependencyCycle = "DependencyCycle";

    public const string TargetNodeIdDetail = "targetNodeId";
    public const string TargetRoleIdDetail = "targetRoleId";
    public const string SourceNodeIdDetail = "sourceNodeId";
    public const string SourceRoleIdDetail = "sourceRoleId";
}
