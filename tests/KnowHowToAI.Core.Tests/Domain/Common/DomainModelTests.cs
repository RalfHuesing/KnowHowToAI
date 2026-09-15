using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Tests.Domain.Common;

[Trait("Category", "Unit")]
public sealed class DomainModelTests
{
    [Fact]
    public void VersionedModels_KeepSnapshotIdentityAndStateWithoutMutation()
    {
        var snapshotId = new SnapshotId(12);
        var nodeId = new NodeId(Guid.Parse("9f4a2c43-0a77-44be-8f98-f403444d3e9f"));
        var sourceNodeId = new NodeId(Guid.Parse("0a77f4a2-2c43-44be-8f98-f403444d3e9f"));
        var roleId = new RoleId("Developer");
        var revisionId = new ContentRevisionId(Guid.Parse("342c9f4a-0a77-44be-8f98-f403444d3e9f"));
        var createdAt = new DateTimeOffset(2026, 9, 15, 10, 30, 0, TimeSpan.Zero);

        var snapshot = new Snapshot(snapshotId, null, SnapshotState.Committed, createdAt, createdAt);
        var transaction = new KnowledgeTransaction(
            new TransactionId(Guid.Parse("542c9f4a-0a77-44be-8f98-f403444d3e9f")),
            snapshotId,
            new SnapshotId(13),
            TransactionState.Open,
            0,
            createdAt,
            null,
            "Dokumentation aktualisieren",
            "Agent",
            "MCP",
            null);
        var release = new Release(new ReleaseId(3), snapshotId, "v1.0", "Erste Freigabe", createdAt);
        var node = new Node(snapshotId, nodeId, null, "Installation", "Einrichtung", 0, false);
        var role = new Role(snapshotId, roleId, "Developer", "Technische Zielgruppe", false);
        var content = new NodeContent(snapshotId, nodeId, roleId, revisionId, ContentMode.Derived, "Installiere die Anwendung.", false);
        var dependency = new ContentDependency(snapshotId, nodeId, roleId, sourceNodeId, roleId, revisionId);

        Assert.Equal(snapshotId, snapshot.SnapshotId);
        Assert.Equal(TransactionState.Open, transaction.State);
        Assert.Equal(snapshotId, release.SnapshotId);
        Assert.Equal(nodeId, node.NodeId);
        Assert.Equal(roleId, role.RoleId);
        Assert.Equal(ContentMode.Derived, content.ContentMode);
        Assert.Equal(sourceNodeId, dependency.SourceNodeId);
    }
}
