using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Features.Knowledge;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgePageChangeVersionTests
{
    [Fact]
    public void Resolve_KeepsLatestVersionForTheSameTransaction()
    {
        var transactionId = new TransactionId(Guid.NewGuid());

        var version = KnowledgePageChangeVersion.Resolve(
            new ReadContext(TransactionId: transactionId), transactionId, 5, 4);

        Assert.Equal(5, version);
    }

    [Fact]
    public void Resolve_UsesVersionFromAChangedTransaction()
    {
        var resolvedTransaction = new TransactionId(Guid.NewGuid());
        var activeTransaction = new TransactionId(Guid.NewGuid());

        var version = KnowledgePageChangeVersion.Resolve(
            new ReadContext(TransactionId: resolvedTransaction), activeTransaction, 5, 1);

        Assert.Equal(1, version);
    }
}
