using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.IntegrationTests.TestSupport;

/// <summary>
/// Erzeugt streng fortlaufende, vorab bekannte IDs für Node, ContentRevision und
/// Transaction. Ermöglicht Abnahmetests mit festen, zufallsfreien Identifizierern,
/// ohne die produktive ID-Erzeugung zu umgehen.
/// </summary>
public sealed class SequentialIdentifierGenerator : IIdentifierGenerator
{
    private int _counter;

    public TransactionId CreateTransactionId() => new(NextId());

    public NodeId CreateNodeId() => new(NextId());

    public ContentRevisionId CreateContentRevisionId() => new(NextId());

    private Guid NextId() =>
        Guid.Parse($"9{Interlocked.Increment(ref _counter):D7}-0000-0000-0000-000000000000");
}
