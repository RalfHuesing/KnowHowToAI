using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Tests.Application.Navigation;

[Trait("Category", "Unit")]
public sealed class NavigationServiceTests
{
    private static readonly SnapshotId CurrentSnapshotId = new(10);
    private static readonly SnapshotId WorkingSnapshotId = new(11);
    private static readonly SnapshotId HistoricalSnapshotId = new(5);
    private static readonly TransactionId OpenTransactionId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly NodeId RootNodeId = new(Guid.Parse("b764fc68-d485-4bca-8617-b33a51d838ae"));
    private static readonly NodeId Child1NodeId = new(Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly NodeId Child2NodeId = new(Guid.Parse("20000000-0000-0000-0000-000000000002"));
    private static readonly NodeId Child3NodeId = new(Guid.Parse("30000000-0000-0000-0000-000000000003"));
    private static readonly NodeId SubChildNodeId = new(Guid.Parse("40000000-0000-0000-0000-000000000004"));
    private static readonly RoleId RoleDeveloper = new("Developer");
    private static readonly RoleId RoleConsultant = new("Consultant");

    // ── GetRootAsync Tests ──────────────────────────────────────────────────

    [Fact]
    public async Task GetRootAsync_EmptySnapshotWithoutRoot_ReturnsAvailabilityNoneAndNullNode()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        // Kein Root-Node im Harness
        var service = testHarness.CreateService();

        var result = await service.GetRootAsync(new ReadContext(), RoleDeveloper);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Node);
        Assert.Equal(Availability.None, result.Value.Availability);
        Assert.Equal(RoleDeveloper, result.Value.RequestedRoleId);
        Assert.Null(result.Value.Content);
    }

    [Fact]
    public async Task GetRootAsync_WithRootAndContent_ReturnsPopulatedNodeWithResolvedRole()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, RootNodeId, null, "Root Title", "Root Description", 0, false));
        testHarness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));
        testHarness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleDeveloper, RoleDeveloper, 1));
        testHarness.AddContent(new NodeContent(
            CurrentSnapshotId,
            RootNodeId,
            RoleDeveloper,
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            "Root Content in Markdown without Headings",
            false));

        var service = testHarness.CreateService();
        var result = await service.GetRootAsync(new ReadContext(), RoleDeveloper);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Node);
        Assert.Equal(RootNodeId, result.Value.Node!.NodeId);
        Assert.Equal("Root Title", result.Value.Node.Title);
        Assert.Equal("Root Description", result.Value.Node.Description);
        Assert.Equal(Availability.Explicit, result.Value.Availability);
        Assert.Equal(RoleDeveloper, result.Value.ResolvedRoleId);
        Assert.False(result.Value.FallbackUsed);
        Assert.NotNull(result.Value.Content);
        Assert.Equal(Freshness.Current, result.Value.Freshness);
    }

    // ── GetNodeAsync Tests ──────────────────────────────────────────────────

    [Fact]
    public async Task GetNodeAsync_ExistingNode_ReturnsNodeWithContent()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", "Purpose 1", 10, false));
        testHarness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));
        testHarness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleDeveloper, RoleDeveloper, 1));
        testHarness.AddContent(new NodeContent(
            CurrentSnapshotId,
            Child1NodeId,
            RoleDeveloper,
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            "Child content",
            false));

        var service = testHarness.CreateService();
        var result = await service.GetNodeAsync(Child1NodeId, new ReadContext(), RoleDeveloper);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Node);
        Assert.Equal(Child1NodeId, result.Value.Node!.NodeId);
        Assert.Equal("Child 1", result.Value.Node.Title);
        Assert.Equal(Availability.Explicit, result.Value.Availability);
    }

    [Fact]
    public async Task GetNodeAsync_NonExistentNode_ReturnsNodeNotFound()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        var missingNodeId = new NodeId(Guid.NewGuid());
        var service = testHarness.CreateService();

        var result = await service.GetNodeAsync(missingNodeId, new ReadContext(), RoleDeveloper);

        Assert.False(result.IsSuccess);
        Assert.Equal(NavigationErrorCodes.NodeNotFound, result.Code);
        Assert.Equal(missingNodeId.ToString(), result.Details[NavigationErrorCodes.NodeIdDetail]);
    }

    [Fact]
    public async Task GetNodeAsync_DeletedNode_WithoutIncludeDeleted_ReturnsNodeNotFound()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", "Purpose 1", 10, IsDeleted: true));
        var service = testHarness.CreateService();

        var result = await service.GetNodeAsync(Child1NodeId, new ReadContext(IncludeDeleted: false), RoleDeveloper);

        Assert.False(result.IsSuccess);
        Assert.Equal(NavigationErrorCodes.NodeNotFound, result.Code);
    }

    // ── ListRolesAsync Tests ────────────────────────────────────────────────

    [Fact]
    public async Task ListRolesAsync_ReturnsActiveRolesAndRespectsIncludeDeleted()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        var activeRole = new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false);
        var deletedRole = new Role(CurrentSnapshotId, RoleConsultant, "Consultant", null, true);
        testHarness.AddRole(activeRole);
        testHarness.AddRole(deletedRole);

        var service = testHarness.CreateService();

        var activeOnlyResult = await service.ListRolesAsync(new ReadContext());
        Assert.True(activeOnlyResult.IsSuccess);
        Assert.Single(activeOnlyResult.Value!);
        Assert.Equal(RoleDeveloper, activeOnlyResult.Value![0].RoleId);

        var includeDeletedResult = await service.ListRolesAsync(new ReadContext(IncludeDeleted: true));
        Assert.True(includeDeletedResult.IsSuccess);
        Assert.Equal(2, includeDeletedResult.Value!.Count);
    }

    // ── ListChildrenAsync Tests ─────────────────────────────────────────────

    [Fact]
    public async Task ListChildrenAsync_MetadataFirst_ReturnsSummariesWithoutFullContent()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, RootNodeId, null, "Root", null, 0, false));
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", "Desc 1", 1, false));
        testHarness.AddNode(new Node(CurrentSnapshotId, SubChildNodeId, Child1NodeId, "Subchild", "Desc Sub", 1, false));
        testHarness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));
        testHarness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleDeveloper, RoleDeveloper, 1));
        var content = "This is child 1 content in markdown.";
        testHarness.AddContent(new NodeContent(
            CurrentSnapshotId,
            Child1NodeId,
            RoleDeveloper,
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            content,
            false));

        var service = testHarness.CreateService();
        var result = await service.ListChildrenAsync(new ListChildrenQuery(RootNodeId, new ReadContext(), RoleDeveloper));

        Assert.True(result.IsSuccess);
        var page = result.Value!;
        Assert.Single(page.Items);

        var summary = page.Items[0];
        Assert.Equal(Child1NodeId, summary.NodeId);
        Assert.Equal("Child 1", summary.Title);
        Assert.Equal("Desc 1", summary.Description);
        Assert.Equal(1, summary.SortOrder);
        Assert.Equal(1, summary.ChildCount); // Hat Subchild
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(content), summary.ContentSizeBytes);
        Assert.Equal(Availability.Explicit, summary.Availability);
        Assert.Equal(RoleDeveloper, summary.ResolvedRoleId);
        Assert.Equal(Freshness.Current, summary.Freshness);
    }

    [Fact]
    public async Task ListChildrenAsync_DeterministicOrdering_SortsBySortOrderThenNodeId()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        var nodeB = new Node(CurrentSnapshotId, Child2NodeId, RootNodeId, "Node B", null, 20, false);
        var nodeA = new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Node A", null, 10, false);
        var nodeC = new Node(CurrentSnapshotId, Child3NodeId, RootNodeId, "Node C", null, 20, false);
        // Child2 (2000...) und Child3 (3000...) haben beide SortOrder 20; Child2NodeId.Value < Child3NodeId.Value
        testHarness.AddNode(nodeB);
        testHarness.AddNode(nodeA);
        testHarness.AddNode(nodeC);

        var service = testHarness.CreateService();
        var result = await service.ListChildrenAsync(new ListChildrenQuery(RootNodeId, new ReadContext(), RoleDeveloper));

        Assert.True(result.IsSuccess);
        var items = result.Value!.Items;
        Assert.Equal(3, items.Count);
        Assert.Equal(Child1NodeId, items[0].NodeId); // SortOrder 10
        Assert.Equal(Child2NodeId, items[1].NodeId); // SortOrder 20, Guid kleiner
        Assert.Equal(Child3NodeId, items[2].NodeId); // SortOrder 20, Guid größer
    }

    [Fact]
    public async Task ListChildrenAsync_CursorPaging_PagesThroughAllItemsWithoutGapsOrDuplicates()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", null, 1, false));
        testHarness.AddNode(new Node(CurrentSnapshotId, Child2NodeId, RootNodeId, "Child 2", null, 2, false));
        testHarness.AddNode(new Node(CurrentSnapshotId, Child3NodeId, RootNodeId, "Child 3", null, 3, false));

        var service = testHarness.CreateService();

        // Seite 1: Limit 2 -> liefert Child 1 und Child 2 + NextCursor
        var page1Result = await service.ListChildrenAsync(new ListChildrenQuery(
            RootNodeId,
            new ReadContext(),
            RoleDeveloper,
            Limit: 2));

        Assert.True(page1Result.IsSuccess);
        var page1 = page1Result.Value!;
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(Child1NodeId, page1.Items[0].NodeId);
        Assert.Equal(Child2NodeId, page1.Items[1].NodeId);
        Assert.NotNull(page1.NextCursor);

        // Seite 2: Fortsetzung mit NextCursor, Limit 2 -> liefert Child 3, NextCursor == null
        var page2Result = await service.ListChildrenAsync(new ListChildrenQuery(
            RootNodeId,
            new ReadContext(),
            RoleDeveloper,
            Limit: 2,
            Cursor: page1.NextCursor));

        Assert.True(page2Result.IsSuccess);
        var page2 = page2Result.Value!;
        Assert.Single(page2.Items);
        Assert.Equal(Child3NodeId, page2.Items[0].NodeId);
        Assert.Null(page2.NextCursor);
    }

    [Fact]
    public async Task ListChildrenAsync_InvalidCursor_MalformedString_ReturnsInvalidCursor()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", null, 1, false));

        var service = testHarness.CreateService();
        var invalidCursor = "NotAValidCursorString";

        var result = await service.ListChildrenAsync(new ListChildrenQuery(
            RootNodeId,
            new ReadContext(),
            RoleDeveloper,
            Cursor: invalidCursor));

        Assert.False(result.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, result.Code);
        Assert.Equal(invalidCursor, result.Details[NavigationErrorCodes.CursorDetail]);
    }

    [Fact]
    public async Task ListChildrenAsync_InvalidCursor_MismatchedParent_ReturnsInvalidCursor()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        var otherParentId = new NodeId(Guid.NewGuid());
        var cursor = new NavigationCursor(CurrentSnapshotId, null, otherParentId, RoleDeveloper, false, Child1NodeId, 1).Encode();

        var service = testHarness.CreateService();
        var result = await service.ListChildrenAsync(new ListChildrenQuery(
            RootNodeId, // Query ist für RootNodeId, Cursor war für otherParentId
            new ReadContext(),
            RoleDeveloper,
            Cursor: cursor));

        Assert.False(result.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, result.Code);
    }

    [Fact]
    public async Task ListChildrenAsync_InvalidCursor_MismatchedRole_ReturnsInvalidCursor()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        var cursor = new NavigationCursor(CurrentSnapshotId, null, RootNodeId, RoleConsultant, false, Child1NodeId, 1).Encode();

        var service = testHarness.CreateService();
        var result = await service.ListChildrenAsync(new ListChildrenQuery(
            RootNodeId,
            new ReadContext(),
            RoleDeveloper, // Query ist Developer, Cursor war Consultant
            Cursor: cursor));

        Assert.False(result.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, result.Code);
    }

    [Fact]
    public async Task ListChildrenAsync_InvalidCursor_NodeNotFoundInResultSet_ReturnsInvalidCursor()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", null, 1, false));

        var deletedOrGhostNodeId = new NodeId(Guid.NewGuid());
        var cursor = new NavigationCursor(CurrentSnapshotId, null, RootNodeId, RoleDeveloper, false, deletedOrGhostNodeId, 1).Encode();

        var service = testHarness.CreateService();
        var result = await service.ListChildrenAsync(new ListChildrenQuery(
            RootNodeId,
            new ReadContext(),
            RoleDeveloper,
            Cursor: cursor));

        Assert.False(result.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, result.Code);
    }

    // ── Cursor Expiration Tests ─────────────────────────────────────────────

    [Fact]
    public async Task ListChildrenAsync_WorkingRead_AfterMutation_RejectsCursorWithCursorExpired()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(WorkingSnapshotId, Child1NodeId, RootNodeId, "Child 1", null, 1, false));
        testHarness.AddNode(new Node(WorkingSnapshotId, Child2NodeId, RootNodeId, "Child 2", null, 2, false));

        // Transaktion bei ChangeVersion = 1
        var transaction = new KnowledgeTransaction(
            OpenTransactionId,
            CurrentSnapshotId,
            WorkingSnapshotId,
            TransactionState.Open,
            ChangeVersion: 1,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null);
        testHarness.SetTransaction(transaction);

        var service = testHarness.CreateService();

        // Seite 1 mit Limit 1 abrufen (bei ChangeVersion = 1)
        var page1Result = await service.ListChildrenAsync(new ListChildrenQuery(
            RootNodeId,
            new ReadContext(TransactionId: OpenTransactionId),
            RoleDeveloper,
            Limit: 1));

        Assert.True(page1Result.IsSuccess);
        var cursorV1 = page1Result.Value!.NextCursor;
        Assert.NotNull(cursorV1);

        // Jetzt erfolgt eine Mutation in der Transaktion -> ChangeVersion wird inkrementiert auf 2
        var mutatedTransaction = transaction with { ChangeVersion = 2 };
        testHarness.SetTransaction(mutatedTransaction);

        // Abruf von Seite 2 mit dem alten CursorV1 (ChangeVersion = 1)
        var page2Result = await service.ListChildrenAsync(new ListChildrenQuery(
            RootNodeId,
            new ReadContext(TransactionId: OpenTransactionId),
            RoleDeveloper,
            Limit: 1,
            Cursor: cursorV1));

        Assert.False(page2Result.IsSuccess);
        Assert.Equal(NavigationErrorCodes.CursorExpired, page2Result.Code);
        Assert.Equal(cursorV1, page2Result.Details[NavigationErrorCodes.CursorDetail]);
    }

    [Fact]
    public async Task ListChildrenAsync_CurrentRead_AfterCommit_RejectsCursorWithCursorExpired()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.AddNode(new Node(CurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", null, 1, false));
        testHarness.AddNode(new Node(CurrentSnapshotId, Child2NodeId, RootNodeId, "Child 2", null, 2, false));

        var service = testHarness.CreateService();

        // Seite 1 auf Snapshot 10 abrufen
        var page1Result = await service.ListChildrenAsync(new ListChildrenQuery(
            RootNodeId,
            new ReadContext(),
            RoleDeveloper,
            Limit: 1));

        Assert.True(page1Result.IsSuccess);
        var cursorSnapshot10 = page1Result.Value!.NextCursor;
        Assert.NotNull(cursorSnapshot10);

        // Jetzt committet jemand, so dass Current Snapshot 11 wird
        var newCurrentSnapshotId = new SnapshotId(11);
        testHarness.SetCurrentSnapshot(newCurrentSnapshotId);
        testHarness.AddNode(new Node(newCurrentSnapshotId, Child1NodeId, RootNodeId, "Child 1", null, 1, false));
        testHarness.AddNode(new Node(newCurrentSnapshotId, Child2NodeId, RootNodeId, "Child 2", null, 2, false));

        // Abruf mit ReadContext() (liest neuen Current Snapshot 11) aber altem Cursor von Snapshot 10
        var page2Result = await service.ListChildrenAsync(new ListChildrenQuery(
            RootNodeId,
            new ReadContext(),
            RoleDeveloper,
            Limit: 1,
            Cursor: cursorSnapshot10));

        Assert.False(page2Result.IsSuccess);
        Assert.Equal(NavigationErrorCodes.CursorExpired, page2Result.Code);
        Assert.Equal(cursorSnapshot10, page2Result.Details[NavigationErrorCodes.CursorDetail]);
    }

    [Fact]
    public async Task ListChildrenAsync_HistoricalRead_SnapshotDoesNotExpireAcrossCommits()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        // Historischer Snapshot 5 registrieren
        testHarness.AddHistoricalSnapshot(new Snapshot(
            HistoricalSnapshotId,
            null,
            SnapshotState.Committed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));
        testHarness.AddNode(new Node(HistoricalSnapshotId, Child1NodeId, RootNodeId, "Child 1", null, 1, false));
        testHarness.AddNode(new Node(HistoricalSnapshotId, Child2NodeId, RootNodeId, "Child 2", null, 2, false));

        var service = testHarness.CreateService();

        // Seite 1 auf historischem Snapshot 5 abrufen
        var page1Result = await service.ListChildrenAsync(new ListChildrenQuery(
            RootNodeId,
            new ReadContext(SnapshotId: HistoricalSnapshotId),
            RoleDeveloper,
            Limit: 1));

        Assert.True(page1Result.IsSuccess);
        var cursorSnapshot5 = page1Result.Value!.NextCursor;
        Assert.NotNull(cursorSnapshot5);

        // Current Snapshot rückt auf 12 weiter
        testHarness.SetCurrentSnapshot(new SnapshotId(12));

        // Abruf von Seite 2 auf Snapshot 5 mit cursorSnapshot5 bleibt stabil und erfolgreich!
        var page2Result = await service.ListChildrenAsync(new ListChildrenQuery(
            RootNodeId,
            new ReadContext(SnapshotId: HistoricalSnapshotId),
            RoleDeveloper,
            Limit: 1,
            Cursor: cursorSnapshot5));

        Assert.True(page2Result.IsSuccess);
        Assert.Single(page2Result.Value!.Items);
        Assert.Equal(Child2NodeId, page2Result.Value.Items[0].NodeId);
    }

}
