using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Tests.Application.Abstractions;

[Trait("Category", "Unit")]
public sealed class RuntimePortTests
{
    [Fact]
    public void RuntimePorts_CanProvideDeterministicTimeAndIdentifiers()
    {
        var expectedTime = new DateTimeOffset(2026, 9, 15, 10, 30, 0, TimeSpan.Zero);
        IClock clock = new FixedClock(expectedTime);
        IIdentifierGenerator identifiers = new FixedIdentifierGenerator(
            new TransactionId(Guid.Parse("9f4a2c43-0a77-44be-8f98-f403444d3e9f")),
            new NodeId(Guid.Parse("0a77f4a2-2c43-44be-8f98-f403444d3e9f")),
            new ContentRevisionId(Guid.Parse("342c9f4a-0a77-44be-8f98-f403444d3e9f")));

        Assert.Equal(expectedTime, clock.UtcNow);
        Assert.Equal("9f4a2c43-0a77-44be-8f98-f403444d3e9f", identifiers.CreateTransactionId().ToString());
        Assert.Equal("0a77f4a2-2c43-44be-8f98-f403444d3e9f", identifiers.CreateNodeId().ToString());
        Assert.Equal("342c9f4a-0a77-44be-8f98-f403444d3e9f", identifiers.CreateContentRevisionId().ToString());
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FixedIdentifierGenerator(
        TransactionId transactionId,
        NodeId nodeId,
        ContentRevisionId contentRevisionId) : IIdentifierGenerator
    {
        public TransactionId CreateTransactionId() => transactionId;

        public NodeId CreateNodeId() => nodeId;

        public ContentRevisionId CreateContentRevisionId() => contentRevisionId;
    }
}
