using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.Navigation;

[Trait("Category", "Unit")]
public sealed class NavigationServiceAudienceTests
{
    private static readonly SnapshotId CurrentSnapshotId = new(10);
    private static readonly NodeId RootNodeId = new(Guid.Parse("b764fc68-d485-4bca-8617-b33a51d838ae"));
    private static readonly NodeId Child1NodeId = new(Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly AudienceId AudienceDeveloper = new("Developer");
    private static readonly AudienceId AudienceArchived = new("Archived");
    private static readonly AudienceId MissingAudience = new("MissingAudience");

    [Fact]
    public async Task GetRootAsync_EmptySnapshot_WithValidAudience_ReturnsAvailabilityNoneAndNullNode()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        var service = testHarness.CreateService();

        var result = await service.GetRootAsync(new ReadContext(), AudienceDeveloper);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Node);
        Assert.Equal(Availability.None, result.Value.Availability);
        Assert.Equal(AudienceDeveloper, result.Value.RequestedAudienceId);
        Assert.Null(result.Value.Content);
        Assert.Equal(Freshness.Unknown, result.Value.Freshness);
    }

    [Fact]
    public async Task GetRootAsync_EmptySnapshot_WithMissingRequestedAudience_ReturnsRequestedAudienceNotFound()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        var service = testHarness.CreateService();

        var result = await service.GetRootAsync(new ReadContext(), MissingAudience);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.RequestedAudienceNotFound, result.Code);
        Assert.Equal(MissingAudience.ToString(), result.Details[AudienceResolutionErrorCodes.RequestedAudienceIdDetail]);
    }

    [Fact]
    public async Task GetRootAsync_EmptySnapshot_WithDeletedRequestedAudience_ReturnsRequestedAudienceDeleted()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, true));
        var service = testHarness.CreateService();

        var result = await service.GetRootAsync(new ReadContext(), AudienceDeveloper);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.RequestedAudienceDeleted, result.Code);
        Assert.Equal(AudienceDeveloper.ToString(), result.Details[AudienceResolutionErrorCodes.RequestedAudienceIdDetail]);
    }

    [Fact]
    public async Task GetRootAsync_EmptySnapshot_WithDeletedCandidateAudience_ReturnsCandidateAudienceDeleted()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, false));
        testHarness.AddAudience(new Audience(CurrentSnapshotId, AudienceArchived, "Archived", null, true));
        testHarness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, AudienceDeveloper, AudienceArchived, 2));
        var service = testHarness.CreateService();

        var result = await service.GetRootAsync(new ReadContext(), AudienceDeveloper);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.CandidateAudienceDeleted, result.Code);
        Assert.Equal(AudienceArchived.ToString(), result.Details[AudienceResolutionErrorCodes.CandidateAudienceIdDetail]);
    }

    [Fact]
    public async Task GetNodeAsync_ExistingNode_WithMissingRequestedAudience_ReturnsRequestedAudienceNotFound()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", "Purpose 1", 10, false));
        var service = testHarness.CreateService();

        var result = await service.GetNodeAsync(Child1NodeId, new ReadContext(), MissingAudience);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.RequestedAudienceNotFound, result.Code);
        Assert.Equal(MissingAudience.ToString(), result.Details[AudienceResolutionErrorCodes.RequestedAudienceIdDetail]);
    }

    [Fact]
    public async Task GetNodeAsync_ExistingNode_WithDeletedRequestedAudience_ReturnsRequestedAudienceDeleted()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", "Purpose 1", 10, false));
        testHarness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, true));
        var service = testHarness.CreateService();

        var result = await service.GetNodeAsync(Child1NodeId, new ReadContext(), AudienceDeveloper);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.RequestedAudienceDeleted, result.Code);
        Assert.Equal(AudienceDeveloper.ToString(), result.Details[AudienceResolutionErrorCodes.RequestedAudienceIdDetail]);
    }

    [Fact]
    public async Task ListChildrenAsync_WithMissingRequestedAudience_ReturnsRequestedAudienceNotFound()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", null, 1, false));
        var service = testHarness.CreateService();

        var result = await service.ListChildrenAsync(new ListChildrenQuery(RootNodeId, new ReadContext(), MissingAudience));

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.RequestedAudienceNotFound, result.Code);
        Assert.Equal(MissingAudience.ToString(), result.Details[AudienceResolutionErrorCodes.RequestedAudienceIdDetail]);
    }

    [Fact]
    public async Task ListChildrenAsync_WithDeletedRequestedAudience_ReturnsRequestedAudienceDeleted()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", null, 1, false));
        testHarness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, true));
        var service = testHarness.CreateService();

        var result = await service.ListChildrenAsync(new ListChildrenQuery(RootNodeId, new ReadContext(), AudienceDeveloper));

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.RequestedAudienceDeleted, result.Code);
        Assert.Equal(AudienceDeveloper.ToString(), result.Details[AudienceResolutionErrorCodes.RequestedAudienceIdDetail]);
    }
}
