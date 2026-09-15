using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;

namespace KnowHowToAI.IntegrationTests.SqlServer.Repositories;

[Trait("Category", "Unit")]
public sealed class SqlStorageMappingTests
{
    [Fact]
    public void ToDomain_MapsAllPersistedEnumsAndUtcTimestamp()
    {
        var createdAt = new DateTime(2026, 9, 15, 10, 30, 0, DateTimeKind.Unspecified);
        var content = SqlRowMapper.ToNodeContent(new NodeContentRow(
            42,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Developer",
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "Derived",
            "Inhalt",
            false));
        var snapshot = SqlRowMapper.ToSnapshot(new SnapshotRow(42, 41, "Committed", createdAt, createdAt));

        Assert.Equal(new SnapshotId(42), snapshot.SnapshotId);
        Assert.Equal(SnapshotState.Committed, snapshot.State);
        Assert.Equal(TimeSpan.Zero, snapshot.CreatedAtUtc.Offset);
        Assert.Equal(ContentMode.Derived, content.ContentMode);
        Assert.Equal(new RoleId("Developer"), content.RoleId);
    }

    [Fact]
    public void ToDomain_RejectsUnknownPersistedContentMode()
    {
        var row = new NodeContentRow(1, Guid.NewGuid(), "Default", Guid.NewGuid(), "Unknown", "Text", false);

        var exception = Assert.Throws<InvalidOperationException>(() => SqlRowMapper.ToNodeContent(row));

        Assert.Contains("NodeContent.ContentMode", exception.Message);
    }

    [Fact]
    public void Create_AppliesConfiguredTimeoutParametersAndCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var parameters = new { snapshotId = 7L };
        var definition = SqlCommandFactory.Create(
            "SELECT @snapshotId;",
            parameters,
            new SqlStoragePolicy { CommandTimeoutSeconds = 17 },
            cancellation.Token);

        Assert.Equal(17, definition.CommandTimeout);
        Assert.Same(parameters, definition.Parameters);
        Assert.Equal(cancellation.Token, definition.CancellationToken);
    }
}
