namespace KnowHowToAI.Core.Domain.Hierarchy;

/// <summary>
/// Stabile Fehlercodes und Detailnamen für Hierarchie-Invarianten.
/// </summary>
public static class HierarchyErrorCodes
{
    public const string DuplicateNodeId = "DuplicateNodeId";
    public const string HierarchyCycle = "HierarchyCycle";
    public const string NodeIdAlreadyUsed = "NodeIdAlreadyUsed";
    public const string NodeNotFound = "NodeNotFound";
    public const string ParentNodeNotFound = "ParentNodeNotFound";
    public const string RootAlreadyExists = "RootAlreadyExists";
    public const string SelfParentNotAllowed = "SelfParentNotAllowed";
    public const string SnapshotMismatch = "SnapshotMismatch";
    public const string TitleRequired = "TitleRequired";
    public const string ExpectedSnapshotIdDetail = "expectedSnapshotId";
    public const string NodeIdDetail = "nodeId";
    public const string ParentNodeIdDetail = "parentNodeId";
    public const string RootCountDetail = "rootCount";
    public const string SnapshotIdDetail = "snapshotId";
}
