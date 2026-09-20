using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Tests.Application.History;

[Trait("Category", "Unit")]
public sealed class SnapshotDiffCalculatorTests
{
    private static readonly SnapshotId BaseSnap = new(10);
    private static readonly SnapshotId TargetSnap = new(11);
    private static readonly AudienceId AudienceDev = new("Developer");
    private static readonly AudienceId AudienceConsultant = new("Consultant");

    [Fact]
    public void Compute_IdenticalSnapshots_ReturnsEmptyDiff()
    {
        var node = new Node(BaseSnap, new NodeId(Guid.NewGuid()), null, "Title", "Desc", 0, false);
        var audience = new Audience(BaseSnap, AudienceDev, "Developer", "Desc", false);
        var res = new AudienceResolution(BaseSnap, AudienceDev, AudienceDev, 1);
        var content = new NodeContent(BaseSnap, node.NodeId, AudienceDev, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Md", false);
        var dep = new ContentDependency(BaseSnap, node.NodeId, AudienceDev, node.NodeId, AudienceDev, content.ContentRevisionId);

        var baseData = new SnapshotData([node], [audience], [res], [content], [dep]);
        var targetData = new SnapshotData([node], [audience], [res], [content], [dep]);

        var request = new SnapshotDiffCalculationRequest(BaseSnap, TargetSnap, baseData, targetData, 50, 0);
        var diff = SnapshotDiffCalculator.Compute(request);

        Assert.Equal(0, diff.TotalCount);
        Assert.Empty(diff.Nodes);
        Assert.Empty(diff.Audiences);
        Assert.Empty(diff.AudienceResolutions);
        Assert.Empty(diff.Contents);
        Assert.Empty(diff.Dependencies);
        Assert.Null(diff.NextCursor);
    }

    [Fact]
    public void Compute_NodesChanges_ClassifiesAddedModifiedDeletedAndOmitted()
    {
        var addedNodeId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000001"));
        var modifiedNodeId = new NodeId(Guid.Parse("20000000-0000-0000-0000-000000000002"));
        var deletedNodeId = new NodeId(Guid.Parse("30000000-0000-0000-0000-000000000003"));
        var unchangedNodeId = new NodeId(Guid.Parse("40000000-0000-0000-0000-000000000004"));

        var baseNodes = new List<Node>
        {
            new(BaseSnap, modifiedNodeId, null, "Old Title", "Desc", 1, false),
            new(BaseSnap, deletedNodeId, null, "To Delete", "Desc", 2, false),
            new(BaseSnap, unchangedNodeId, null, "Unchanged", "Desc", 3, false)
        };

        var targetNodes = new List<Node>
        {
            new(TargetSnap, addedNodeId, null, "New Node", "Desc", 0, false),
            new(TargetSnap, modifiedNodeId, null, "New Title", "Desc", 1, false),
            new(TargetSnap, deletedNodeId, null, "To Delete", "Desc", 2, true), // soft deleted
            new(TargetSnap, unchangedNodeId, null, "Unchanged", "Desc", 3, false)
        };

        var baseData = new SnapshotData(baseNodes, [], [], [], []);
        var targetData = new SnapshotData(targetNodes, [], [], [], []);

        var request = new SnapshotDiffCalculationRequest(BaseSnap, TargetSnap, baseData, targetData, 50, 0);
        var diff = SnapshotDiffCalculator.Compute(request);

        Assert.Equal(3, diff.TotalCount);
        Assert.Equal(3, diff.Nodes.Count);

        var added = Assert.Single(diff.Nodes, n => n.Kind == DiffChangeKind.Added);
        Assert.Equal(addedNodeId, added.After!.NodeId);
        Assert.Null(added.Before);

        var modified = Assert.Single(diff.Nodes, n => n.Kind == DiffChangeKind.Modified);
        Assert.Equal(modifiedNodeId, modified.After!.NodeId);
        Assert.Equal("Old Title", modified.Before!.Title);
        Assert.Equal("New Title", modified.After!.Title);

        var deleted = Assert.Single(diff.Nodes, n => n.Kind == DiffChangeKind.Deleted);
        Assert.Equal(deletedNodeId, deleted.Before!.NodeId);
        Assert.Null(deleted.After);
    }

    [Fact]
    public void Compute_AudiencesAndResolutionsChanges_ClassifiesCorrectly()
    {
        var audience1 = new Audience(BaseSnap, AudienceDev, "Developer", "Old", false);
        var audience1Updated = new Audience(TargetSnap, AudienceDev, "Developer Pro", "New", false);
        var audience2 = new Audience(TargetSnap, AudienceConsultant, "Consultant", "Desc", false);

        var res1 = new AudienceResolution(BaseSnap, AudienceDev, AudienceDev, 1);
        var res1Updated = new AudienceResolution(TargetSnap, AudienceDev, AudienceDev, 2);
        var res2 = new AudienceResolution(TargetSnap, AudienceDev, AudienceConsultant, 1);

        var baseData = new SnapshotData([], [audience1], [res1], [], []);
        var targetData = new SnapshotData([], [audience1Updated, audience2], [res1Updated, res2], [], []);

        var request = new SnapshotDiffCalculationRequest(BaseSnap, TargetSnap, baseData, targetData, 50, 0);
        var diff = SnapshotDiffCalculator.Compute(request);

        Assert.Equal(2, diff.Audiences.Count);
        Assert.Contains(diff.Audiences, r => r.Kind == DiffChangeKind.Modified && r.After!.AudienceId == AudienceDev);
        Assert.Contains(diff.Audiences, r => r.Kind == DiffChangeKind.Added && r.After!.AudienceId == AudienceConsultant);

        Assert.Equal(2, diff.AudienceResolutions.Count);
        Assert.Contains(diff.AudienceResolutions, r => r.Kind == DiffChangeKind.Modified && r.After!.CandidateAudienceId == AudienceDev);
        Assert.Contains(diff.AudienceResolutions, r => r.Kind == DiffChangeKind.Added && r.After!.CandidateAudienceId == AudienceConsultant);
    }

    [Fact]
    public void Compute_ContentsAndDependenciesChanges_ClassifiesCorrectly()
    {
        var nodeId = new NodeId(Guid.NewGuid());
        var rev1 = new ContentRevisionId(Guid.NewGuid());
        var rev2 = new ContentRevisionId(Guid.NewGuid());

        var contentBase = new NodeContent(BaseSnap, nodeId, AudienceDev, rev1, ContentMode.Independent, "Old Content", false);
        var contentTarget = new NodeContent(TargetSnap, nodeId, AudienceDev, rev2, ContentMode.Independent, "New Content", false);

        var depBase = new ContentDependency(BaseSnap, nodeId, AudienceDev, nodeId, AudienceDev, rev1);
        var depTarget = new ContentDependency(TargetSnap, nodeId, AudienceDev, nodeId, AudienceDev, rev2);

        var baseData = new SnapshotData([], [], [], [contentBase], [depBase]);
        var targetData = new SnapshotData([], [], [], [contentTarget], [depTarget]);

        var request = new SnapshotDiffCalculationRequest(BaseSnap, TargetSnap, baseData, targetData, 50, 0);
        var diff = SnapshotDiffCalculator.Compute(request);

        var contentDiff = Assert.Single(diff.Contents);
        Assert.Equal(DiffChangeKind.Modified, contentDiff.Kind);
        Assert.Equal("Old Content", contentDiff.Before!.ContentMd);
        Assert.Equal("New Content", contentDiff.After!.ContentMd);

        var depDiff = Assert.Single(diff.Dependencies);
        Assert.Equal(DiffChangeKind.Modified, depDiff.Kind);
        Assert.Equal(rev1, depDiff.Before!.SourceContentRevisionId);
        Assert.Equal(rev2, depDiff.After!.SourceContentRevisionId);
    }

    [Fact]
    public void Compute_NodeFilter_OnlyIncludesMatchingNodeContentAndDependenciesAndBindsTheCursor()
    {
        var selectedNodeId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000001"));
        var otherNodeId = new NodeId(Guid.Parse("20000000-0000-0000-0000-000000000002"));
        var selectedRevision = new ContentRevisionId(Guid.Parse("30000000-0000-0000-0000-000000000003"));
        var otherRevision = new ContentRevisionId(Guid.Parse("40000000-0000-0000-0000-000000000004"));

        var targetData = new SnapshotData(
            [
                new Node(TargetSnap, selectedNodeId, null, "Ausgewählt", null, 1, false),
                new Node(TargetSnap, otherNodeId, null, "Anderer", null, 2, false)
            ],
            [new Audience(TargetSnap, AudienceDev, "Developer", null, false)],
            [],
            [
                new NodeContent(TargetSnap, selectedNodeId, AudienceDev, selectedRevision, ContentMode.Independent, "Ausgewählt", false),
                new NodeContent(TargetSnap, otherNodeId, AudienceDev, otherRevision, ContentMode.Independent, "Anderer", false)
            ],
            [
                new ContentDependency(TargetSnap, selectedNodeId, AudienceDev, otherNodeId, AudienceDev, otherRevision),
                new ContentDependency(TargetSnap, otherNodeId, AudienceDev, otherNodeId, AudienceDev, otherRevision)
            ]);

        var diff = SnapshotDiffCalculator.Compute(new SnapshotDiffCalculationRequest(
            BaseSnap,
            TargetSnap,
            new SnapshotData([], [], [], [], []),
            targetData,
            Limit: 2,
            Offset: 0,
            FilterNodeId: selectedNodeId));

        Assert.Equal(3, diff.TotalCount);
        Assert.Single(diff.Nodes);
        Assert.Single(diff.Contents);
        Assert.Empty(diff.Audiences);
        Assert.Empty(diff.AudienceResolutions);
        Assert.Empty(diff.Dependencies);

        var cursor = Assert.IsType<DiffCursor>(DiffCursor.TryDecode(diff.NextCursor));
        Assert.Equal(selectedNodeId, cursor.FilterNodeId);
    }

    [Fact]
    public void Compute_CategorySlicingAcrossMultiplePages_PaginatesDeterministically()
    {
        // 2 Audiences, 3 Nodes, 2 Contents -> Total 7 items
        var audience1 = new Audience(TargetSnap, new AudienceId("AudienceA"), "A", null, false);
        var audience2 = new Audience(TargetSnap, new AudienceId("AudienceB"), "B", null, false);

        var node1 = new Node(TargetSnap, new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000001")), null, "N1", null, 1, false);
        var node2 = new Node(TargetSnap, new NodeId(Guid.Parse("20000000-0000-0000-0000-000000000002")), null, "N2", null, 2, false);
        var node3 = new Node(TargetSnap, new NodeId(Guid.Parse("30000000-0000-0000-0000-000000000003")), null, "N3", null, 3, false);

        var content1 = new NodeContent(TargetSnap, node1.NodeId, AudienceDev, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "C1", false);
        var content2 = new NodeContent(TargetSnap, node2.NodeId, AudienceDev, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "C2", false);

        var baseData = new SnapshotData([], [], [], [], []);
        var targetData = new SnapshotData([node1, node2, node3], [audience1, audience2], [], [content1, content2], []);

        // Page 1: limit 3 -> 2 audiences + 1 node
        var p1Req = new SnapshotDiffCalculationRequest(BaseSnap, TargetSnap, baseData, targetData, 3, 0);
        var page1 = SnapshotDiffCalculator.Compute(p1Req);

        Assert.Equal(7, page1.TotalCount);
        Assert.Equal(2, page1.Audiences.Count);
        Assert.Single(page1.Nodes);
        Assert.Empty(page1.Contents);
        Assert.NotNull(page1.NextCursor);

        var cursor1 = DiffCursor.TryDecode(page1.NextCursor);
        Assert.NotNull(cursor1);
        Assert.Equal(3, cursor1.NextOffset);

        // Page 2: limit 3 -> 2 nodes + 1 content
        var p2Req = new SnapshotDiffCalculationRequest(BaseSnap, TargetSnap, baseData, targetData, 3, cursor1.NextOffset);
        var page2 = SnapshotDiffCalculator.Compute(p2Req);

        Assert.Equal(7, page2.TotalCount);
        Assert.Empty(page2.Audiences);
        Assert.Equal(2, page2.Nodes.Count);
        Assert.Single(page2.Contents);
        Assert.NotNull(page2.NextCursor);

        var cursor2 = DiffCursor.TryDecode(page2.NextCursor);
        Assert.NotNull(cursor2);
        Assert.Equal(6, cursor2.NextOffset);

        // Page 3: limit 3 -> remaining 1 content
        var p3Req = new SnapshotDiffCalculationRequest(BaseSnap, TargetSnap, baseData, targetData, 3, cursor2.NextOffset);
        var page3 = SnapshotDiffCalculator.Compute(p3Req);

        Assert.Equal(7, page3.TotalCount);
        Assert.Empty(page3.Audiences);
        Assert.Empty(page3.Nodes);
        Assert.Single(page3.Contents);
        Assert.Null(page3.NextCursor); // Done!
    }

    [Fact]
    public void Compute_NodeMove_ClassifiesAsModifiedWithParentNodeIdChange()
    {
        var oldParentId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000001"));
        var newParentId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000002"));
        var movedNodeId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000003"));

        var baseNodes = new List<Node>
        {
            new(BaseSnap, oldParentId, null, "Old Parent", null, 1, false),
            new(BaseSnap, newParentId, null, "New Parent", null, 2, false),
            new(BaseSnap, movedNodeId, oldParentId, "Moved Child", null, 1, false)
        };

        var targetNodes = new List<Node>
        {
            new(TargetSnap, oldParentId, null, "Old Parent", null, 1, false),
            new(TargetSnap, newParentId, null, "New Parent", null, 2, false),
            new(TargetSnap, movedNodeId, newParentId, "Moved Child", null, 1, false)
        };

        var baseData = new SnapshotData(baseNodes, [], [], [], []);
        var targetData = new SnapshotData(targetNodes, [], [], [], []);

        var request = new SnapshotDiffCalculationRequest(BaseSnap, TargetSnap, baseData, targetData, 50, 0);
        var diff = SnapshotDiffCalculator.Compute(request);

        var moveChange = Assert.Single(diff.Nodes);
        Assert.Equal(DiffChangeKind.Modified, moveChange.Kind);
        Assert.Equal(movedNodeId, moveChange.After!.NodeId);
        Assert.Equal(oldParentId, moveChange.Before!.ParentNodeId);
        Assert.Equal(newParentId, moveChange.After!.ParentNodeId);
    }
}
