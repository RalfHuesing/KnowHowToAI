using System.Globalization;

namespace KnowHowToAI.Core.Domain.Common;

public readonly record struct SnapshotId(long Value)
{
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public readonly record struct TransactionId(Guid Value)
{
    public override string ToString() => Value.ToString("D");
}

public readonly record struct NodeId(Guid Value)
{
    public override string ToString() => Value.ToString("D");
}

public readonly record struct AudienceId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct ContentRevisionId(Guid Value)
{
    public override string ToString() => Value.ToString("D");
}

public readonly record struct ReleaseId(long Value)
{
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
