using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgeTreeRequestCoordinatorTests
{
    [Fact]
    public void RegisterRequest_CreatesActiveRequest_AndAssignsIncrementingIds()
    {
        using var coordinator = new KnowledgeTreeRequestCoordinator();
        var nodeId1 = Guid.NewGuid();
        var nodeId2 = Guid.NewGuid();

        var req1 = coordinator.RegisterRequest(nodeId1, generation: 1, CancellationToken.None);
        var req2 = coordinator.RegisterRequest(nodeId2, generation: 1, CancellationToken.None);

        Assert.NotNull(req1);
        Assert.NotNull(req2);
        Assert.True(req2.RequestId > req1.RequestId);
        Assert.False(req1.Cts.IsCancellationRequested);
        Assert.False(req2.Cts.IsCancellationRequested);
    }

    [Fact]
    public void RegisterRequest_WhenNodeAlreadyHasActiveRequest_CancelsAndDisposesPreviousRequest()
    {
        using var coordinator = new KnowledgeTreeRequestCoordinator();
        var nodeId = Guid.NewGuid();

        var req1 = coordinator.RegisterRequest(nodeId, generation: 1, CancellationToken.None);
        var req2 = coordinator.RegisterRequest(nodeId, generation: 1, CancellationToken.None);

        Assert.True(req1.Cts.IsCancellationRequested);
        Assert.False(req2.Cts.IsCancellationRequested);
        Assert.True(req2.RequestId > req1.RequestId);
    }

    [Fact]
    public void IsCurrentRequest_ReturnsTrueForMatchingGenerationAndId_AndFalseOtherwise()
    {
        using var coordinator = new KnowledgeTreeRequestCoordinator();
        var nodeId = Guid.NewGuid();

        var req = coordinator.RegisterRequest(nodeId, generation: 1, CancellationToken.None);

        Assert.True(coordinator.IsCurrentRequest(nodeId, req.RequestId, generation: 1, currentGeneration: 1));
        Assert.False(coordinator.IsCurrentRequest(nodeId, req.RequestId, generation: 1, currentGeneration: 2));
        Assert.False(coordinator.IsCurrentRequest(nodeId, req.RequestId + 1, generation: 1, currentGeneration: 1));
        Assert.False(coordinator.IsCurrentRequest(Guid.NewGuid(), req.RequestId, generation: 1, currentGeneration: 1));
    }

    [Fact]
    public void TryCompleteRequest_WhenCurrent_RemovesRequestAndDisposesCts()
    {
        using var coordinator = new KnowledgeTreeRequestCoordinator();
        var nodeId = Guid.NewGuid();

        var req = coordinator.RegisterRequest(nodeId, generation: 1, CancellationToken.None);
        var completed = coordinator.TryCompleteRequest(nodeId, req, currentGeneration: 1);

        Assert.True(completed);
        Assert.False(coordinator.IsCurrentRequest(nodeId, req.RequestId, generation: 1, currentGeneration: 1));
    }

    [Fact]
    public void TryCompleteRequest_WhenGenerationMismatch_ReturnsFalse()
    {
        using var coordinator = new KnowledgeTreeRequestCoordinator();
        var nodeId = Guid.NewGuid();

        var req = coordinator.RegisterRequest(nodeId, generation: 1, CancellationToken.None);
        var completed = coordinator.TryCompleteRequest(nodeId, req, currentGeneration: 2);

        Assert.False(completed);
    }

    [Fact]
    public void CancelRequest_CancelsAndDisposesOnlySpecifiedNode()
    {
        using var coordinator = new KnowledgeTreeRequestCoordinator();
        var nodeId1 = Guid.NewGuid();
        var nodeId2 = Guid.NewGuid();

        var req1 = coordinator.RegisterRequest(nodeId1, generation: 1, CancellationToken.None);
        var req2 = coordinator.RegisterRequest(nodeId2, generation: 1, CancellationToken.None);

        coordinator.CancelRequest(nodeId1);

        Assert.True(req1.Cts.IsCancellationRequested);
        Assert.False(req2.Cts.IsCancellationRequested);
    }

    [Fact]
    public void CancelAll_CancelsAllActiveRequests_AndResetsGlobalToken()
    {
        using var coordinator = new KnowledgeTreeRequestCoordinator();
        var oldGlobalToken = coordinator.GlobalToken;
        var nodeId1 = Guid.NewGuid();
        var nodeId2 = Guid.NewGuid();

        var req1 = coordinator.RegisterRequest(nodeId1, generation: 1, CancellationToken.None);
        var req2 = coordinator.RegisterRequest(nodeId2, generation: 1, CancellationToken.None);

        coordinator.CancelAll();

        Assert.True(req1.Cts.IsCancellationRequested);
        Assert.True(req2.Cts.IsCancellationRequested);
        Assert.True(oldGlobalToken.IsCancellationRequested);
        Assert.False(coordinator.GlobalToken.IsCancellationRequested);
    }

    [Fact]
    public void Dispose_CancelsAllAndPreventsNewRegistrations()
    {
        var coordinator = new KnowledgeTreeRequestCoordinator();
        var nodeId = Guid.NewGuid();
        var req = coordinator.RegisterRequest(nodeId, generation: 1, CancellationToken.None);

        coordinator.Dispose();

        Assert.True(req.Cts.IsCancellationRequested);
        Assert.Throws<ObjectDisposedException>(() =>
            coordinator.RegisterRequest(nodeId, generation: 1, CancellationToken.None));
    }
}
