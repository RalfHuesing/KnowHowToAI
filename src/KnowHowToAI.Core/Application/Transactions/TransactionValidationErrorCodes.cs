namespace KnowHowToAI.Core.Application.Transactions;

/// <summary>Stabile Fehlercodes für die Validierung eines Working Snapshots.</summary>
public static class TransactionValidationErrorCodes
{
    public const string TransactionNotFound = "TransactionNotFound";
    public const string TransactionClosed = "TransactionClosed";
    public const string WorkingSnapshotNotOpen = "WorkingSnapshotNotOpen";
    public const string SnapshotConflict = "SnapshotConflict";
    public const string SnapshotMutationConflict = "SnapshotMutationConflict";
    public const string ChangeVersionConflict = "ChangeVersionConflict";
    public const string TransactionIdDetail = "transactionId";
    public const string ExpectedChangeVersionDetail = "expectedChangeVersion";
    public const string ActualChangeVersionDetail = "actualChangeVersion";
    public const string BaseSnapshotIdDetail = "baseSnapshotId";
    public const string CurrentSnapshotIdDetail = "currentSnapshotId";
}
