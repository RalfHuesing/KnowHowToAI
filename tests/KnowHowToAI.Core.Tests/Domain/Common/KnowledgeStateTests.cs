using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Tests.Domain.Common;

[Trait("Category", "Unit")]
public sealed class KnowledgeStateTests
{
    [Fact]
    public void StateEnums_ContainAllV1ContractValues()
    {
        Assert.Contains(SnapshotState.Working, Enum.GetValues<SnapshotState>());
        Assert.Contains(SnapshotState.Committed, Enum.GetValues<SnapshotState>());
        Assert.Contains(SnapshotState.Discarded, Enum.GetValues<SnapshotState>());
        Assert.Contains(TransactionState.Open, Enum.GetValues<TransactionState>());
        Assert.Contains(TransactionState.Committed, Enum.GetValues<TransactionState>());
        Assert.Contains(TransactionState.Discarded, Enum.GetValues<TransactionState>());
        Assert.Contains(ContentMode.Independent, Enum.GetValues<ContentMode>());
        Assert.Contains(ContentMode.Derived, Enum.GetValues<ContentMode>());
        Assert.Contains(Availability.Explicit, Enum.GetValues<Availability>());
        Assert.Contains(Availability.Fallback, Enum.GetValues<Availability>());
        Assert.Contains(Availability.None, Enum.GetValues<Availability>());
        Assert.Contains(Freshness.Current, Enum.GetValues<Freshness>());
        Assert.Contains(Freshness.Stale, Enum.GetValues<Freshness>());
    }
}
