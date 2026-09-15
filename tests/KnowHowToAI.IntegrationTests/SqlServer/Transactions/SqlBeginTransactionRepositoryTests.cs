using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
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

            INSERT INTO dbo.KnowHowToAI_Role (SnapshotId, RoleId, Name, Description, IsDeleted)
            VALUES (@snapshotId, N'Developer', N'Entwicklung', N'Technische Rolle', 0);

            INSERT INTO dbo.KnowHowToAI_RoleResolution (
                SnapshotId, RequestedRoleId, CandidateRoleId, Priority)
            VALUES (@snapshotId, N'Developer', N'Default', 1);

            INSERT INTO dbo.KnowHowToAI_Node (
                SnapshotId, NodeId, ParentNodeId, Title, Description, SortOrder, IsDeleted)
            VALUES
                (@snapshotId, @rootNodeId, NULL, N'Wurzel', N'Beschreibung der Wurzel', 0, 0),
                (@snapshotId, @childNodeId, @rootNodeId, N'Kind', N'Beschreibung des Kindes', 0, 0);

            INSERT INTO dbo.KnowHowToAI_NodeContent (
                SnapshotId, NodeId, RoleId, ContentRevisionId, ContentMode, ContentMd, IsDeleted)
            VALUES
                (@snapshotId, @rootNodeId, N'Default', @sourceRevisionId, 'Independent', N'Quellinhalt', 0),
                (@snapshotId, @childNodeId, N'Developer', @targetRevisionId, 'Derived', N'Abgeleiteter Inhalt', 0);

            INSERT INTO dbo.KnowHowToAI_ContentDependency (
                SnapshotId, TargetNodeId, TargetRoleId, SourceNodeId, SourceRoleId, SourceContentRevisionId)
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
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_Role WHERE SnapshotId = @snapshotId),
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_RoleResolution WHERE SnapshotId = @snapshotId),
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
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_Role
                    WHERE SnapshotId = @snapshotId AND RoleId = N'Developer'
                      AND Name = N'Entwicklung' AND Description = N'Technische Rolle' AND IsDeleted = 0),
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_RoleResolution
                    WHERE SnapshotId = @snapshotId AND RequestedRoleId = N'Developer'
                      AND CandidateRoleId = N'Default' AND Priority = 1),
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_Node
                    WHERE SnapshotId = @snapshotId AND NodeId = @childNodeId AND ParentNodeId = @rootNodeId
                      AND Title = N'Kind' AND Description = N'Beschreibung des Kindes' AND SortOrder = 0 AND IsDeleted = 0),
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_NodeContent
                    WHERE SnapshotId = @snapshotId AND NodeId = @childNodeId AND RoleId = N'Developer'
                      AND ContentRevisionId = @targetRevisionId AND ContentMode = 'Derived'
                      AND ContentMd = N'Abgeleiteter Inhalt' AND IsDeleted = 0),
                (SELECT COUNT_BIG(*) FROM dbo.KnowHowToAI_ContentDependency
                    WHERE SnapshotId = @snapshotId AND TargetNodeId = @childNodeId AND TargetRoleId = N'Developer'
                      AND SourceNodeId = @rootNodeId AND SourceRoleId = N'Default'
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

    private sealed record SeededSnapshot(
        SnapshotId BaseSnapshotId,
        Guid RootNodeId,
        Guid ChildNodeId,
        Guid SourceRevisionId,
        Guid TargetRevisionId);

    private sealed record SnapshotAreaCounts(
        long Roles,
        long RoleResolutions,
        long Nodes,
        long NodeContents,
        long ContentDependencies);
}
