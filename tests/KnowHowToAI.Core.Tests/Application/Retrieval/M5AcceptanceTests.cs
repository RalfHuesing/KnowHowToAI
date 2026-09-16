using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Retrieval.Export;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Tests.Application.Retrieval;

[Trait("Category", "Unit")]
public sealed class M5AcceptanceTests
{
    private static readonly SnapshotId SnapshotV1 = new(100);
    private static readonly SnapshotId SnapshotV2 = new(200);
    private static readonly RoleId RoleDefault = new("default");
    private static readonly RoleId RoleConsultant = new("consultant");

    private static readonly NodeId RootId = new(Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly NodeId Child1Id = new(Guid.Parse("10000000-0000-0000-0000-000000000002"));
    private static readonly NodeId Child2Id = new(Guid.Parse("10000000-0000-0000-0000-000000000003"));
    private static readonly NodeId NewChild3Id = new(Guid.Parse("10000000-0000-0000-0000-000000000004"));

    private static readonly DateTimeOffset FixedNow = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HistoricalReproduction_ReleasePreservesState_IndependentOfLaterSnapshots()
    {
        var harness = new MultiSnapshotTestHarness();

        // 1. Snapshot V1 aufbauen
        harness.AddSnapshot(new Snapshot(SnapshotV1, null, SnapshotState.Committed, FixedNow, FixedNow));
        harness.AddRole(new Role(SnapshotV1, RoleDefault, "Default", null, false));
        harness.AddRoleResolution(new RoleResolution(SnapshotV1, RoleDefault, RoleDefault, 1));
        harness.AddNode(new Node(SnapshotV1, RootId, null, "Kapitel 1", null, 1, false));
        harness.AddNode(new Node(SnapshotV1, Child1Id, RootId, "Abschnitt 1.1", null, 1, false));
        harness.AddNode(new Node(SnapshotV1, Child2Id, RootId, "Abschnitt 1.2", null, 2, false));
        harness.AddContent(new NodeContent(SnapshotV1, RootId, RoleDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Root Text V1.", false));
        harness.AddContent(new NodeContent(SnapshotV1, Child1Id, RoleDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Child 1 Text V1.", false));
        harness.AddContent(new NodeContent(SnapshotV1, Child2Id, RoleDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Child 2 Text V1.", false));

        // Release v1.0 auf Snapshot 1 anlegen
        var releaseService = harness.CreateReleaseService();
        var r1Result = await releaseService.CreateReleaseAsync("v1.0", SnapshotV1, "Erster Stand");
        Assert.True(r1Result.IsSuccess);

        // 2. Snapshot V2 aufbauen mit Mutationen:
        // - NewChild3 hinzugefügt (create)
        // - Child2 unter Child1 verschoben (move)
        // - Child1 Content aktualisiert (update)
        // - Rolle Consultant hinzugefügt
        // - Resolution Order geändert
        harness.AddSnapshot(new Snapshot(SnapshotV2, SnapshotV1, SnapshotState.Committed, FixedNow.AddDays(1), FixedNow.AddDays(1)));
        harness.AddRole(new Role(SnapshotV2, RoleDefault, "Default", null, false));
        harness.AddRole(new Role(SnapshotV2, RoleConsultant, "Consultant", null, false));
        harness.AddRoleResolution(new RoleResolution(SnapshotV2, RoleDefault, RoleConsultant, 1));
        harness.AddRoleResolution(new RoleResolution(SnapshotV2, RoleDefault, RoleDefault, 2));

        harness.AddNode(new Node(SnapshotV2, RootId, null, "Kapitel 1", null, 1, false));
        harness.AddNode(new Node(SnapshotV2, Child1Id, RootId, "Abschnitt 1.1", null, 1, false));
        harness.AddNode(new Node(SnapshotV2, Child2Id, Child1Id, "Abschnitt 1.2 (Moved)", null, 1, false)); // Move nach Child1!
        harness.AddNode(new Node(SnapshotV2, NewChild3Id, RootId, "Abschnitt 1.3 (New)", null, 2, false)); // Neu!

        harness.AddContent(new NodeContent(SnapshotV2, RootId, RoleDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Root Text V1.", false));
        harness.AddContent(new NodeContent(SnapshotV2, Child1Id, RoleDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Child 1 Text V2 Aktualisiert.", false));
        harness.AddContent(new NodeContent(SnapshotV2, Child2Id, RoleDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Child 2 Text V1.", false));
        harness.AddContent(new NodeContent(SnapshotV2, NewChild3Id, RoleDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Child 3 Text V2 Neu.", false));

        // Release v2.0 auf Snapshot 2 anlegen
        var r2Result = await releaseService.CreateReleaseAsync("v2.0", SnapshotV2, "Zweiter Stand");
        Assert.True(r2Result.IsSuccess);

        // 3. Historische Reproduktion: Export auf Release v1.0 (Snapshot 1) liefert unberührt den exakten Stand von damals
        var exportService = harness.CreateExportService();
        var exportV1 = await exportService.ExportTreeAsync(RootId, new ReadContext(SnapshotId: SnapshotV1), RoleDefault);
        Assert.True(exportV1.IsSuccess);
        var expectedV1 = "# Kapitel 1\n\nRoot Text V1.\n\n## Abschnitt 1.1\n\nChild 1 Text V1.\n\n## Abschnitt 1.2\n\nChild 2 Text V1.\n";
        Assert.Equal(expectedV1, exportV1.Value);

        // 4. Export auf Release v2.0 (Snapshot 2) liefert die neue Struktur mit Verschiebung und neuem Node
        var exportV2 = await exportService.ExportTreeAsync(RootId, new ReadContext(SnapshotId: SnapshotV2), RoleDefault);
        Assert.True(exportV2.IsSuccess);
        var expectedV2 = "# Kapitel 1\n\nRoot Text V1.\n\n## Abschnitt 1.1\n\nChild 1 Text V2 Aktualisiert.\n\n### Abschnitt 1.2 (Moved)\n\nChild 2 Text V1.\n\n## Abschnitt 1.3 (New)\n\nChild 3 Text V2 Neu.\n";
        Assert.Equal(expectedV2, exportV2.Value);

        // 5. Netto-Diff zwischen v1.0 und v2.0 belegt alle Änderungen präzise
        var historyService = harness.CreateHistoryService();
        var diffResult = await historyService.CompareSnapshotsAsync(SnapshotV1, SnapshotV2);
        Assert.True(diffResult.IsSuccess);
        var diff = diffResult.Value!;

        // Node Änderungen
        Assert.Contains(diff.Nodes, n => n.Kind == DiffChangeKind.Added && n.After!.NodeId == NewChild3Id);
        var movedNode = Assert.Single(diff.Nodes, n => n.Kind == DiffChangeKind.Modified && n.After!.NodeId == Child2Id);
        Assert.Equal(RootId, movedNode.Before!.ParentNodeId);
        Assert.Equal(Child1Id, movedNode.After!.ParentNodeId);

        // Content Änderung
        var updatedContent = Assert.Single(diff.Contents, c => c.Kind == DiffChangeKind.Modified && c.After!.NodeId == Child1Id);
        Assert.Equal("Child 1 Text V1.", updatedContent.Before!.ContentMd);
        Assert.Equal("Child 1 Text V2 Aktualisiert.", updatedContent.After!.ContentMd);

        // Rollenänderung
        Assert.Contains(diff.Roles, r => r.Kind == DiffChangeKind.Added && r.After!.RoleId == RoleConsultant);
    }

    [Fact]
    public async Task AcceptanceCriterion_NoToolReturnsUnboundedDataSet_ExceptExport()
    {
        var harness = new MultiSnapshotTestHarness();
        harness.AddSnapshot(new Snapshot(SnapshotV1, null, SnapshotState.Committed, FixedNow, FixedNow));
        harness.AddRole(new Role(SnapshotV1, RoleDefault, "Default", null, false));
        harness.AddRoleResolution(new RoleResolution(SnapshotV1, RoleDefault, RoleDefault, 1));
        harness.AddNode(new Node(SnapshotV1, RootId, null, "Root", null, 0, false));

        // 25 Kindknoten anlegen
        for (var i = 1; i <= 25; i++)
        {
            var cid = new NodeId(Guid.Parse($"20000000-0000-0000-0000-{i:D12}"));
            harness.AddNode(new Node(SnapshotV1, cid, RootId, $"Child {i}", null, i, false));
            harness.AddContent(new NodeContent(SnapshotV1, cid, RoleDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, $"Inhalt {i}", false));
        }

        // Policy mit Default=5, Max=10
        var policy = new RetrievalPolicy
        {
            DefaultPageSize = 5,
            MaximumPageSize = 10,
            SearchPageSize = 5,
            SearchMaximumPageSize = 10,
            SnippetMaximumCharacters = 100
        };

        // 1. Navigation ListChildrenAsync beachtet MaximumPageSize
        var navService = harness.CreateNavigationService(policy);
        var navResult = await navService.ListChildrenAsync(
            new ListChildrenQuery(RootId, new ReadContext(SnapshotId: SnapshotV1), RoleDefault, Limit: 9999));
        Assert.True(navResult.IsSuccess);
        Assert.NotNull(navResult.Value);
        Assert.Equal(10, navResult.Value.Items.Count); // Begrenzt auf MaximumPageSize = 10!
        Assert.NotNull(navResult.Value.NextCursor);

        // 2. History CompareSnapshotsAsync beachtet MaximumPageSize
        var snapshotBase = new SnapshotId(50);
        harness.AddSnapshot(new Snapshot(snapshotBase, null, SnapshotState.Committed, FixedNow.AddDays(-1), FixedNow.AddDays(-1)));
        var historyService = harness.CreateHistoryService(policy);
        var diffResult = await historyService.CompareSnapshotsAsync(snapshotBase, SnapshotV1, limit: 9999);
        Assert.True(diffResult.IsSuccess);
        Assert.NotNull(diffResult.Value);
        // Insgesamt 1 Role + 1 Resolution + 26 Nodes + 25 Contents = 53 Items
        var totalDiffItemsOnPage = diffResult.Value.Roles.Count + diffResult.Value.RoleResolutions.Count + diffResult.Value.Nodes.Count + diffResult.Value.Contents.Count + diffResult.Value.Dependencies.Count;
        Assert.Equal(10, totalDiffItemsOnPage); // Begrenzt auf MaximumPageSize = 10!
        Assert.NotNull(diffResult.Value.NextCursor);

        // 3. Release ListReleasesAsync beachtet MaximumPageSize
        var releaseService = harness.CreateReleaseService(policy);
        for (var r = 1; r <= 20; r++)
        {
            harness.ReleaseRepo.ExistingReleases.Add(new Release(new ReleaseId(r), SnapshotV1, $"v{r}.0", null, FixedNow.AddMinutes(r)));
        }
        var releaseListResult = await releaseService.ListReleasesAsync(limit: 9999, cursor: null);
        Assert.True(releaseListResult.IsSuccess);
        Assert.Equal(10, releaseListResult.Value!.Items.Count); // Begrenzt auf MaximumPageSize = 10!
        Assert.NotNull(releaseListResult.Value.NextCursor);

        // 4. ExportTreeAsync liefert den vollständigen Baum (die ausdrücklich angeforderte Ausnahme!)
        var exportService = harness.CreateExportService();
        var exportResult = await exportService.ExportTreeAsync(RootId, new ReadContext(SnapshotId: SnapshotV1), RoleDefault);
        Assert.True(exportResult.IsSuccess);
        // Enthält alle 25 Child-Überschriften
        for (var i = 1; i <= 25; i++)
        {
            Assert.Contains($"## Child {i}", exportResult.Value!);
        }
    }

    private sealed class MultiSnapshotTestHarness
    {
        public List<Snapshot> Snapshots { get; } = [];
        public List<Node> Nodes { get; } = [];
        public List<Role> Roles { get; } = [];
        public List<RoleResolution> Resolutions { get; } = [];
        public List<NodeContent> Contents { get; } = [];
        public List<ContentDependency> Dependencies { get; } = [];
        public InMemoryReleaseRepository ReleaseRepo { get; } = new();

        public void AddSnapshot(Snapshot s) => Snapshots.Add(s);
        public void AddNode(Node n) => Nodes.Add(n);
        public void AddRole(Role r) => Roles.Add(r);
        public void AddRoleResolution(RoleResolution res) => Resolutions.Add(res);
        public void AddContent(NodeContent c) => Contents.Add(c);

        public SnapshotReadRepositories CreateSnapshotReadRepositories() => new(
            new DelegatingSnapshotRepo(Snapshots),
            new ThrowingTxRepo(),
            new DelegatingHierarchyRepo(Nodes),
            new DelegatingContentRepo(Contents),
            new DelegatingRoleRepo(Roles, Resolutions),
            new DelegatingDepRepo(Dependencies));

        public MarkdownExportService CreateExportService() => new(CreateSnapshotReadRepositories());

        public HistoryService CreateHistoryService(RetrievalPolicy? policy = null) =>
            new(CreateSnapshotReadRepositories(), policy ?? StandardPolicy());

        public ReleaseService CreateReleaseService(RetrievalPolicy? policy = null) => new(
            CreateSnapshotReadRepositories(),
            ReleaseRepo,
            new FrozenClock(FixedNow),
            policy ?? StandardPolicy(),
            new ValidationPolicy
            {
                ContentSizeWarningBytes = 4096,
                ChildCountWarning = 50,
                HierarchyDepthWarning = 10,
                PossibleEmbeddedHeadingWarning = true
            });

        public NavigationService CreateNavigationService(RetrievalPolicy? policy = null)
        {
            var repos = CreateSnapshotReadRepositories();
            var navRepos = new SnapshotReadRepositories(
                repos.Snapshots,
                repos.Transactions,
                repos.Hierarchy,
                repos.Contents,
                repos.Roles,
                repos.Dependencies);
            return new NavigationService(navRepos, policy ?? StandardPolicy());
        }

        private static RetrievalPolicy StandardPolicy() => new()
        {
            DefaultPageSize = 10,
            MaximumPageSize = 50,
            SearchPageSize = 10,
            SearchMaximumPageSize = 50,
            SnippetMaximumCharacters = 100
        };
    }

    private sealed class InMemoryReleaseRepository : IReleaseMutationRepository
    {
        public List<Release> ExistingReleases { get; } = [];

        public Task<Result<Release>> CreateAsync(CreateReleaseRecord request, CancellationToken cancellationToken = default)
        {
            var rel = new Release(new ReleaseId(ExistingReleases.Count + 1), request.SnapshotId, request.Name, request.Description, request.CreatedAtUtc);
            ExistingReleases.Add(rel);
            return Task.FromResult(Result<Release>.Success(rel));
        }

        public Task<IReadOnlyList<Release>> ListAsync(int limit, long? afterReleaseId, CancellationToken cancellationToken = default)
        {
            var query = ExistingReleases.AsEnumerable();
            if (afterReleaseId.HasValue)
                query = query.Where(r => r.ReleaseId.Value > afterReleaseId.Value);

            var items = query.OrderBy(r => r.ReleaseId.Value).Take(limit).ToArray();
            return Task.FromResult<IReadOnlyList<Release>>(items);
        }
    }

    private sealed class FrozenClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class DelegatingSnapshotRepo(List<Snapshot> snapshots) : ISnapshotRepository
    {
        public Task<Snapshot?> FindAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshots.FirstOrDefault(s => s.SnapshotId == snapshotId));

        public Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshots.First(s => s.State == SnapshotState.Committed));
    }

    private sealed class ThrowingTxRepo : ITransactionRepository
    {
        public Task<KnowledgeTransaction> BeginAsync(KnowHowToAI.Core.Application.Transactions.BeginTransactionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KnowHowToAI.Core.Application.Transactions.CommitTransactionResult> CommitAsync(KnowHowToAI.Core.Application.Transactions.CommitTransactionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DelegatingHierarchyRepo(List<Node> nodes) : IHierarchyRepository
    {
        public Task<IReadOnlyList<Node>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Node>>(nodes.Where(n => n.SnapshotId == snapshotId).ToArray());
    }

    private sealed class DelegatingRoleRepo(List<Role> roles, List<RoleResolution> resolutions) : IRoleRepository
    {
        public Task<IReadOnlyList<Role>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Role>>(roles.Where(r => r.SnapshotId == snapshotId).ToArray());

        public Task<IReadOnlyList<RoleResolution>> ListResolutionsBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoleResolution>>(resolutions.Where(r => r.SnapshotId == snapshotId).ToArray());
    }

    private sealed class DelegatingContentRepo(List<NodeContent> contents) : IContentRepository
    {
        public Task<IReadOnlyList<NodeContent>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NodeContent>>(contents.Where(c => c.SnapshotId == snapshotId).ToArray());
    }

    private sealed class DelegatingDepRepo(List<ContentDependency> dependencies) : IDependencyRepository
    {
        public Task<IReadOnlyList<ContentDependency>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ContentDependency>>(dependencies.Where(d => d.SnapshotId == snapshotId).ToArray());
    }
}
