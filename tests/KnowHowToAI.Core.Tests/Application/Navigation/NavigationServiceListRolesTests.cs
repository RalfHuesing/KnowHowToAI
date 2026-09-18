using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.Navigation;

[Trait("Category", "Unit")]
public sealed class NavigationServiceListRolesTests
{
    private static readonly SnapshotId CurrentSnapshotId = new(10);
    private static readonly SnapshotId WorkingSnapshotId = new(11);
    private static readonly SnapshotId HistoricalSnapshotId = new(5);
    private static readonly TransactionId OpenTransactionId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly RoleId RoleDeveloper = new("Developer");
    private static readonly RoleId RoleConsultant = new("Consultant");

    [Fact]
    public async Task ListRolesAsync_ReturnsActiveRolesAndRespectsIncludeDeleted()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        var activeRole = new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false);
        var deletedRole = new Role(CurrentSnapshotId, RoleConsultant, "Consultant", null, true);
        testHarness.AddRole(activeRole);
        testHarness.AddRole(deletedRole);

        var service = testHarness.CreateService();

        var activeOnlyResult = await service.ListRolesAsync(new ListRolesQuery(new ReadContext()));
        Assert.True(activeOnlyResult.IsSuccess);
        Assert.Single(activeOnlyResult.Value!.Items);
        Assert.Equal(RoleDeveloper, activeOnlyResult.Value.Items[0].RoleId);

        var includeDeletedResult = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(IncludeDeleted: true)));
        Assert.True(includeDeletedResult.IsSuccess);
        Assert.Equal(2, includeDeletedResult.Value!.Items.Count);
    }

    [Fact]
    public async Task ListRolesAsync_CurrentRead_ReturnsRolesSortedByRoleIdOrdinal()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.ClearRoles(CurrentSnapshotId);

        testHarness.AddRole(new Role(CurrentSnapshotId, new RoleId("Zulu"), "Zulu", null, false));
        testHarness.AddRole(new Role(CurrentSnapshotId, new RoleId("Alpha"), "Alpha", null, false));
        testHarness.AddRole(new Role(CurrentSnapshotId, new RoleId("alpha"), "alpha lowercase", null, false));
        testHarness.AddRole(new Role(CurrentSnapshotId, new RoleId("Beta"), "Beta", null, false));

        var service = testHarness.CreateService();
        var result = await service.ListRolesAsync(new ListRolesQuery());

        Assert.True(result.IsSuccess);
        var roleIds = result.Value!.Items.Select(r => r.RoleId.Value).ToArray();
        // Ordinal: 'A' (65), 'B' (66), 'Z' (90), 'a' (97)
        Assert.Equal(["Alpha", "Beta", "Zulu", "alpha"], roleIds);
    }

    [Fact]
    public async Task ListRolesAsync_HistoricalSnapshot_ReturnsRolesForSnapshot()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        var historicalSnapshot = new Snapshot(HistoricalSnapshotId, null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        testHarness.AddHistoricalSnapshot(historicalSnapshot);
        testHarness.ClearRoles(HistoricalSnapshotId);

        var histRole = new Role(HistoricalSnapshotId, new RoleId("ArchivedAuditor"), "Auditor", null, false);
        testHarness.AddRole(histRole);

        var service = testHarness.CreateService();
        var result = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(SnapshotId: HistoricalSnapshotId)));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(new RoleId("ArchivedAuditor"), result.Value.Items[0].RoleId);
    }

    [Fact]
    public async Task ListRolesAsync_WorkingTransaction_ReturnsWorkingRoles()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
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
        testHarness.ClearRoles(WorkingSnapshotId);

        var workingRole = new Role(WorkingSnapshotId, new RoleId("DraftRole"), "Draft Role", null, false);
        testHarness.AddRole(workingRole);

        var service = testHarness.CreateService();
        var result = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(TransactionId: OpenTransactionId)));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(new RoleId("DraftRole"), result.Value.Items[0].RoleId);
    }

    [Fact]
    public async Task ListRolesAsync_Pagination_ReturnsSeamlessSubsequentPages()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.ClearRoles(CurrentSnapshotId);

        var r1 = new Role(CurrentSnapshotId, new RoleId("R1"), "Role 1", null, false);
        var r2 = new Role(CurrentSnapshotId, new RoleId("R2"), "Role 2", null, false);
        var r3 = new Role(CurrentSnapshotId, new RoleId("R3"), "Role 3", null, false);
        var r4 = new Role(CurrentSnapshotId, new RoleId("R4"), "Role 4", null, false);
        var r5 = new Role(CurrentSnapshotId, new RoleId("R5"), "Role 5", null, false);
        testHarness.AddRole(r1);
        testHarness.AddRole(r2);
        testHarness.AddRole(r3);
        testHarness.AddRole(r4);
        testHarness.AddRole(r5);

        var service = testHarness.CreateService();

        // Seite 1: Limit 2
        var page1 = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(), Limit: 2));
        Assert.True(page1.IsSuccess);
        Assert.Equal(2, page1.Value!.Items.Count);
        Assert.Equal(["R1", "R2"], page1.Value.Items.Select(r => r.RoleId.Value));
        Assert.NotNull(page1.Value.NextCursor);

        // Seite 2: Limit 2 mit Cursor von Seite 1
        var page2 = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(), Limit: 2, Cursor: page1.Value.NextCursor));
        Assert.True(page2.IsSuccess);
        Assert.Equal(2, page2.Value!.Items.Count);
        Assert.Equal(["R3", "R4"], page2.Value.Items.Select(r => r.RoleId.Value));
        Assert.NotNull(page2.Value.NextCursor);

        // Seite 3: Limit 2 mit Cursor von Seite 2 (letzte Seite)
        var page3 = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(), Limit: 2, Cursor: page2.Value.NextCursor));
        Assert.True(page3.IsSuccess);
        Assert.Single(page3.Value!.Items);
        Assert.Equal("R5", page3.Value.Items[0].RoleId.Value);
        Assert.Null(page3.Value.NextCursor);
    }

    [Fact]
    public async Task ListRolesAsync_Limits_RespectsMaximumPageSizeAndDefaultsOnNonPositiveLimit()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.ClearRoles(CurrentSnapshotId);

        for (var i = 1; i <= 6; i++)
        {
            testHarness.AddRole(new Role(CurrentSnapshotId, new RoleId($"Role{i}"), $"Role {i}", null, false));
        }

        // defaultPageSize = 2, maximumPageSize = 4
        var service = testHarness.CreateService(defaultPageSize: 2, maximumPageSize: 4);

        // 1. Ohne Limit -> defaultPageSize (2)
        var defaultResult = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(), Limit: null));
        Assert.True(defaultResult.IsSuccess);
        Assert.Equal(2, defaultResult.Value!.Items.Count);

        // 2. Mit Limit <= 0 -> defaultPageSize (2)
        var zeroLimitResult = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(), Limit: 0));
        Assert.True(zeroLimitResult.IsSuccess);
        Assert.Equal(2, zeroLimitResult.Value!.Items.Count);

        var negativeLimitResult = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(), Limit: -10));
        Assert.True(negativeLimitResult.IsSuccess);
        Assert.Equal(2, negativeLimitResult.Value!.Items.Count);

        // 3. Mit Limit > maximumPageSize -> capped auf maximumPageSize (4)
        var largeLimitResult = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(), Limit: 100));
        Assert.True(largeLimitResult.IsSuccess);
        Assert.Equal(4, largeLimitResult.Value!.Items.Count);
    }

    [Fact]
    public async Task ListRolesAsync_InvalidCursor_RejectsManipulatedOrMismatchedCursor()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.ClearRoles(CurrentSnapshotId);
        testHarness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));

        var service = testHarness.CreateService();

        // 1. Korrupter / ungültiger String
        var corruptResult = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(), Cursor: "not-a-valid-base64url"));
        Assert.False(corruptResult.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, corruptResult.Code);

        // 2. Falscher Snapshot auf historischem Read
        var historicalSnapshot = new Snapshot(HistoricalSnapshotId, null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        testHarness.AddHistoricalSnapshot(historicalSnapshot);
        var wrongSnapshotCursor = new RoleCursor(CurrentSnapshotId, null, false, RoleDeveloper).Encode();
        var wrongSnapshotResult = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(SnapshotId: HistoricalSnapshotId), Cursor: wrongSnapshotCursor));
        Assert.False(wrongSnapshotResult.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, wrongSnapshotResult.Code);

        // 3. Abweichendes IncludeDeleted
        var diffIncludeDeletedCursor = new RoleCursor(CurrentSnapshotId, null, IncludeDeleted: true, RoleDeveloper).Encode();
        var diffIncludeDeletedResult = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(IncludeDeleted: false), Cursor: diffIncludeDeletedCursor));
        Assert.False(diffIncludeDeletedResult.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, diffIncludeDeletedResult.Code);

        // 4. RoleId nicht im ResultSet vorhanden
        var ghostRoleCursor = new RoleCursor(CurrentSnapshotId, null, false, new RoleId("NonExistentRole")).Encode();
        var ghostResult = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(), Cursor: ghostRoleCursor));
        Assert.False(ghostResult.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, ghostResult.Code);
    }

    [Fact]
    public async Task ListRolesAsync_CurrentRead_AfterCommit_RejectsCursorWithCursorExpired()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.ClearRoles(CurrentSnapshotId);
        testHarness.AddRole(new Role(CurrentSnapshotId, new RoleId("R1"), "Role 1", null, false));
        testHarness.AddRole(new Role(CurrentSnapshotId, new RoleId("R2"), "Role 2", null, false));

        var service = testHarness.CreateService();

        // Seite 1 mit Limit 1 abrufen
        var page1 = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(), Limit: 1));
        Assert.True(page1.IsSuccess);
        Assert.NotNull(page1.Value!.NextCursor);

        // Snapshot weiterdrehen (Commit)
        var nextSnapshotId = new SnapshotId(12);
        testHarness.SetCurrentSnapshot(nextSnapshotId);
        testHarness.ClearRoles(nextSnapshotId);
        testHarness.AddRole(new Role(nextSnapshotId, new RoleId("R1"), "Role 1", null, false));
        testHarness.AddRole(new Role(nextSnapshotId, new RoleId("R2"), "Role 2", null, false));

        // Seite 2 mit altem Cursor abfragen
        var page2 = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(), Limit: 1, Cursor: page1.Value.NextCursor));
        Assert.False(page2.IsSuccess);
        Assert.Equal(NavigationErrorCodes.CursorExpired, page2.Code);
    }

    [Fact]
    public async Task ListRolesAsync_WorkingRead_AfterMutation_RejectsCursorWithCursorExpired()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.ClearRoles(WorkingSnapshotId);
        testHarness.AddRole(new Role(WorkingSnapshotId, new RoleId("R1"), "Role 1", null, false));
        testHarness.AddRole(new Role(WorkingSnapshotId, new RoleId("R2"), "Role 2", null, false));

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

        // Seite 1 mit Limit 1 abrufen
        var page1 = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(TransactionId: OpenTransactionId), Limit: 1));
        Assert.True(page1.IsSuccess);
        Assert.NotNull(page1.Value!.NextCursor);

        // Mutation: ChangeVersion erhöht sich auf 2
        var updatedTransaction = transaction with { ChangeVersion = 2 };
        testHarness.SetTransaction(updatedTransaction);

        // Seite 2 mit altem Cursor abrufen
        var page2 = await service.ListRolesAsync(new ListRolesQuery(new ReadContext(TransactionId: OpenTransactionId), Limit: 1, Cursor: page1.Value.NextCursor));
        Assert.False(page2.IsSuccess);
        Assert.Equal(NavigationErrorCodes.CursorExpired, page2.Code);
    }
}
