using System.Text.Json;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.History;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.Storage.SqlServer.Repositories.Snapshots;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;

namespace KnowHowToAI.IntegrationTests.SqlServer.Abnahme;

/// <summary>
/// M5.12-Abnahme: weist anhand fester IDs und Zeitwerte alle fünf Diff-Kategorien
/// (Nodes, Roles, RoleResolutions, Contents, Dependencies) mit Added, Modified und
/// Deleted über die echte SQL-Datenbank nach, reproduziert die Diffs historisch nach
/// spaeteren Commits und prueft das Release-Listing über mehrere Cursor-Seiten.
/// </summary>
[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed partial class SqlDiffReleaseAbnahmeTests
{
    private static readonly TransactionId ErsteTransactionId = new(Guid.Parse("51210000-0000-0000-0000-000000000001"));
    private static readonly TransactionId AenderungsTransactionId = new(Guid.Parse("51210000-0000-0000-0000-000000000002"));
    private static readonly TransactionId FolgetransactionId = new(Guid.Parse("51210000-0000-0000-0000-000000000003"));

    private static readonly AudienceId RoleEntwickler = new("Entwickler");
    private static readonly AudienceId RoleEndanwender = new("Endanwender");
    private static readonly AudienceId RoleBerater = new("Berater");
    private static readonly AudienceId RoleProjekt = new("Projekt");
    private static readonly AudienceId RoleAdmin = new("Admin");

    [Fact]
    public async Task DiffKategorien_WerdenDeterministischUndHistorischReproduzierbarNachgewiesen()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var historyService = CreateHistoryService(database);
        var identifierGenerator = new SequentialIdentifierGenerator();
        var baseSnapshot = await SeedBaseSnapshotAsync(database, identifierGenerator);
        var (geaenderterSnapshotId, changes) = await CommitChangeTransactionAsync(database, baseSnapshot, identifierGenerator);
        var folgeSnapshotId = await CommitFollowUpTransactionAsync(database, baseSnapshot.WurzelNodeId, identifierGenerator);

        var aenderungsDiff = await RequireDiffAsync(
            historyService.CompareSnapshotsAsync(baseSnapshot.SnapshotId, geaenderterSnapshotId));
        AssertDiffCategoryCounts(aenderungsDiff, nodes: 3, roles: 3, resolutions: 5, contents: 6, dependencies: 3);
        AssertChangeDiffContent(aenderungsDiff, changes);

        var reproduziert = await RequireDiffAsync(
            historyService.CompareSnapshotsAsync(baseSnapshot.SnapshotId, geaenderterSnapshotId));
        Assert.Equal(ToReportableJson(aenderungsDiff), ToReportableJson(reproduziert));

        var historisch = await historyService.GetTransactionChangesAsync(AenderungsTransactionId);
        Assert.True(historisch.IsSuccess, historisch.Message);
        Assert.Equal(ToReportableJson(aenderungsDiff), ToReportableJson(historisch.Value!.Changes));

        var folgeDiff = await RequireDiffAsync(
            historyService.CompareSnapshotsAsync(geaenderterSnapshotId, folgeSnapshotId));
        AssertDiffCategoryCounts(folgeDiff, nodes: 1, roles: 0, resolutions: 0, contents: 0, dependencies: 0);
        Assert.Equal(DiffChangeKind.Added, Assert.Single(folgeDiff.Nodes).Kind);
    }

    [Fact]
    public async Task ReleaseListing_PaginiertDeterministischUeberMehrereSeiten()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var policy = new SqlStoragePolicy { CommandTimeoutSeconds = 60 };
        var snapshotRepository = new SqlSnapshotRepository(database.ConnectionFactory, policy);
        var releaseRepository = new SqlReleaseRepository(database.ConnectionFactory, policy);
        var historyRepositories = new SnapshotReadRepositories(
            snapshotRepository,
            new SqlTransactionRepository(database.ConnectionFactory, policy),
            new SqlHierarchyRepository(database.ConnectionFactory, policy),
            new SqlContentRepository(database.ConnectionFactory, policy),
            new SqlRoleRepository(database.ConnectionFactory, policy),
            new SqlDependencyRepository(database.ConnectionFactory, policy));
        var releaseService = new ReleaseService(
            historyRepositories,
            releaseRepository,
            new StaticClock(new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero)),
            new RetrievalPolicy { DefaultPageSize = 2, MaximumPageSize = 10 },
            new ValidationPolicy
            {
                ContentSizeWarningBytes = 4096,
                ChildCountWarning = 50,
                HierarchyDepthWarning = 10
            });

        var aktuellerSnapshot = await snapshotRepository.GetCurrentAsync();
        var erwarteteIds = new List<ReleaseId>();
        for (var index = 1; index <= 6; index++)
        {
            var created = await releaseService.CreateReleaseAsync(
                $"rel-{index:00}", aktuellerSnapshot.SnapshotId, $"Beschreibung {index:00}");
            Assert.True(created.IsSuccess, created.Message);
            erwarteteIds.Add(created.Value!.Release.ReleaseId);
        }

        var gefundeneNamen = new List<string>();
        var gefundeneIds = new List<ReleaseId>();
        string? cursor = null;
        var seiten = 0;
        while (seiten < 10)
        {
            var result = await releaseService.ListReleasesAsync(2, cursor);
            Assert.True(result.IsSuccess, result.Message);
            if (result.Value!.Items.Count == 0)
                break;

            seiten++;
            Assert.Equal(2, result.Value.Items.Count);
            gefundeneNamen.AddRange(result.Value.Items.Select(release => release.Name));
            gefundeneIds.AddRange(result.Value.Items.Select(release => release.ReleaseId));
            cursor = result.Value.NextCursor;
            if (cursor is null)
                break;
        }

        Assert.Equal(3, seiten);
        Assert.Equal(new[] { "rel-01", "rel-02", "rel-03", "rel-04", "rel-05", "rel-06" }, gefundeneNamen);
        Assert.Equal(erwarteteIds, gefundeneIds);
        Assert.Null(cursor);
    }

    private static async Task<BaseSnapshotSeed> SeedBaseSnapshotAsync(
        SqlTestDatabase database,
        SequentialIdentifierGenerator identifierGenerator)
    {
        await using var session = await WorkingTransactionSession.BeginAsync(
            database, ErsteTransactionId, identifierGenerator, "M5.12 Diff-Basissnapshot");

        await session.CreateRoleAsync("Entwickler", null);
        await session.CreateRoleAsync("Endanwender", null);
        await session.CreateRoleAsync("Berater", null);
        await session.CreateRoleAsync("Projekt", null);
        await session.SetResolutionAsync(RoleEntwickler, RoleEntwickler, RoleEndanwender);
        await session.SetResolutionAsync(RoleEndanwender, RoleEndanwender, RoleEntwickler);
        await session.SetResolutionAsync(RoleBerater, RoleBerater, RoleEndanwender);

        var wurzel = await session.CreateNodeAsync(null, "Wurzel Diff", null, 0);
        var knotenA = await session.CreateNodeAsync(wurzel.NodeId, "A Titel eins", null, 1);
        var knotenB = await session.CreateNodeAsync(wurzel.NodeId, "B Titel eins", null, 2);
        var knotenC = await session.CreateNodeAsync(wurzel.NodeId, "C Titel eins", null, 3);

        var contentA = await session.ReplaceIndependentContentAsync(
            knotenA.NodeId, RoleEntwickler, "A Entwickler Stand eins.");
        var contentAEndanwender = await session.ReplaceIndependentContentAsync(
            knotenA.NodeId, RoleEndanwender, "A Endanwender Stand eins.");
        await session.ReplaceDerivedContentAsync(
            knotenB.NodeId, RoleEntwickler, "B abgeleitet von A eins.",
            [new ContentDependencySource(knotenA.NodeId, RoleEntwickler, contentA.ContentRevisionId)]);
        await session.ReplaceIndependentContentAsync(knotenC.NodeId, RoleEntwickler, "C Entwickler Stand eins.");
        await session.ReplaceDerivedContentAsync(
            knotenC.NodeId, RoleBerater, "C Berater abgeleitet.",
            [new ContentDependencySource(knotenA.NodeId, RoleEndanwender, contentAEndanwender.ContentRevisionId)]);

        var committed = await session.CommitAsync(database, "Commit Diff-Basissnapshot");
        return new BaseSnapshotSeed(
            committed.WorkingSnapshotId,
            wurzel.NodeId,
            knotenA.NodeId,
            knotenB.NodeId,
            knotenC.NodeId);
    }

    private static async Task<(SnapshotId SnapshotId, ChangeTransactionIds Changes)> CommitChangeTransactionAsync(
        SqlTestDatabase database,
        BaseSnapshotSeed basis,
        SequentialIdentifierGenerator identifierGenerator)
    {
        await using var session = await WorkingTransactionSession.BeginAsync(
            database, AenderungsTransactionId, identifierGenerator, "M5.12 Diff-Aenderungen");

        var geaenderterKnoten = await session.UpdateNodeAsync(basis.KnotenA, "A Titel zwei", null);
        var geloeschterKnoten = await session.DeleteNodeSubtreeAsync(basis.KnotenC);
        var neuerKnoten = await session.CreateNodeAsync(basis.WurzelNodeId, "D Titel", null, 10);

        await session.CreateRoleAsync("Admin", null);
        await session.UpdateRoleDescriptionAsync(RoleEndanwender, "Endanwender", "Anwendersicht aktualisiert");
        await session.DeleteRoleAsync(RoleProjekt);

        await session.SetResolutionAsync(RoleEntwickler, RoleEndanwender, RoleEntwickler);
        await session.SetResolutionAsync(RoleAdmin, RoleAdmin);
        await session.SetResolutionAsync(RoleBerater);

        var contentAGeuendert = await session.ReplaceIndependentContentAsync(
            basis.KnotenA, RoleEntwickler, "A Entwickler Stand zwei.");
        await session.ReplaceDerivedContentAsync(
            basis.KnotenB, RoleEntwickler, "B abgeleitet von A zwei.",
            [new ContentDependencySource(basis.KnotenA, RoleEntwickler, contentAGeuendert.ContentRevisionId)]);
        await session.ReplaceDerivedContentAsync(
            neuerKnoten.NodeId, RoleEntwickler, "D abgeleitet von A zwei.",
            [new ContentDependencySource(basis.KnotenA, RoleEntwickler, contentAGeuendert.ContentRevisionId)]);
        await session.ReplaceIndependentContentAsync(basis.KnotenA, RoleAdmin, "A Admin neu.");

        var committed = await session.CommitAsync(database, "Commit Diff-Aenderungen");
        var changes = new ChangeTransactionIds(
            geaenderterKnoten.NodeId,
            geloeschterKnoten.NodeId,
            neuerKnoten.NodeId,
            contentAGeuendert.ContentRevisionId);
        return (committed.WorkingSnapshotId, changes);
    }

    private static async Task<SnapshotId> CommitFollowUpTransactionAsync(
        SqlTestDatabase database,
        NodeId wurzelNodeId,
        SequentialIdentifierGenerator identifierGenerator)
    {
        await using var session = await WorkingTransactionSession.BeginAsync(
            database, FolgetransactionId, identifierGenerator, "M5.12 Diff-Folgecommit");

        await session.CreateNodeAsync(wurzelNodeId, "E Titel", null, 20);
        var committed = await session.CommitAsync(database, "Commit Diff-Folgetransaction");
        return committed.WorkingSnapshotId;
    }

    private static HistoryService CreateHistoryService(SqlTestDatabase database)
    {
        var policy = new SqlStoragePolicy { CommandTimeoutSeconds = 60 };
        var historyRepositories = new SnapshotReadRepositories(
            new SqlSnapshotRepository(database.ConnectionFactory, policy),
            new SqlTransactionRepository(database.ConnectionFactory, policy),
            new SqlHierarchyRepository(database.ConnectionFactory, policy),
            new SqlContentRepository(database.ConnectionFactory, policy),
            new SqlRoleRepository(database.ConnectionFactory, policy),
            new SqlDependencyRepository(database.ConnectionFactory, policy));
        return new HistoryService(historyRepositories, new RetrievalPolicy
        {
            DefaultPageSize = 20,
            MaximumPageSize = 100,
            SearchPageSize = 10,
            SearchMaximumPageSize = 50,
            SnippetMaximumCharacters = 100
        });
    }

    private static async Task<SnapshotDiff> RequireDiffAsync(Task<Result<SnapshotDiff>> diffTask)
    {
        var result = await diffTask.ConfigureAwait(false);
        Assert.True(result.IsSuccess, result.Message);
        return result.Value!;
    }

    private static void AssertDiffCategoryCounts(
        SnapshotDiff diff,
        int nodes,
        int roles,
        int resolutions,
        int contents,
        int dependencies)
    {
        Assert.Equal(nodes, diff.Nodes.Count);
        Assert.Equal(roles, diff.Audiences.Count);
        Assert.Equal(resolutions, diff.AudienceResolutions.Count);
        Assert.Equal(contents, diff.Contents.Count);
        Assert.Equal(dependencies, diff.Dependencies.Count);
        Assert.Equal(nodes + roles + resolutions + contents + dependencies, diff.TotalCount);
    }

    private static void AssertChangeDiffContent(SnapshotDiff diff, ChangeTransactionIds changes)
    {
        Assert.Equal(changes.GeaenderterKnoten, Assert.Single(diff.Nodes, entry => entry.Kind == DiffChangeKind.Modified).After!.NodeId);
        Assert.Equal(changes.GeloeschterKnoten, Assert.Single(diff.Nodes, entry => entry.Kind == DiffChangeKind.Deleted).Before!.NodeId);
        Assert.Equal(changes.NeuerKnoten, Assert.Single(diff.Nodes, entry => entry.Kind == DiffChangeKind.Added).After!.NodeId);

        Assert.Equal(RoleAdmin, Assert.Single(diff.Audiences, entry => entry.Kind == DiffChangeKind.Added).After!.AudienceId);
        Assert.Equal(RoleEndanwender, Assert.Single(diff.Audiences, entry => entry.Kind == DiffChangeKind.Modified).After!.AudienceId);
        Assert.Equal(RoleProjekt, Assert.Single(diff.Audiences, entry => entry.Kind == DiffChangeKind.Deleted).Before!.AudienceId);

        Assert.Equal(2, diff.AudienceResolutions.Count(entry => entry.Kind == DiffChangeKind.Modified));
        Assert.Equal(2, diff.AudienceResolutions.Count(entry => entry.Kind == DiffChangeKind.Deleted));
        Assert.Equal(RoleAdmin, Assert.Single(diff.AudienceResolutions, entry => entry.Kind == DiffChangeKind.Added).After!.RequestedAudienceId);

        Assert.Equal(2, diff.Contents.Count(entry => entry.Kind == DiffChangeKind.Added));
        Assert.Equal(2, diff.Contents.Count(entry => entry.Kind == DiffChangeKind.Modified));
        Assert.Equal(2, diff.Contents.Count(entry => entry.Kind == DiffChangeKind.Deleted));

        Assert.Equal(1, diff.Dependencies.Count(entry => entry.Kind == DiffChangeKind.Added));
        Assert.Equal(changes.NeueRevision, Assert.Single(diff.Dependencies, entry => entry.Kind == DiffChangeKind.Modified).After!.SourceContentRevisionId);
        Assert.Equal(1, diff.Dependencies.Count(entry => entry.Kind == DiffChangeKind.Deleted));
    }

    private static string ToReportableJson(SnapshotDiff diff) => JsonSerializer.Serialize(DiffSummary(diff));

    private static object DiffSummary(SnapshotDiff diff) => new
    {
        diff.TotalCount,
        Nodes = diff.Nodes.Select(entry =>
            (entry.Kind, entry.Before?.NodeId, entry.After?.NodeId, entry.After?.Title)).ToArray(),
        Roles = diff.Audiences.Select(entry =>
            (entry.Kind, entry.Before?.AudienceId, entry.After?.AudienceId)).ToArray(),
        Resolutions = diff.AudienceResolutions.Select(entry =>
            (entry.Kind, entry.Before?.RequestedAudienceId, entry.Before?.CandidateAudienceId, entry.Before?.Priority,
                entry.After?.RequestedAudienceId, entry.After?.CandidateAudienceId, entry.After?.Priority)).ToArray(),
        Contents = diff.Contents.Select(entry =>
            (entry.Kind, entry.Before?.NodeId, entry.Before?.AudienceId, entry.Before?.ContentRevisionId,
                entry.After?.NodeId, entry.After?.AudienceId, entry.After?.ContentRevisionId)).ToArray(),
        Dependencies = diff.Dependencies.Select(entry =>
            (entry.Kind, entry.Before?.SourceContentRevisionId, entry.After?.SourceContentRevisionId)).ToArray()
    };

    private sealed record BaseSnapshotSeed(
        SnapshotId SnapshotId,
        NodeId WurzelNodeId,
        NodeId KnotenA,
        NodeId KnotenB,
        NodeId KnotenC);

    private sealed record ChangeTransactionIds(
        NodeId GeaenderterKnoten,
        NodeId GeloeschterKnoten,
        NodeId NeuerKnoten,
        ContentRevisionId NeueRevision);

    private sealed class StaticClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
