using System.Text.Json;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Server.Mcp.Mapping;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

[Trait("Category", "Unit")]
public sealed class McpHistoryMapperTests
{
    private static readonly SnapshotId BaseSnapshotId = new(41);
    private static readonly SnapshotId TargetSnapshotId = new(42);

    [Fact]
    public void SnapshotDiff_PreservesBindingCategoryOrder()
    {
        var role = new Role(TargetSnapshotId, new RoleId("Developer"), "Developer", null, false);
        var node = new Node(
            TargetSnapshotId,
            new NodeId(Guid.Parse("30000000-0000-0000-0000-000000000001")),
            null,
            "Node",
            null,
            0,
            false);
        var diff = new SnapshotDiff(
            BaseSnapshotId,
            TargetSnapshotId,
            [new NodeDiffEntry(DiffChangeKind.Added, null, node)],
            [new RoleDiffEntry(DiffChangeKind.Added, null, role)],
            [],
            [],
            []);

        var envelope = McpHistoryMapper.ToSnapshotDiffEnvelope(Result<SnapshotDiff>.Success(diff));

        Assert.Equal(["role", "node"], envelope.Data!.Items.Select(item => item.EntityType));
    }

    [Fact]
    public void DependencyEntry_IdentifiesTargetAndSource()
    {
        var targetNodeId = new NodeId(Guid.Parse("30000000-0000-0000-0000-000000000001"));
        var sourceNodeId = new NodeId(Guid.Parse("30000000-0000-0000-0000-000000000002"));
        var dependency = new ContentDependency(
            TargetSnapshotId,
            targetNodeId,
            new RoleId("EndUser"),
            sourceNodeId,
            new RoleId("Developer"),
            new ContentRevisionId(Guid.Parse("b4e0e04a-2dce-4b5e-8b34-4bd2b9f0e020")));
        var diff = new SnapshotDiff(
            BaseSnapshotId,
            TargetSnapshotId,
            [],
            [],
            [],
            [],
            [new DependencyDiffEntry(DiffChangeKind.Added, null, dependency)]);

        var json = JsonSerializer.Serialize(
            McpHistoryMapper.ToSnapshotDiffEnvelope(Result<SnapshotDiff>.Success(diff)));

        using var document = JsonDocument.Parse(json);
        var item = document.RootElement.GetProperty("data").GetProperty("items")[0];
        Assert.Equal(targetNodeId.ToString(), item.GetProperty("id").GetString());
        Assert.Equal("EndUser", item.GetProperty("roleId").GetString());
        Assert.Equal(sourceNodeId.ToString(), item.GetProperty("sourceNodeId").GetString());
        Assert.Equal("Developer", item.GetProperty("sourceRoleId").GetString());
    }
}
