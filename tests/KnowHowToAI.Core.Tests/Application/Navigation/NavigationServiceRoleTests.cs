using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Tests.Application.Navigation;

[Trait("Category", "Unit")]
public sealed class NavigationServiceRoleTests
{
    private static readonly SnapshotId CurrentSnapshotId = new(10);
    private static readonly NodeId RootNodeId = new(Guid.Parse("b764fc68-d485-4bca-8617-b33a51d838ae"));
    private static readonly NodeId Child1NodeId = new(Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly RoleId RoleDeveloper = new("Developer");
    private static readonly RoleId RoleArchived = new("Archived");
    private static readonly RoleId MissingRole = new("MissingRole");

    [Fact]
    public async Task GetRootAsync_EmptySnapshot_WithValidRole_ReturnsAvailabilityNoneAndNullNode()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        var service = testHarness.CreateService();

        var result = await service.GetRootAsync(new ReadContext(), RoleDeveloper);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Node);
        Assert.Equal(Availability.None, result.Value.Availability);
        Assert.Equal(RoleDeveloper, result.Value.RequestedRoleId);
        Assert.Null(result.Value.Content);
        Assert.Equal(Freshness.Unknown, result.Value.Freshness);
    }

    [Fact]
    public async Task GetRootAsync_EmptySnapshot_WithMissingRequestedRole_ReturnsRequestedRoleNotFound()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        var service = testHarness.CreateService();

        var result = await service.GetRootAsync(new ReadContext(), MissingRole);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleNotFound, result.Code);
        Assert.Equal(MissingRole.ToString(), result.Details[RoleResolutionErrorCodes.RequestedRoleIdDetail]);
    }

    [Fact]
    public async Task GetRootAsync_EmptySnapshot_WithDeletedRequestedRole_ReturnsRequestedRoleDeleted()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, true));
        var service = testHarness.CreateService();

        var result = await service.GetRootAsync(new ReadContext(), RoleDeveloper);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleDeleted, result.Code);
        Assert.Equal(RoleDeveloper.ToString(), result.Details[RoleResolutionErrorCodes.RequestedRoleIdDetail]);
    }

    [Fact]
    public async Task GetRootAsync_EmptySnapshot_WithDeletedCandidateRole_ReturnsCandidateRoleDeleted()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));
        testHarness.AddRole(new Role(CurrentSnapshotId, RoleArchived, "Archived", null, true));
        testHarness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleDeveloper, RoleArchived, 2));
        var service = testHarness.CreateService();

        var result = await service.GetRootAsync(new ReadContext(), RoleDeveloper);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.CandidateRoleDeleted, result.Code);
        Assert.Equal(RoleArchived.ToString(), result.Details[RoleResolutionErrorCodes.CandidateRoleIdDetail]);
    }

    [Fact]
    public async Task GetNodeAsync_ExistingNode_WithMissingRequestedRole_ReturnsRequestedRoleNotFound()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", "Purpose 1", 10, false));
        var service = testHarness.CreateService();

        var result = await service.GetNodeAsync(Child1NodeId, new ReadContext(), MissingRole);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleNotFound, result.Code);
        Assert.Equal(MissingRole.ToString(), result.Details[RoleResolutionErrorCodes.RequestedRoleIdDetail]);
    }

    [Fact]
    public async Task GetNodeAsync_ExistingNode_WithDeletedRequestedRole_ReturnsRequestedRoleDeleted()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", "Purpose 1", 10, false));
        testHarness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, true));
        var service = testHarness.CreateService();

        var result = await service.GetNodeAsync(Child1NodeId, new ReadContext(), RoleDeveloper);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleDeleted, result.Code);
        Assert.Equal(RoleDeveloper.ToString(), result.Details[RoleResolutionErrorCodes.RequestedRoleIdDetail]);
    }

    [Fact]
    public async Task ListChildrenAsync_WithMissingRequestedRole_ReturnsRequestedRoleNotFound()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", null, 1, false));
        var service = testHarness.CreateService();

        var result = await service.ListChildrenAsync(new ListChildrenQuery(RootNodeId, new ReadContext(), MissingRole));

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleNotFound, result.Code);
        Assert.Equal(MissingRole.ToString(), result.Details[RoleResolutionErrorCodes.RequestedRoleIdDetail]);
    }

    [Fact]
    public async Task ListChildrenAsync_WithDeletedRequestedRole_ReturnsRequestedRoleDeleted()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", null, 1, false));
        testHarness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, true));
        var service = testHarness.CreateService();

        var result = await service.ListChildrenAsync(new ListChildrenQuery(RootNodeId, new ReadContext(), RoleDeveloper));

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleDeleted, result.Code);
        Assert.Equal(RoleDeveloper.ToString(), result.Details[RoleResolutionErrorCodes.RequestedRoleIdDetail]);
    }
}
