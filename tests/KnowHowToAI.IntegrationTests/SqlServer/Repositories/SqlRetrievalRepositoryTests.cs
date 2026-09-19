using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.Storage.SqlServer.Repositories.Retrieval;
using KnowHowToAI.Storage.SqlServer.Repositories.Snapshots;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;

namespace KnowHowToAI.IntegrationTests.SqlServer.Repositories;

[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed class SqlRetrievalRepositoryTests
{
    private static readonly RoleId RoleDev = new("Developer");
    private static readonly RoleId RoleConsultant = new("Consultant");

    [Fact]
    public async Task SearchAsync_TitleHit_ReturnsRank1WithNullSnippet()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var (snapshotId, node) = await SeedCommittedSnapshotAsync(
            database, session => session.CreateNodeAsync(null, "SQL Server Installation", "Guide", 0));

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var request = new SearchRequest(snapshotId, "Installation", null, 10, null, 100);

        var hits = (await repository.SearchAsync(request)).Value!;

        var hit = Assert.Single(hits);
        Assert.Equal(node.NodeId, hit.NodeId);
        Assert.Equal("Title", hit.HitField);
        Assert.Null(hit.Snippet);
    }

    [Fact]
    public async Task SearchAsync_WildcardEscaping_FindsExactSpecialCharactersWithoutTreatingThemAsWildcard()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var (snapshotId, nodes) = await SeedCommittedSnapshotAsync(database, async session =>
        {
            var percentNode = await session.CreateNodeAsync(null, "Save 100% money", "Promo", 1);
            var thousandNode = await session.CreateNodeAsync(percentNode.NodeId, "Save 1000 money", "Promo", 2);
            return (PercentNode: percentNode, ThousandNode: thousandNode);
        });

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var request = new SearchRequest(snapshotId, "100%", null, 10, null, 100);

        var hits = (await repository.SearchAsync(request)).Value!;

        var hit = Assert.Single(hits);
        Assert.Equal(nodes.PercentNode.NodeId, hit.NodeId);
        Assert.Equal("Save 100% money", hit.Title);
    }

    [Fact]
    public async Task SearchAsync_DescriptionHit_ReturnsRank2WithSnippet()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var (snapshotId, node) = await SeedCommittedSnapshotAsync(database, session =>
            session.CreateNodeAsync(null, "Overview", "Contains details about database clustering options", 0));

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var request = new SearchRequest(snapshotId, "clustering", null, 10, null, 50);

        var hits = (await repository.SearchAsync(request)).Value!;

        var hit = Assert.Single(hits);
        Assert.Equal(node.NodeId, hit.NodeId);
        Assert.Equal("Description", hit.HitField);
        Assert.NotNull(hit.Snippet);
        Assert.Contains("clustering", hit.Snippet);
    }

    [Fact]
    public async Task SearchAsync_ContentHitWithRoleFallback_ReturnsRank3WithSnippetAndFallbackAvailability()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var (snapshotId, node) = await SeedCommittedSnapshotAsync(database, async session =>
        {
            await session.CreateRoleAsync("Developer", null);
            await session.CreateRoleAsync("Consultant", null);
            await session.SetResolutionAsync(RoleConsultant, RoleDev);
            var created = await session.CreateNodeAsync(null, "Node Title", "Node Description", 0);
            await session.ReplaceIndependentContentAsync(
                created.NodeId, RoleDev, "Deep architectural secrets of the engine");
            return created;
        });

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var request = new SearchRequest(snapshotId, "architectural", RoleConsultant, 10, null, 50);

        var hits = (await repository.SearchAsync(request)).Value!;

        var hit = Assert.Single(hits);
        Assert.Equal(node.NodeId, hit.NodeId);
        Assert.Equal("Content", hit.HitField);
        Assert.Equal(Availability.Fallback, hit.Availability);
        Assert.Equal(RoleDev, hit.ResolvedRoleId);
        Assert.NotNull(hit.Snippet);
        Assert.Contains("architectural", hit.Snippet);
    }

    [Fact]
    public async Task SearchAsync_FilteredFacets_AreAppliedBeforeKeysetPaging()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        await using var session = await WorkingTransactionSession.BeginAsync(
            database, new TransactionId(Guid.NewGuid()), new SequentialIdentifierGenerator(), "Filtertest");
        await session.CreateRoleAsync(RoleDev.Value, null);
        await session.SetResolutionAsync(RoleDev, RoleDev);
        var source = await session.CreateNodeAsync(null, "Quelle", null, 1);
        var target = await session.CreateNodeAsync(source.NodeId, "Gefilterter Treffer", null, 2);
        var secondTarget = await session.CreateNodeAsync(source.NodeId, "Gefilterter zweiter Treffer", null, 3);
        var sourceContent = await session.ReplaceIndependentContentAsync(source.NodeId, RoleDev, "Aktueller Quellinhalt");
        await session.ReplaceDerivedContentAsync(
            target.NodeId,
            RoleDev,
            "Gefilterter Inhalt",
            [new ContentDependencySource(source.NodeId, RoleDev, sourceContent.ContentRevisionId)]);
        await session.ReplaceDerivedContentAsync(
            secondTarget.NodeId,
            RoleDev,
            "Gefilterter zweiter Inhalt",
            [new ContentDependencySource(source.NodeId, RoleDev, sourceContent.ContentRevisionId)]);
        await session.ReplaceIndependentContentAsync(source.NodeId, RoleDev, "Neuere Quellrevision");
        var committed = await session.CommitAsync(database, "Filtertest committen");

        var filter = new SearchFilter(
            [RoleDev, new RoleId("AndereRolle")],
            [Availability.Explicit, Availability.Fallback],
            [Freshness.Stale],
            ["StaleDerivedContent"]);
        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var result = await repository.SearchAsync(new SearchRequest(
            committed.WorkingSnapshotId, "Gefilterter", RoleDev, 1, null, 100, Filter: filter));

        Assert.True(result.IsSuccess);
        var hit = Assert.Single(result.Value!);
        Assert.Equal(target.NodeId, hit.NodeId);
        Assert.Equal(Freshness.Stale, hit.Freshness);
        Assert.Equal(["StaleDerivedContent"], hit.Findings);

        var cursor = new SearchCursor(
            committed.WorkingSnapshotId,
            null,
            "Gefilterter",
            RoleDev,
            1,
            hit.SortOrder,
            hit.NodeId,
            filter.Fingerprint).Encode();
        var nextPage = await repository.SearchAsync(new SearchRequest(
            committed.WorkingSnapshotId, "Gefilterter", RoleDev, 1, cursor, 100, Filter: filter));
        Assert.Equal(secondTarget.NodeId, Assert.Single(nextPage.Value!).NodeId);
    }

    [Fact]
    public async Task SearchAsync_RankingAndKeysetPaging_OrdersCorrectlyAndPages()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var (snapshotId, nodes) = await SeedCommittedSnapshotAsync(database, async session =>
        {
            await session.CreateRoleAsync("Developer", null);
            await session.SetResolutionAsync(RoleDev, RoleDev);
            var titleNode = await session.CreateNodeAsync(null, "Alpha match in title", "Other text", 10);
            var descNode = await session.CreateNodeAsync(titleNode.NodeId, "Beta title", "Alpha match in description", 20);
            var contentNode = await session.CreateNodeAsync(titleNode.NodeId, "Gamma title", "Gamma desc", 30);
            await session.ReplaceIndependentContentAsync(contentNode.NodeId, RoleDev, "Alpha match in content body");
            return (TitleNode: titleNode, DescNode: descNode, ContentNode: contentNode);
        });

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

        // Page 1: limit 2
        var page1 = (await repository.SearchAsync(new SearchRequest(snapshotId, "Alpha", RoleDev, 2, null, 100))).Value!;
        Assert.Equal(2, page1.Count);
        Assert.Equal(nodes.TitleNode.NodeId, page1[0].NodeId);
        Assert.Equal("Title", page1[0].HitField);
        Assert.Equal(nodes.DescNode.NodeId, page1[1].NodeId);
        Assert.Equal("Description", page1[1].HitField);

        // Page 2 using cursor
        var cursor = new SearchCursor(snapshotId, null, "Alpha", RoleDev, 2, page1[1].SortOrder, page1[1].NodeId).Encode();
        var page2 = (await repository.SearchAsync(new SearchRequest(snapshotId, "Alpha", RoleDev, 2, cursor, 100))).Value!;
        var hit = Assert.Single(page2);
        Assert.Equal(nodes.ContentNode.NodeId, hit.NodeId);
        Assert.Equal("Content", hit.HitField);
    }

    [Fact]
    public async Task SearchAsync_WithRoleExplicitContentHit_ReturnsExplicitAvailabilityAndRoleData()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var (snapshotId, _) = await SeedCommittedSnapshotAsync(database, async session =>
        {
            await session.CreateRoleAsync("Developer", null);
            await session.SetResolutionAsync(RoleDev, RoleDev);
            var created = await session.CreateNodeAsync(null, "Node Title", "Node Description", 0);
            await session.ReplaceIndependentContentAsync(created.NodeId, RoleDev, "Explicit developer content");
            return created;
        });

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var result = (await repository.SearchAsync(new SearchRequest(snapshotId, "developer", RoleDev, 10, null, 50))).Value!;

        var hit = Assert.Single(result);
        Assert.Equal("Content", hit.HitField);
        Assert.Equal(Availability.Explicit, hit.Availability);
        Assert.Equal(RoleDev, hit.ResolvedRoleId);
        Assert.Contains(result.Roles!, role => role.RoleId == RoleDev && !role.IsDeleted);
        Assert.Contains(result.Resolutions!, resolution => resolution.RequestedRoleId == RoleDev);
    }

    [Fact]
    public async Task SearchAsync_WithoutRole_DoesNotSearchContent()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var (snapshotId, _) = await SeedCommittedSnapshotAsync(database, async session =>
        {
            var created = await session.CreateNodeAsync(null, "Overview", "Describes clustering options", 0);
            await session.ReplaceIndependentContentAsync(
                created.NodeId, new RoleId("Default"), "Content about clustering internals");
            return created;
        });

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

        var contentOnlyResult = (await repository.SearchAsync(new SearchRequest(snapshotId, "internals", null, 10, null, 50))).Value!;
        Assert.Empty(contentOnlyResult);

        var titleResult = (await repository.SearchAsync(new SearchRequest(snapshotId, "Overview", null, 10, null, 50))).Value!;
        var hit = Assert.Single(titleResult);
        Assert.Equal("Title", hit.HitField);
        Assert.Equal(Availability.None, hit.Availability);
        Assert.Null(hit.ResolvedRoleId);
        Assert.Null(hit.Snippet);
    }

    [Fact]
    public async Task SearchAsync_WithClosedTransaction_ReturnsStableErrorInsteadOfThrowing()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var transactionRepository = new SqlTransactionRepository(
            database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var transaction = await transactionRepository.BeginAsync(new BeginTransactionRequest(
            new TransactionId(Guid.NewGuid()), null, null, "xUnit"));
        var discardResult = await transactionRepository.DiscardAsync(transaction.TransactionId);
        Assert.True(discardResult.IsSuccess);

        var repository = new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });

        var result = await repository.SearchAsync(new SearchRequest(
            transaction.WorkingSnapshotId, "text", null, 10, null, 50, transaction.TransactionId));

        Assert.False(result.IsSuccess);
        Assert.Equal(SearchErrorCodes.TransactionClosed, result.Error!.Code);
        Assert.Equal(
            transaction.TransactionId.ToString(),
            result.Error.Details[SearchErrorCodes.TransactionIdDetail]);
    }

    [Fact]
    public async Task Search_WithUnknownRequestedRole_ReturnsRequestedRoleNotFound()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var service = CreateSearchService(database);
        var result = await service.SearchAsync(new SearchQuery("text", RoleId: new RoleId("Missing")), new ReadContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleNotFound, result.Error!.Code);
        Assert.Equal(
            "Missing",
            result.Error.Details[RoleResolutionErrorCodes.RequestedRoleIdDetail]);
    }

    [Fact]
    public async Task Search_WithDeletedRequestedRole_ReturnsRequestedRoleDeleted()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var (snapshotId, _) = await SeedCommittedSnapshotAsync(database, async session =>
        {
            await session.CreateRoleAsync("Ghost", null);
            await session.DeleteRoleAsync(new RoleId("Ghost"));
            return true;
        });

        var service = CreateSearchService(database);
        var result = await service.SearchAsync(new SearchQuery("text", RoleId: new RoleId("Ghost")), new ReadContext());

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleDeleted, result.Error!.Code);
    }

    [Fact]
    public async Task Search_WithDeletedCandidateRole_ReturnsCandidateRoleDeleted()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        // Die Kombination "Resolution auf gelöschten Kandidaten" ist fachlich unzulässig und
        // damit über Mutationen unerreichbar (DeleteRole-Referenzschutz und Commit-Validierung).
        // Für die Fehlerpfad-Abnahme wird der Tombstone deshalb im offenen Working Snapshot
        // erzeugt und die Transaction bewusst nicht committet.
        var transactionId = new TransactionId(Guid.NewGuid());
        await using var session = await WorkingTransactionSession.BeginAsync(
            database, transactionId, new SequentialIdentifierGenerator(), "Repo-Test");
        await session.CreateRoleAsync("Ghost", null);
        await session.SetResolutionAsync(new RoleId("Default"), new RoleId("Ghost"));
        await TombstoneRoleAsync(database, transactionId, new RoleId("Ghost"));

        var service = CreateSearchService(database);
        var result = await service.SearchAsync(
            new SearchQuery("text", RoleId: new RoleId("Default")), new ReadContext(transactionId));

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.CandidateRoleDeleted, result.Error!.Code);
    }

    [Fact]
    public async Task Search_WithoutConfiguredResolutionOrder_ReturnsSuccessWithoutContentHits()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var (snapshotId, _) = await SeedCommittedSnapshotAsync(database, async session =>
        {
            var roleWithoutOrder = new RoleId("NoOrder");
            await session.CreateRoleAsync("NoOrder", null);
            var created = await session.CreateNodeAsync(null, "Neuland Overview", null, 0);
            await session.ReplaceIndependentContentAsync(
                created.NodeId, roleWithoutOrder, "Content about clustering internals");
            return created;
        });

        var service = CreateSearchService(database);
        var contentResult = await service.SearchAsync(new SearchQuery("internals", RoleId: new RoleId("NoOrder")), new ReadContext());
        Assert.True(contentResult.IsSuccess);
        Assert.Empty(contentResult.Value!.Items);

        var titleResult = await service.SearchAsync(new SearchQuery("Neuland", RoleId: new RoleId("NoOrder")), new ReadContext());
        Assert.True(titleResult.IsSuccess);
        Assert.Single(titleResult.Value!.Items);
    }

    [Fact]
    public async Task Search_WithFallbackAndExplicitContent_ResolvesFirstCandidateContent()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var (snapshotId, nodes) = await SeedCommittedSnapshotAsync(database, async session =>
        {
            var defaultRole = new RoleId("Default");
            await session.CreateRoleAsync("Developer", null);
            await session.SetResolutionAsync(RoleDev, defaultRole, RoleDev);
            var fallbackNode = await session.CreateNodeAsync(null, "Fallback Node", null, 10);
            var explicitNode = await session.CreateNodeAsync(fallbackNode.NodeId, "Explicit Node", null, 20);
            await session.ReplaceIndependentContentAsync(fallbackNode.NodeId, defaultRole, "Shared content marker");
            await session.ReplaceIndependentContentAsync(explicitNode.NodeId, RoleDev, "Shared content marker");
            return (FallbackNode: fallbackNode, ExplicitNode: explicitNode);
        });

        var service = CreateSearchService(database);
        var result = await service.SearchAsync(new SearchQuery("marker", RoleId: RoleDev), new ReadContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        var fallbackHit = Assert.Single(result.Value.Items, hit => hit.NodeId == nodes.FallbackNode.NodeId);
        Assert.Equal("Content", fallbackHit.HitField);
        Assert.Equal(Availability.Fallback, fallbackHit.Availability);
        Assert.Equal(new RoleId("Default"), fallbackHit.ResolvedRoleId);
        var explicitHit = Assert.Single(result.Value.Items, hit => hit.NodeId == nodes.ExplicitNode.NodeId);
        Assert.Equal(Availability.Explicit, explicitHit.Availability);
        Assert.Equal(RoleDev, explicitHit.ResolvedRoleId);
    }

    private static SearchService CreateSearchService(SqlTestDatabase database) => new(
        new SearchRepositories(
            new SqlSnapshotRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 }),
            new SqlTransactionRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 }),
            new SqlRetrievalRepository(database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 })),
        new RetrievalPolicy
        {
            DefaultPageSize = 10,
            MaximumPageSize = 100,
            SearchPageSize = 10,
            SearchMaximumPageSize = 100,
            SnippetMaximumCharacters = 100
        });

    /// <summary>
    /// Seedt ausschließlich über den produktiven Mutations-Pfad: offene Working Transaction
    /// via <see cref="WorkingTransactionSession"/> und Commit; der Committierte Snapshot wird
    /// nie direkt per SQL verändert.
    /// </summary>
    private static async Task<(SnapshotId SnapshotId, T Value)> SeedCommittedSnapshotAsync<T>(
        SqlTestDatabase database,
        Func<WorkingTransactionSession, Task<T>> seed)
    {
        await using var session = await WorkingTransactionSession.BeginAsync(
            database, new TransactionId(Guid.NewGuid()), new SequentialIdentifierGenerator(), "Repo-Test");
        var value = await seed(session);
        var committed = await session.CommitAsync(database, "Commit des Repo-Test-Seedings");
        return (committed.WorkingSnapshotId, value);
    }

    private static async Task TombstoneRoleAsync(SqlTestDatabase database, TransactionId transactionId, RoleId roleId)
    {
        var repository = new SqlRoleMutationRepository(
            database.ConnectionFactory, new SqlStoragePolicy { CommandTimeoutSeconds = 30 });
        var result = await repository.ExecuteAsync(transactionId, state =>
        {
            var deleted = state.Roles.Single(candidate => candidate.RoleId == roleId) with { IsDeleted = true };
            var roles = state.Roles.Select(candidate => candidate.RoleId == roleId ? deleted : candidate).ToArray();
            return Result<WorkingRoleMutationDecision<Role>>.Success(
                new WorkingRoleMutationDecision<Role>(deleted, state with { Roles = roles }));
        });
        Assert.True(result.IsSuccess);
    }
}
