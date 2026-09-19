using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.History;

namespace KnowHowToAI.Web.Tests.Features.History;

[Trait("Category", "Unit")]
public sealed class HistoryMapperTests
{
    [Fact]
    public void ToSnapshotViewModel_MapsAllProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = new Snapshot(
            new SnapshotId(12),
            new SnapshotId(11),
            SnapshotState.Committed,
            now.AddHours(-1),
            now);

        var vm = HistoryMapper.ToSnapshotViewModel(snapshot);

        Assert.Equal(12L, vm.SnapshotId);
        Assert.Equal("Committed", vm.State);
        Assert.Equal(now.AddHours(-1), vm.CreatedAtUtc);
        Assert.Equal(11L, vm.BaseSnapshotId);
        Assert.Equal(now, vm.CommittedAtUtc);
    }

    [Fact]
    public void ToReleasePageViewModel_MapsReleasesAndPreservesCursor()
    {
        var releaseId = new ReleaseId(1L);
        var now = DateTimeOffset.UtcNow;
        var release = new Release(
            releaseId,
            new SnapshotId(15),
            "v1.0.0",
            "Erster stabiler Release",
            now);

        var page = new ReleasePage(new[] { release }, NextCursor: "rel-cursor-1");
        var vm = HistoryMapper.ToReleasePageViewModel(page);

        Assert.Equal("rel-cursor-1", vm.NextCursor);
        var item = Assert.Single(vm.Items);
        Assert.Equal(releaseId.Value, item.ReleaseId);
        Assert.Equal(15L, item.SnapshotId);
        Assert.Equal("v1.0.0", item.Name);
        Assert.Equal("Erster stabiler Release", item.Description);
        Assert.Equal(now, item.ReleasedAtUtc);
    }

    [Fact]
    public void ToSnapshotDiffViewModel_FlattensEntriesCorrectly()
    {
        var nodeId = new NodeId(Guid.NewGuid());
        var node = new Node(new SnapshotId(2), nodeId, null, "Neuer Node", null, 0, false);
        var nodeDiff = new NodeDiffEntry(DiffChangeKind.Added, Before: null, After: node);

        var roleId = new RoleId("admin");
        var role = new Role(new SnapshotId(2), roleId, "Admin", null, false);
        var roleDiff = new RoleDiffEntry(DiffChangeKind.Added, Before: null, After: role);

        var diff = new SnapshotDiff(
            new SnapshotId(1),
            new SnapshotId(2),
            new[] { nodeDiff },
            new[] { roleDiff },
            Array.Empty<RoleResolutionDiffEntry>(),
            Array.Empty<ContentDiffEntry>(),
            Array.Empty<DependencyDiffEntry>(),
            NextCursor: "diff-cursor-xyz",
            TotalCount: 2);

        var vm = HistoryMapper.ToSnapshotDiffViewModel(diff);

        Assert.Equal(1L, vm.BaseSnapshotId);
        Assert.Equal(2L, vm.TargetSnapshotId);
        Assert.Equal(2, vm.TotalCount);
        Assert.Equal("diff-cursor-xyz", vm.NextCursor);
        Assert.Equal(2, vm.Entries.Count);

        var rEntry = vm.Entries.First(e => e.EntityType == "Role");
        Assert.Equal("Added", rEntry.Kind);
        Assert.Equal(roleId.Value, rEntry.PrimaryId);

        var nEntry = vm.Entries.First(e => e.EntityType == "Node");
        Assert.Equal("Added", nEntry.Kind);
        Assert.Equal(nodeId.Value.ToString(), nEntry.PrimaryId);
        Assert.Equal("Neuer Node", nEntry.Detail);
    }

    [Fact]
    public void ResultMappers_PreserveErrorsAndWarnings()
    {
        var error = new DomainError("SnapshotNotFound", "Snapshot existiert nicht.");
        var warning = new DomainWarning("DiffWarning", "Diff ist groß.");

        var failed = Result<Snapshot>.Failure(error, new[] { warning });
        var result = HistoryMapper.ToSnapshotResult(failed);

        Assert.False(result.IsSuccess);
        Assert.Equal("SnapshotNotFound", result.Error!.Code);
        Assert.Single(result.Warnings);
    }
}
