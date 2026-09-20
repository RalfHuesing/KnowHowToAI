using Dapper;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.History;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.Storage.SqlServer.Repositories.Snapshots;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Repositories;

[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed class SqlReleaseIntegrationTests
{
    [Fact]
    public async Task ReleaseLifecycle_CreateListAndFindings_BehavesCorrectlyInDatabase()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var policy = new SqlStoragePolicy { CommandTimeoutSeconds = 30 };
        var snapshotRepo = new SqlSnapshotRepository(database.ConnectionFactory, policy);
        var releaseRepo = new SqlReleaseRepository(database.ConnectionFactory, policy);

        var initialSnapshot = await snapshotRepo.GetCurrentAsync();

        // 1. Create a release
        var now = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        var createResult = await releaseRepo.CreateAsync(new CreateReleaseRecord(
            "v1.0.0",
            initialSnapshot.SnapshotId,
            now,
            "Initial production release"));

        Assert.True(createResult.IsSuccess);
        var release = createResult.Value!;
        Assert.True(release.ReleaseId.Value > 0);
        Assert.Equal("v1.0.0", release.Name);
        Assert.Equal("Initial production release", release.Description);
        Assert.Equal(initialSnapshot.SnapshotId, release.SnapshotId);

        // 2. Find release by ID
        var foundRelease = await releaseRepo.FindAsync(release.ReleaseId);
        Assert.NotNull(foundRelease);
        Assert.Equal(release.ReleaseId, foundRelease.ReleaseId);
        Assert.Equal("v1.0.0", foundRelease.Name);

        // 3. Duplicate release name should fail with ReleaseNameConflict
        var duplicateResult = await releaseRepo.CreateAsync(new CreateReleaseRecord(
            "v1.0.0",
            initialSnapshot.SnapshotId,
            now));

        Assert.False(duplicateResult.IsSuccess);
        Assert.Equal(ReleaseErrorCodes.ReleaseNameConflict, duplicateResult.Error!.Code);

        // 4. Create more releases for pagination test
        var r2 = await releaseRepo.CreateAsync(new CreateReleaseRecord("v1.1.0", initialSnapshot.SnapshotId, now.AddHours(1)));
        var r3 = await releaseRepo.CreateAsync(new CreateReleaseRecord("v1.2.0", initialSnapshot.SnapshotId, now.AddHours(2)));
        Assert.True(r2.IsSuccess);
        Assert.True(r3.IsSuccess);

        // 5. Paging with ListAsync
        var page1 = await releaseRepo.ListAsync(limit: 2, afterReleaseId: null);
        Assert.Equal(2, page1.Count);
        Assert.Equal("v1.0.0", page1[0].Name);
        Assert.Equal("v1.1.0", page1[1].Name);

        var page2 = await releaseRepo.ListAsync(limit: 2, afterReleaseId: page1[^1].ReleaseId.Value);
        var remainingRelease = Assert.Single(page2);
        Assert.Equal("v1.2.0", remainingRelease.Name);

        // 6. Test ReleaseService transparent findings on committed snapshot with stale content
        var historyRepos = new SnapshotReadRepositories(
            snapshotRepo,
            new SqlTransactionRepository(database.ConnectionFactory, policy),
            new SqlHierarchyRepository(database.ConnectionFactory, policy),
            new SqlContentRepository(database.ConnectionFactory, policy),
            new SqlAudienceRepository(database.ConnectionFactory, policy),
            new SqlDependencyRepository(database.ConnectionFactory, policy));

        var releaseService = new ReleaseService(
            historyRepos,
            releaseRepo,
            new StaticClock(now),
            new RetrievalPolicy { DefaultPageSize = 2, MaximumPageSize = 10 },
            new ValidationPolicy
            {
                ContentSizeWarningBytes = 4096,
                ChildCountWarning = 25,
                HierarchyDepthWarning = 8,
                PossibleEmbeddedHeadingWarning = true
            });

        // Insert derived content that is stale in the database
        await SeedStaleDerivedContentInDatabaseAsync(database, initialSnapshot.SnapshotId);

        var serviceReleaseResult = await releaseService.CreateReleaseAsync("v2.0.0", initialSnapshot.SnapshotId, "Release with stale content");
        Assert.True(serviceReleaseResult.IsSuccess);
        Assert.NotNull(serviceReleaseResult.Value);
        Assert.Equal("v2.0.0", serviceReleaseResult.Value.Release.Name);
        Assert.NotEmpty(serviceReleaseResult.Value.Findings);
        Assert.Contains(serviceReleaseResult.Value.Findings, f => f.Code == QualityWarningCodes.StaleDerivedContent);
    }

    [Fact]
    public async Task CreateAsync_SnapshotDiscardedBetweenPreCheckAndRepositoryCall_RejectsWithoutReleaseRecord()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var policy = new SqlStoragePolicy { CommandTimeoutSeconds = 30 };
        var snapshotRepo = new SqlSnapshotRepository(database.ConnectionFactory, policy);
        var releaseRepo = new SqlReleaseRepository(database.ConnectionFactory, policy);
        var now = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        var transaction = await new SqlTransactionRepository(database.ConnectionFactory, policy)
            .BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.NewGuid()), null, null, "xUnit"));
        var snapshotId = transaction.WorkingSnapshotId;

        // Service-Vorprüfung: der referenzierte Snapshot ist committed.
        await SetSnapshotStateAsync(database, snapshotId, "Committed", DateTime.UtcNow);
        var preCheck = await snapshotRepo.FindAsync(snapshotId);
        Assert.NotNull(preCheck);
        Assert.Equal(SnapshotState.Committed, preCheck.State);

        // Rennen: der Snapshot-Zustand ändert sich genau zwischen Vorprüfung und Repository-Aufruf.
        await SetSnapshotStateAsync(database, snapshotId, "Discarded", null);

        var result = await releaseRepo.CreateAsync(new CreateReleaseRecord(
            "v1.0.0",
            snapshotId,
            now,
            "Zu spät verworfen"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ReleaseErrorCodes.SnapshotNotCommitted, result.Error!.Code);
        Assert.Equal(0, await CountReleasesAsync(database));
    }

    [Fact]
    public async Task CreateAsync_MissingSnapshotWorkingSnapshotAndDuplicateName_MapDistinctErrorCodes()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var policy = new SqlStoragePolicy { CommandTimeoutSeconds = 30 };
        var snapshotRepo = new SqlSnapshotRepository(database.ConnectionFactory, policy);
        var releaseRepo = new SqlReleaseRepository(database.ConnectionFactory, policy);
        var now = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

        // 1. Fehlender Snapshot
        var missing = await releaseRepo.CreateAsync(new CreateReleaseRecord("v1.0.0", new SnapshotId(987654321), now));
        Assert.False(missing.IsSuccess);
        Assert.Equal(ReleaseErrorCodes.SnapshotNotFound, missing.Error!.Code);
        Assert.Equal("987654321", missing.Error.Details[ReleaseErrorCodes.SnapshotIdDetail]);

        // 2. Nicht committed: Working Snapshot einer offenen Transaction
        var transaction = await new SqlTransactionRepository(database.ConnectionFactory, policy)
            .BeginAsync(new BeginTransactionRequest(new TransactionId(Guid.NewGuid()), null, null, "xUnit"));
        var working = await releaseRepo.CreateAsync(new CreateReleaseRecord("v1.0.0", transaction.WorkingSnapshotId, now));
        Assert.False(working.IsSuccess);
        Assert.Equal(ReleaseErrorCodes.SnapshotNotCommitted, working.Error!.Code);
        Assert.Equal(transaction.WorkingSnapshotId.ToString(), working.Error.Details[ReleaseErrorCodes.SnapshotIdDetail]);

        // 3. Doppelter Name nach erfolgreichem Release
        var initialSnapshot = await snapshotRepo.GetCurrentAsync();
        var created = await releaseRepo.CreateAsync(new CreateReleaseRecord("v1.0.0", initialSnapshot.SnapshotId, now));
        Assert.True(created.IsSuccess);
        var duplicate = await releaseRepo.CreateAsync(new CreateReleaseRecord("v1.0.0", initialSnapshot.SnapshotId, now));
        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ReleaseErrorCodes.ReleaseNameConflict, duplicate.Error!.Code);
        Assert.Equal("v1.0.0", duplicate.Error.Details[ReleaseErrorCodes.ReleaseNameDetail]);

        Assert.Equal(1, await CountReleasesAsync(database));
    }

    private static Task SetSnapshotStateAsync(
        SqlTestDatabase database,
        SnapshotId snapshotId,
        string state,
        DateTime? committedAtUtc) =>
        database.ExecuteAsync(
            "UPDATE dbo.KnowHowToAI_Snapshot SET State = @state, CommittedAtUtc = @committedAtUtc WHERE SnapshotId = @snapshotId;",
            new SqlParameter("@state", state),
            new SqlParameter("@committedAtUtc", committedAtUtc ?? (object)DBNull.Value),
            new SqlParameter("@snapshotId", snapshotId.Value));

    private static async Task<int> CountReleasesAsync(SqlTestDatabase database)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.KnowHowToAI_Release;");
    }

    private static async Task SeedStaleDerivedContentInDatabaseAsync(SqlTestDatabase db, SnapshotId snapshotId)
    {
        await using var conn = await db.ConnectionFactory.OpenAsync();
        var nodeId = Guid.NewGuid();
        var sourceRev = Guid.NewGuid();
        var oldRev = Guid.NewGuid();

        await conn.ExecuteAsync("""
            INSERT INTO dbo.KnowHowToAI_Audience (SnapshotId, AudienceId, Name, Description, IsDeleted)
            VALUES (@snap, 'enduser', 'EndUser', 'End user audience', 0);

            INSERT INTO dbo.KnowHowToAI_AudienceResolution (SnapshotId, RequestedAudienceId, Priority, CandidateAudienceId)
            VALUES (@snap, 'enduser', 1, 'enduser');

            INSERT INTO dbo.KnowHowToAI_Node (SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
            VALUES (@snap, @nodeId, NULL, 'Node For Stale Test', NULL, 100, 0);

            -- Default content with current source revision
            INSERT INTO dbo.KnowHowToAI_NodeContent (SnapshotId, NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
            VALUES (@snap, @nodeId, N'Default', @sourceRev, 'Independent', 'Source content text', 0);

            -- EndUser derived content with dependency on OLD revision
            INSERT INTO dbo.KnowHowToAI_NodeContent (SnapshotId, NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
            VALUES (@snap, @nodeId, 'enduser', NEWID(), 'Derived', 'Derived content text', 0);

            INSERT INTO dbo.KnowHowToAI_ContentDependency (SnapshotId, TargetNodeId, TargetAudienceId, SourceNodeId, SourceAudienceId, SourceContentRevisionId)
            VALUES (@snap, @nodeId, 'enduser', @nodeId, N'Default', @oldRev);
            """, new { snap = snapshotId.Value, nodeId, sourceRev, oldRev });
    }

    private sealed class StaticClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
