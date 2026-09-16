using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Runtime;

/// <summary>
/// Erzeugt zufallsbasierte GUID-Identifizierer (Version 4) für Transactions,
/// Nodes und Content-Revisions. Produktive Implementierung des Ports.
/// </summary>
public sealed class GuidIdentifierGenerator : IIdentifierGenerator
{
    public TransactionId CreateTransactionId() => new(Guid.NewGuid());

    public NodeId CreateNodeId() => new(Guid.NewGuid());

    public ContentRevisionId CreateContentRevisionId() => new(Guid.NewGuid());
}
