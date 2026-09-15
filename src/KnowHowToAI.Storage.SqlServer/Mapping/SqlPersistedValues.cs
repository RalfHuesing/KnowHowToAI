namespace KnowHowToAI.Storage.SqlServer.Mapping;

/// <summary>Die relational gespeicherten Repräsentationen der stabilen Domain-Enums.</summary>
internal static class SqlPersistedValues
{
    public const string SnapshotWorking = "Working";
    public const string SnapshotCommitted = "Committed";
    public const string SnapshotDiscarded = "Discarded";
    public const string TransactionOpen = "Open";
    public const string TransactionCommitted = "Committed";
    public const string TransactionDiscarded = "Discarded";
    public const string ContentIndependent = "Independent";
    public const string ContentDerived = "Derived";
}
