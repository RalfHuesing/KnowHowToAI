using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Web.Tests.State;

[Trait("Category", "Unit")]
public sealed class KnowledgePageContextResolverTests
{
    [Fact]
    public async Task MissingDraftId_ResolvesCurrentSnapshot()
    {
        var harness = new NavigationTestHarness(new SnapshotId(7));
        var repositories = harness.CreateRepositories();
        var resolver = new KnowledgePageContextResolver(repositories.Snapshots, repositories.Transactions);

        var result = await resolver.ResolveAsync(null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.ReadContext.TransactionId);
        Assert.Equal(KnowledgeReadContextKind.Current, result.Value.ContextViewModel.ReadContext);
        Assert.Equal(7, result.Value.LoadedSnapshotId);
    }

    [Fact]
    public async Task OpenDraftId_ResolvesItsWorkingSnapshotAndChangeVersion()
    {
        var harness = new NavigationTestHarness(new SnapshotId(7));
        var transaction = new KnowledgeTransaction(
            new TransactionId(Guid.NewGuid()), new SnapshotId(7), new SnapshotId(8), TransactionState.Open,
            ChangeVersion: 3, CreatedAtUtc: DateTimeOffset.UnixEpoch, CommittedAtUtc: null,
            Purpose: "Entwurf", Actor: "Test", Client: "Web UI", CommitMessage: null);
        harness.SetTransaction(transaction);
        var repositories = harness.CreateRepositories();
        var resolver = new KnowledgePageContextResolver(repositories.Snapshots, repositories.Transactions);

        var result = await resolver.ResolveAsync(transaction.TransactionId.Value.ToString("D"));

        Assert.True(result.IsSuccess);
        Assert.Equal(transaction.TransactionId, result.Value!.ReadContext.TransactionId);
        Assert.Equal(KnowledgeReadContextKind.Transaction, result.Value.ContextViewModel.ReadContext);
        Assert.Equal(7, result.Value.LoadedSnapshotId);
        Assert.Equal(3, result.Value.ChangeVersion);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000099")]
    public async Task InvalidOrMissingDraftId_ReturnsReadContextError(string transactionId)
    {
        var harness = new NavigationTestHarness(new SnapshotId(7));
        var repositories = harness.CreateRepositories();
        var resolver = new KnowledgePageContextResolver(repositories.Snapshots, repositories.Transactions);

        var result = await resolver.ResolveAsync(transactionId);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Error!.Code, new[] { ReadContextErrorCodes.InvalidReadContext, ReadContextErrorCodes.TransactionNotFound });
    }
}
