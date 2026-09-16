namespace KnowHowToAI.Core.Application.History;

/// <summary>Stabile Fehlercodes für History-Use-Cases.</summary>
public static class HistoryErrorCodes
{
    public const string SnapshotNotFound = "SnapshotNotFound";
    public const string SnapshotNotCommitted = "SnapshotNotCommitted";
    public const string TransactionNotFound = "TransactionNotFound";
    public const string InvalidCursor = "InvalidCursor";
    public const string CursorExpired = "CursorExpired";
    public const string TransactionDiscarded = "TransactionDiscarded";

    public const string SnapshotIdDetail = "snapshotId";
    public const string TransactionIdDetail = "transactionId";
    public const string CursorDetail = "cursor";
}
