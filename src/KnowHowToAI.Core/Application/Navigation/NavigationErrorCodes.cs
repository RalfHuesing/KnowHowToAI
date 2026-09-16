namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>Stabile Fehlercodes und Detailnamen für Navigation-Use-Cases.</summary>
public static class NavigationErrorCodes
{
    public const string NodeNotFound = "NodeNotFound";
    public const string InvalidNodeId = "InvalidNodeId";
    public const string SnapshotNotFound = "SnapshotNotFound";
    public const string SnapshotNotCommitted = "SnapshotNotCommitted";
    public const string TransactionNotFound = "TransactionNotFound";
    public const string TransactionClosed = "TransactionClosed";
    public const string InvalidCursor = "InvalidCursor";
    public const string CursorExpired = "CursorExpired";

    public const string NodeIdDetail = "nodeId";
    public const string ParentNodeIdDetail = "parentNodeId";
    public const string RootNodeIdDetail = "rootNodeId";
    public const string SnapshotIdDetail = "snapshotId";
    public const string TransactionIdDetail = "transactionId";
    public const string CursorDetail = "cursor";
}
