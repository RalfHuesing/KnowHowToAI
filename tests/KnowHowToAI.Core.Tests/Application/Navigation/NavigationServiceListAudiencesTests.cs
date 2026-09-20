using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.Navigation;

[Trait("Category", "Unit")]
public sealed class NavigationServiceListAudiencesTests
{
    private static readonly SnapshotId CurrentSnapshotId = new(10);
    private static readonly SnapshotId WorkingSnapshotId = new(11);
    private static readonly SnapshotId HistoricalSnapshotId = new(5);
    private static readonly TransactionId OpenTransactionId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AudienceId AudienceDeveloper = new("Developer");
    private static readonly AudienceId AudienceConsultant = new("Consultant");

    [Fact]
    public async Task ListAudiencesAsync_ReturnsActiveAudiencesAndRespectsIncludeDeleted()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        var activeAudience = new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, false);
        var deletedAudience = new Audience(CurrentSnapshotId, AudienceConsultant, "Consultant", null, true);
        testHarness.AddAudience(activeAudience);
        testHarness.AddAudience(deletedAudience);

        var service = testHarness.CreateService();

        var activeOnlyResult = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext()));
        Assert.True(activeOnlyResult.IsSuccess);
        Assert.Single(activeOnlyResult.Value!.Items);
        Assert.Equal(AudienceDeveloper, activeOnlyResult.Value.Items[0].AudienceId);

        var includeDeletedResult = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(IncludeDeleted: true)));
        Assert.True(includeDeletedResult.IsSuccess);
        Assert.Equal(2, includeDeletedResult.Value!.Items.Count);
    }

    [Fact]
    public async Task ListAudiencesAsync_CurrentRead_ReturnsAudiencesSortedByAudienceIdOrdinal()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.ClearAudiences(CurrentSnapshotId);

        testHarness.AddAudience(new Audience(CurrentSnapshotId, new AudienceId("Zulu"), "Zulu", null, false));
        testHarness.AddAudience(new Audience(CurrentSnapshotId, new AudienceId("Alpha"), "Alpha", null, false));
        testHarness.AddAudience(new Audience(CurrentSnapshotId, new AudienceId("alpha"), "alpha lowercase", null, false));
        testHarness.AddAudience(new Audience(CurrentSnapshotId, new AudienceId("Beta"), "Beta", null, false));

        var service = testHarness.CreateService();
        var result = await service.ListAudiencesAsync(new ListAudiencesQuery());

        Assert.True(result.IsSuccess);
        var audienceIds = result.Value!.Items.Select(r => r.AudienceId.Value).ToArray();
        // Ordinal: 'A' (65), 'B' (66), 'Z' (90), 'a' (97)
        Assert.Equal(["Alpha", "Beta", "Zulu", "alpha"], audienceIds);
    }

    [Fact]
    public async Task ListAudiencesAsync_HistoricalSnapshot_ReturnsAudiencesForSnapshot()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        var historicalSnapshot = new Snapshot(HistoricalSnapshotId, null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        testHarness.AddHistoricalSnapshot(historicalSnapshot);
        testHarness.ClearAudiences(HistoricalSnapshotId);

        var histAudience = new Audience(HistoricalSnapshotId, new AudienceId("ArchivedAuditor"), "Auditor", null, false);
        testHarness.AddAudience(histAudience);

        var service = testHarness.CreateService();
        var result = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(SnapshotId: HistoricalSnapshotId)));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(new AudienceId("ArchivedAuditor"), result.Value.Items[0].AudienceId);
    }

    [Fact]
    public async Task ListAudiencesAsync_WorkingTransaction_ReturnsWorkingAudiences()
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
        testHarness.ClearAudiences(WorkingSnapshotId);

        var workingAudience = new Audience(WorkingSnapshotId, new AudienceId("DraftAudience"), "Draft Audience", null, false);
        testHarness.AddAudience(workingAudience);

        var service = testHarness.CreateService();
        var result = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(TransactionId: OpenTransactionId)));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(new AudienceId("DraftAudience"), result.Value.Items[0].AudienceId);
    }

    [Fact]
    public async Task ListAudiencesAsync_Pagination_ReturnsSeamlessSubsequentPages()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.ClearAudiences(CurrentSnapshotId);

        var r1 = new Audience(CurrentSnapshotId, new AudienceId("R1"), "Audience 1", null, false);
        var r2 = new Audience(CurrentSnapshotId, new AudienceId("R2"), "Audience 2", null, false);
        var r3 = new Audience(CurrentSnapshotId, new AudienceId("R3"), "Audience 3", null, false);
        var r4 = new Audience(CurrentSnapshotId, new AudienceId("R4"), "Audience 4", null, false);
        var r5 = new Audience(CurrentSnapshotId, new AudienceId("R5"), "Audience 5", null, false);
        testHarness.AddAudience(r1);
        testHarness.AddAudience(r2);
        testHarness.AddAudience(r3);
        testHarness.AddAudience(r4);
        testHarness.AddAudience(r5);

        var service = testHarness.CreateService();

        // Seite 1: Limit 2
        var page1 = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(), Limit: 2));
        Assert.True(page1.IsSuccess);
        Assert.Equal(2, page1.Value!.Items.Count);
        Assert.Equal(["R1", "R2"], page1.Value.Items.Select(r => r.AudienceId.Value));
        Assert.NotNull(page1.Value.NextCursor);

        // Seite 2: Limit 2 mit Cursor von Seite 1
        var page2 = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(), Limit: 2, Cursor: page1.Value.NextCursor));
        Assert.True(page2.IsSuccess);
        Assert.Equal(2, page2.Value!.Items.Count);
        Assert.Equal(["R3", "R4"], page2.Value.Items.Select(r => r.AudienceId.Value));
        Assert.NotNull(page2.Value.NextCursor);

        // Seite 3: Limit 2 mit Cursor von Seite 2 (letzte Seite)
        var page3 = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(), Limit: 2, Cursor: page2.Value.NextCursor));
        Assert.True(page3.IsSuccess);
        Assert.Single(page3.Value!.Items);
        Assert.Equal("R5", page3.Value.Items[0].AudienceId.Value);
        Assert.Null(page3.Value.NextCursor);
    }

    [Fact]
    public async Task ListAudiencesAsync_Limits_RespectsMaximumPageSizeAndDefaultsOnNonPositiveLimit()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.ClearAudiences(CurrentSnapshotId);

        for (var i = 1; i <= 6; i++)
        {
            testHarness.AddAudience(new Audience(CurrentSnapshotId, new AudienceId($"Audience{i}"), $"Audience {i}", null, false));
        }

        // defaultPageSize = 2, maximumPageSize = 4
        var service = testHarness.CreateService(defaultPageSize: 2, maximumPageSize: 4);

        // 1. Ohne Limit -> defaultPageSize (2)
        var defaultResult = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(), Limit: null));
        Assert.True(defaultResult.IsSuccess);
        Assert.Equal(2, defaultResult.Value!.Items.Count);

        // 2. Mit Limit <= 0 -> defaultPageSize (2)
        var zeroLimitResult = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(), Limit: 0));
        Assert.True(zeroLimitResult.IsSuccess);
        Assert.Equal(2, zeroLimitResult.Value!.Items.Count);

        var negativeLimitResult = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(), Limit: -10));
        Assert.True(negativeLimitResult.IsSuccess);
        Assert.Equal(2, negativeLimitResult.Value!.Items.Count);

        // 3. Mit Limit > maximumPageSize -> capped auf maximumPageSize (4)
        var largeLimitResult = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(), Limit: 100));
        Assert.True(largeLimitResult.IsSuccess);
        Assert.Equal(4, largeLimitResult.Value!.Items.Count);
    }

    [Fact]
    public async Task ListAudiencesAsync_InvalidCursor_RejectsManipulatedOrMismatchedCursor()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.ClearAudiences(CurrentSnapshotId);
        testHarness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, false));

        var service = testHarness.CreateService();

        // 1. Korrupter / ungültiger String
        var corruptResult = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(), Cursor: "not-a-valid-base64url"));
        Assert.False(corruptResult.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, corruptResult.Code);

        // 2. Falscher Snapshot auf historischem Read
        var historicalSnapshot = new Snapshot(HistoricalSnapshotId, null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        testHarness.AddHistoricalSnapshot(historicalSnapshot);
        var wrongSnapshotCursor = new AudienceCursor(CurrentSnapshotId, null, false, AudienceDeveloper).Encode();
        var wrongSnapshotResult = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(SnapshotId: HistoricalSnapshotId), Cursor: wrongSnapshotCursor));
        Assert.False(wrongSnapshotResult.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, wrongSnapshotResult.Code);

        // 3. Abweichendes IncludeDeleted
        var diffIncludeDeletedCursor = new AudienceCursor(CurrentSnapshotId, null, IncludeDeleted: true, AudienceDeveloper).Encode();
        var diffIncludeDeletedResult = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(IncludeDeleted: false), Cursor: diffIncludeDeletedCursor));
        Assert.False(diffIncludeDeletedResult.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, diffIncludeDeletedResult.Code);

        // 4. AudienceId nicht im ResultSet vorhanden
        var ghostAudienceCursor = new AudienceCursor(CurrentSnapshotId, null, false, new AudienceId("NonExistentAudience")).Encode();
        var ghostResult = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(), Cursor: ghostAudienceCursor));
        Assert.False(ghostResult.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, ghostResult.Code);
    }

    [Fact]
    public async Task ListAudiencesAsync_CurrentRead_AfterCommit_RejectsCursorWithCursorExpired()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.ClearAudiences(CurrentSnapshotId);
        testHarness.AddAudience(new Audience(CurrentSnapshotId, new AudienceId("R1"), "Audience 1", null, false));
        testHarness.AddAudience(new Audience(CurrentSnapshotId, new AudienceId("R2"), "Audience 2", null, false));

        var service = testHarness.CreateService();

        // Seite 1 mit Limit 1 abrufen
        var page1 = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(), Limit: 1));
        Assert.True(page1.IsSuccess);
        Assert.NotNull(page1.Value!.NextCursor);

        // Snapshot weiterdrehen (Commit)
        var nextSnapshotId = new SnapshotId(12);
        testHarness.SetCurrentSnapshot(nextSnapshotId);
        testHarness.ClearAudiences(nextSnapshotId);
        testHarness.AddAudience(new Audience(nextSnapshotId, new AudienceId("R1"), "Audience 1", null, false));
        testHarness.AddAudience(new Audience(nextSnapshotId, new AudienceId("R2"), "Audience 2", null, false));

        // Seite 2 mit altem Cursor abfragen
        var page2 = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(), Limit: 1, Cursor: page1.Value.NextCursor));
        Assert.False(page2.IsSuccess);
        Assert.Equal(NavigationErrorCodes.CursorExpired, page2.Code);
    }

    [Fact]
    public async Task ListAudiencesAsync_WorkingRead_AfterMutation_RejectsCursorWithCursorExpired()
    {
        var testHarness = new NavigationTestHarness(CurrentSnapshotId);
        testHarness.ClearAudiences(WorkingSnapshotId);
        testHarness.AddAudience(new Audience(WorkingSnapshotId, new AudienceId("R1"), "Audience 1", null, false));
        testHarness.AddAudience(new Audience(WorkingSnapshotId, new AudienceId("R2"), "Audience 2", null, false));

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
        var page1 = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(TransactionId: OpenTransactionId), Limit: 1));
        Assert.True(page1.IsSuccess);
        Assert.NotNull(page1.Value!.NextCursor);

        // Mutation: ChangeVersion erhöht sich auf 2
        var updatedTransaction = transaction with { ChangeVersion = 2 };
        testHarness.SetTransaction(updatedTransaction);

        // Seite 2 mit altem Cursor abrufen
        var page2 = await service.ListAudiencesAsync(new ListAudiencesQuery(new ReadContext(TransactionId: OpenTransactionId), Limit: 1, Cursor: page1.Value.NextCursor));
        Assert.False(page2.IsSuccess);
        Assert.Equal(NavigationErrorCodes.CursorExpired, page2.Code);
    }
}
