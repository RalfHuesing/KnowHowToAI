namespace KnowHowToAI.Core.Application.Retrieval.Search;

/// <summary>Stabile Fehlercodes und Detailnamen für Search-Use-Cases.</summary>
public static class SearchErrorCodes
{
    public const string SnapshotNotFound = "SnapshotNotFound";
    public const string SnapshotNotCommitted = "SnapshotNotCommitted";
    public const string TransactionNotFound = "TransactionNotFound";
    public const string TransactionClosed = "TransactionClosed";
    public const string InvalidCursor = "InvalidCursor";
    public const string CursorExpired = "CursorExpired";

    public const string SnapshotIdDetail = "snapshotId";
    public const string TransactionIdDetail = "transactionId";
    public const string CursorDetail = "cursor";
    public const string QueryDetail = "query";
}
