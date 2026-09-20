using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Transactions;

/// <summary>Belegt die atomare vollständige Snapshot-Kopie von M3.2 gegen SQL Server.</summary>
[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed class SqlBeginTransactionRepositoryTests
{
    [Fact]
    public async Task BeginAsync_CopiesAllVersionedAreasAndRegistersOpenTransaction()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var seed = await SeedSourceSnapshotAsync(database);
        var sourceCounts = await GetAreaCountsAsync(database, seed.BaseSnapshotId);
        var repository = new SqlTransactionRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var transactionId = new TransactionId(Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"));

        var transaction = await repository.BeginAsync(
            new BeginTransactionRequest(transactionId, "Snapshot-Kopie", "Integrationstest", "xUnit"));

        Assert.Equal(transactionId, transaction.TransactionId);
        Assert.Equal(seed.BaseSnapshotId, transaction.BaseSnapshotId);
        Assert.Equal(TransactionState.Open, transaction.State);
        Assert.Equal(0, transaction.ChangeVersion);
        Assert.Null(transaction.CommittedAtUtc);
        Assert.Equal("Snapshot-Kopie", transaction.Purpose);
        Assert.Equal("Integrationstest", transaction.Actor);
        Assert.Equal("xUnit", transaction.Client);

        var workingSnapshot = transaction.WorkingSnapshotId;
        var workingCounts = await GetAreaCountsAsync(database, workingSnapshot);
        Assert.Equal(sourceCounts, workingCounts);
        await AssertSeedRowsCopiedAsync(database, workingSnapshot, seed);
        await AssertCurrentSnapshotAsync(database, seed.BaseSnapshotId);
    }

    [Fact]
    public async Task BeginAsync_RollsBackWorkingSnapshotAndTransactionWhenCopyFails()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var seed = await SeedSourceSnapshotAsync(database);
        var sourceCounts = await GetAreaCountsAsync(database, seed.BaseSnapshotId);
        var transactionId = new TransactionId(Guid.Parse("01234567-89ab-cdef-0123-456789abcdea"));
        await database.ExecuteAsync("""
            CREATE TRIGGER dbo.KnowHowToAI_BeginFailureProbe
            ON dbo.KnowHowToAI_NodeContent
            AFTER INSERT
            AS
            BEGIN
                THROW 51002, 'Injected begin failure.', 1;
            END;
            """);
        var repository = new SqlTransactionRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

        await Assert.ThrowsAsync<SqlException>(() => repository.BeginAsync(
            new BeginTransactionRequest(transactionId, "Fehlschlag", "Integrationstest", "xUnit")));

        Assert.Equal(sourceCounts, await GetAreaCountsAsync(database, seed.BaseSnapshotId));
        await AssertCurrentSnapshotAsync(database, seed.BaseSnapshotId);
        await AssertBeginFailureLeftNoPersistedStateAsync(database, transactionId);
    }

    [Fact]
    public async Task CommitAsync_PreservesACompleteHistoricalSnapshotValueForValueAfterLaterCommit()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        await SeedSourceSnapshotAsync(database);
        var repository = new SqlTransactionRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var first = await repository.BeginAsync(new BeginTransactionRequest(
            new TransactionId(Guid.Parse("01234567-89ab-cdef-0123-456789abcdeb")),
            "Historischer Ausgangsstand",
            "Integrationstest",
            "xUnit"));

        var firstCommit = await repository.CommitAsync(CreateCommitRequest(first.TransactionId));
        Assert.True(firstCommit.IsCommitted);
        var beforeLaterCommit = await ReadVersionedSnapshotValueAsync(database, first.WorkingSnapshotId);

        var second = await repository.BeginAsync(new BeginTransactionRequest(
            new TransactionId(Guid.Parse("01234567-89ab-cdef-0123-456789abcdec")),
            "Spätere Änderung",
            "Integrationstest",
            "xUnit"));
        await database.ExecuteAsync("""
            UPDATE dbo.KnowHowToAI_Audience
            SET Description = N'Nur im späteren Snapshot geändert'
            WHERE SnapshotId = @snapshotId AND AudienceId = N'Developer';
            """,
            new SqlParameter("@snapshotId", second.WorkingSnapshotId.Value));

        var secondCommit = await repository.CommitAsync(CreateCommitRequest(second.TransactionId));

        Assert.True(secondCommit.IsCommitted);
        Assert.Equal(beforeLaterCommit, await ReadVersionedSnapshotValueAsync(database, first.WorkingSnapshotId));
        Assert.Equal("Committed", await ReadSnapshotStateAsync(database, first.WorkingSnapshotId));
        Assert.Equal("Nur im späteren Snapshot geändert", await ReadAudienceDescriptionAsync(database, second.WorkingSnapshotId, "Developer"));
    }

    private static async Task<SeededSnapshot> SeedSourceSnapshotAsync(SqlTestDatabase database)
    {
        var rootNodeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var childNodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var sourceRevisionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var targetRevisionId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DECLARE @snapshotId BIGINT = (
                SELECT CurrentSnapshotId
                FROM dbo.KnowHowToAI_SystemState
                WHERE Id = 1
            );

            INSERT INTO dbo.KnowHowToAI_Audience (SnapshotId, AudienceId, Name, Description, IsDeleted)
            VALUES (@snapshotId, N'Developer', N'Entwicklung', N'Technische Zielgruppe', 0);

            INSERT INTO dbo.KnowHowToAI_AudienceResolution (
                SnapshotId, RequestedAudienceId, CandidateAudienceId, Priority)
            VALUES (@snapshotId, N'Developer', N'Default', 1);

            INSERT INTO dbo.KnowHowToAI_Node (
                SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
            VALUES
                (@snapshotId, @rootNodeId, NULL, N'Wurzel', N'Beschreibung der Wurzel', 0, 0),
                (@snapshotId, @childNodeId, @rootNodeId, N'Kind', N'Beschreibung des Kindes', 0, 0);

            INSERT INTO dbo.KnowHowToAI_NodeContent (
                SnapshotId, NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
            VALUES
                (@snapshotId, @rootNodeId, N'Default', @sourceRevisionId, 'Independent', N'Quellinhalt', 0),
                (@snapshotId, @childNodeId, N'Developer', @targetRevisionId, 'Derived', N'Abgeleiteter Inhalt', 0);

            INSERT INTO dbo.KnowHowToAI_ContentDependency (
                SnapshotId, TargetNodeId, TargetAudienceId, SourceNodeId, SourceAudienceId, SourceContentRevisionId)
            VALUES (
                @snapshotId,
                @childNodeId,
                N'Developer',
                @rootNodeId,
                N'Default',
                @sourceRevisionId);

            SELECT @snapshotId;
            """;
        command.Parameters.Add(new SqlParameter("@rootNodeId", rootNodeId));
        command.Parameters.Add(new SqlParameter("@childNodeId", childNodeId));
        command.Parameters.Add(new SqlParameter("@sourceRevisionId", sourceRevisionId));
        command.Parameters.Add(new SqlParameter("@targetRevisionId", targetRevisionId));

        var snapshotId = (long)(await command.ExecuteScalarAsync())!;
        return new SeededSnapshot(
            new SnapshotId(snapshotId),
            rootNodeId,
            childNodeId,
            sourceRevisionId,
            targetRevisionId);
    }

    private static async Task<SnapshotAreaCounts> GetAreaCountsAsync(
        SqlTestDatabase database,
        SnapshotId snapshotId)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_Audience WHERE SnapshotId = @snapshotId),
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_AudienceResolution WHERE SnapshotId = @snapshotId),
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_Node WHERE SnapshotId = @snapshotId),
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_NodeContent WHERE SnapshotId = @snapshotId),
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_ContentDependency WHERE SnapshotId = @snapshotId);
            """;
        command.Parameters.Add(new SqlParameter("@snapshotId", snapshotId.Value));

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new SnapshotAreaCounts(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            reader.GetInt64(3),
            reader.GetInt64(4));
    }

    private static async Task AssertSeedRowsCopiedAsync(
        SqlTestDatabase database,
        SnapshotId workingSnapshotId,
        SeededSnapshot seed)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_Audience
                    WHERE SnapshotId = @snapshotId AND AudienceId = N'Developer'
                      AND Name = N'Entwicklung' AND Description = N'Technische Zielgruppe' AND IsDeleted = 0),
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_AudienceResolution
                    WHERE SnapshotId = @snapshotId AND RequestedAudienceId = N'Developer'
                      AND CandidateAudienceId = N'Default' AND Priority = 1),
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_Node
                    WHERE SnapshotId = @snapshotId AND NodeId = @childNodeId AND ParentNodeId = @rootNodeId
                      AND Title = N'Kind' AND Description = N'Beschreibung des Kindes' AND SortOrder = 0 AND IsDeleted = 0),
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_NodeContent
                    WHERE SnapshotId = @snapshotId AND NodeId = @childNodeId AND AudienceId = N'Developer'
                      AND ContentRevisionId = @targetRevisionId AND ContentMode = 'Derived'
                      AND ContentMd = N'Abgeleiteter Inhalt' AND IsDeleted = 0),
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_ContentDependency
                    WHERE SnapshotId = @snapshotId AND TargetNodeId = @childNodeId AND TargetAudienceId = N'Developer'
                      AND SourceNodeId = @rootNodeId AND SourceAudienceId = N'Default'
                      AND SourceContentRevisionId = @sourceRevisionId);
            """;
        command.Parameters.Add(new SqlParameter("@snapshotId", workingSnapshotId.Value));
        command.Parameters.Add(new SqlParameter("@rootNodeId", seed.RootNodeId));
        command.Parameters.Add(new SqlParameter("@childNodeId", seed.ChildNodeId));
        command.Parameters.Add(new SqlParameter("@sourceRevisionId", seed.SourceRevisionId));
        command.Parameters.Add(new SqlParameter("@targetRevisionId", seed.TargetRevisionId));

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        for (var index = 0; index < reader.FieldCount; index++)
            Assert.Equal(1L, reader.GetInt64(index));
    }

    private static async Task AssertCurrentSnapshotAsync(SqlTestDatabase database, SnapshotId expectedSnapshotId)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CurrentSnapshotId FROM dbo.KnowHowToAI_SystemState WHERE Id = 1;";

        var actualSnapshotId = (long)(await command.ExecuteScalarAsync())!;
        Assert.Equal(expectedSnapshotId, new SnapshotId(actualSnapshotId));
    }

    private static CommitTransactionRequest CreateCommitRequest(TransactionId transactionId) =>
        new(transactionId, null, new QualityWarningThresholds(4096, 25, 8), WarnOnPossibleEmbeddedHeading: true);

    private static async Task AssertBeginFailureLeftNoPersistedStateAsync(
        SqlTestDatabase database,
        TransactionId transactionId)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_Transaction WHERE TransactionId = @transactionId),
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_Snapshot WHERE State = 'Working');
            """;
        command.Parameters.Add(new SqlParameter("@transactionId", transactionId.Value));

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(0L, reader.GetInt64(0));
        Assert.Equal(0L, reader.GetInt64(1));
    }

    private static async Task<string> ReadVersionedSnapshotValueAsync(SqlTestDatabase database, SnapshotId snapshotId)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT (
                SELECT
                    (SELECT AudienceId, Name, Description, IsDeleted
                     FROM dbo.KnowHowToAI_Audience
                     WHERE SnapshotId = @snapshotId ORDER BY AudienceId FOR JSON PATH, INCLUDE_NULL_VALUES) AS Audiences,
                    (SELECT RequestedAudienceId, CandidateAudienceId, Priority
                     FROM dbo.KnowHowToAI_AudienceResolution
                     WHERE SnapshotId = @snapshotId ORDER BY RequestedAudienceId, Priority FOR JSON PATH, INCLUDE_NULL_VALUES) AS AudienceResolutions,
                    (SELECT NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted
                     FROM dbo.KnowHowToAI_Node
                     WHERE SnapshotId = @snapshotId ORDER BY SortOrder, NodeId FOR JSON PATH, INCLUDE_NULL_VALUES) AS Nodes,
                    (SELECT NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd, IsDeleted
                     FROM dbo.KnowHowToAI_NodeContent
                     WHERE SnapshotId = @snapshotId ORDER BY NodeId, AudienceId FOR JSON PATH, INCLUDE_NULL_VALUES) AS Contents,
                    (SELECT TargetNodeId, TargetAudienceId, SourceNodeId, SourceAudienceId, SourceContentRevisionId
                     FROM dbo.KnowHowToAI_ContentDependency
                     WHERE SnapshotId = @snapshotId ORDER BY TargetNodeId, TargetAudienceId, SourceNodeId, SourceAudienceId FOR JSON PATH, INCLUDE_NULL_VALUES) AS Dependencies
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES
            );
            """;
        command.Parameters.Add(new SqlParameter("@snapshotId", snapshotId.Value));

        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<string> ReadSnapshotStateAsync(SqlTestDatabase database, SnapshotId snapshotId)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT State FROM dbo.KnowHowToAI_Snapshot WHERE SnapshotId = @snapshotId;";
        command.Parameters.Add(new SqlParameter("@snapshotId", snapshotId.Value));

        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<string> ReadAudienceDescriptionAsync(
        SqlTestDatabase database,
        SnapshotId snapshotId,
        string audienceId)
    {
        await using var connection = await database.ConnectionFactory.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Description
            FROM dbo.KnowHowToAI_Audience
            WHERE SnapshotId = @snapshotId AND AudienceId = @audienceId;
            """;
        command.Parameters.Add(new SqlParameter("@snapshotId", snapshotId.Value));
        command.Parameters.Add(new SqlParameter("@audienceId", audienceId));

        return (string)(await command.ExecuteScalarAsync())!;
    }

    private sealed record SeededSnapshot(
        SnapshotId BaseSnapshotId,
        Guid RootNodeId,
        Guid ChildNodeId,
        Guid SourceRevisionId,
        Guid TargetRevisionId);

    private sealed record SnapshotAreaCounts(
        long Audiences,
        long AudienceResolutions,
        long Nodes,
        long NodeContents,
        long ContentDependencies);
}
