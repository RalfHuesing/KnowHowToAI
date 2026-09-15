using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Abstractions.Runtime;

public interface IIdentifierGenerator
{
    TransactionId CreateTransactionId();

    NodeId CreateNodeId();

    ContentRevisionId CreateContentRevisionId();
}
