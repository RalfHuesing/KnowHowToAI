namespace KnowHowToAI.Core.Domain.Common;

public enum SnapshotState
{
    Unknown = 0,
    Working = 1,
    Committed = 2,
    Discarded = 3
}

public enum TransactionState
{
    Unknown = 0,
    Open = 1,
    Committed = 2,
    Discarded = 3
}

public enum ContentMode
{
    Unknown = 0,
    Independent = 1,
    Derived = 2
}

public enum Availability
{
    None = 0,
    Explicit = 1,
    Fallback = 2
}

public enum Freshness
{
    Unknown = 0,
    Current = 1,
    Stale = 2
}
