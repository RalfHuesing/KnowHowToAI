using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Retrieval.Export;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.Retrieval;

[Trait("Category", "Unit")]
public sealed class M5AcceptanceTests
{
    private static readonly SnapshotId SnapshotV1 = new(100);
    private static readonly SnapshotId SnapshotV2 = new(200);
    private static readonly AudienceId AudienceDefault = new("default");
    private static readonly AudienceId AudienceConsultant = new("consultant");

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
        harness.AddAudience(new Audience(SnapshotV1, AudienceDefault, "Default", null, false));
        harness.AddAudienceResolution(new AudienceResolution(SnapshotV1, AudienceDefault, AudienceDefault, 1));
        harness.AddNode(new Node(SnapshotV1, RootId, null, "Kapitel 1", null, 1, false));
        harness.AddNode(new Node(SnapshotV1, Child1Id, RootId, "Abschnitt 1.1", null, 1, false));
        harness.AddNode(new Node(SnapshotV1, Child2Id, RootId, "Abschnitt 1.2", null, 2, false));
        harness.AddContent(new NodeContent(SnapshotV1, RootId, AudienceDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Root Text V1.", false));
        harness.AddContent(new NodeContent(SnapshotV1, Child1Id, AudienceDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Child 1 Text V1.", false));
        harness.AddContent(new NodeContent(SnapshotV1, Child2Id, AudienceDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Child 2 Text V1.", false));

        // Release v1.0 auf Snapshot 1 anlegen
        var releaseService = harness.CreateReleaseService();
        var r1Result = await releaseService.CreateReleaseAsync("v1.0", SnapshotV1, "Erster Stand");
        Assert.True(r1Result.IsSuccess);

        // 2. Snapshot V2 aufbauen mit Mutationen:
        // - NewChild3 hinzugefügt (create)
        // - Child2 unter Child1 verschoben (move)
        // - Child1 Content aktualisiert (update)
        // - Zielgruppe Consultant hinzugefügt
        // - Resolution Order geändert
        harness.AddSnapshot(new Snapshot(SnapshotV2, SnapshotV1, SnapshotState.Committed, FixedNow.AddDays(1), FixedNow.AddDays(1)));
        harness.AddAudience(new Audience(SnapshotV2, AudienceDefault, "Default", null, false));
        harness.AddAudience(new Audience(SnapshotV2, AudienceConsultant, "Consultant", null, false));
        harness.AddAudienceResolution(new AudienceResolution(SnapshotV2, AudienceDefault, AudienceConsultant, 1));
        harness.AddAudienceResolution(new AudienceResolution(SnapshotV2, AudienceDefault, AudienceDefault, 2));

        harness.AddNode(new Node(SnapshotV2, RootId, null, "Kapitel 1", null, 1, false));
        harness.AddNode(new Node(SnapshotV2, Child1Id, RootId, "Abschnitt 1.1", null, 1, false));
        harness.AddNode(new Node(SnapshotV2, Child2Id, Child1Id, "Abschnitt 1.2 (Moved)", null, 1, false)); // Move nach Child1!
        harness.AddNode(new Node(SnapshotV2, NewChild3Id, RootId, "Abschnitt 1.3 (New)", null, 2, false)); // Neu!

        harness.AddContent(new NodeContent(SnapshotV2, RootId, AudienceDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Root Text V1.", false));
        harness.AddContent(new NodeContent(SnapshotV2, Child1Id, AudienceDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Child 1 Text V2 Aktualisiert.", false));
        harness.AddContent(new NodeContent(SnapshotV2, Child2Id, AudienceDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Child 2 Text V1.", false));
        harness.AddContent(new NodeContent(SnapshotV2, NewChild3Id, AudienceDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Child 3 Text V2 Neu.", false));

        // Release v2.0 auf Snapshot 2 anlegen
        var r2Result = await releaseService.CreateReleaseAsync("v2.0", SnapshotV2, "Zweiter Stand");
        Assert.True(r2Result.IsSuccess);

        // 3. Historische Reproduktion: Export auf Release v1.0 (Snapshot 1) liefert unberührt den exakten Stand von damals
        var exportService = harness.CreateExportService();
        var exportV1 = await exportService.ExportTreeAsync(RootId, new ReadContext(SnapshotId: SnapshotV1), AudienceDefault);
        Assert.True(exportV1.IsSuccess);
        var expectedV1 = "# Kapitel 1\n\nRoot Text V1.\n\n## Abschnitt 1.1\n\nChild 1 Text V1.\n\n## Abschnitt 1.2\n\nChild 2 Text V1.\n";
        Assert.Equal(expectedV1, exportV1.Value);

        // 4. Export auf Release v2.0 (Snapshot 2) liefert die neue Struktur mit Verschiebung und neuem Node
        var exportV2 = await exportService.ExportTreeAsync(RootId, new ReadContext(SnapshotId: SnapshotV2), AudienceDefault);
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
        Assert.Contains(diff.Audiences, r => r.Kind == DiffChangeKind.Added && r.After!.AudienceId == AudienceConsultant);
    }

    [Fact]
    public async Task AcceptanceCriterion_NoToolReturnsUnboundedDataSet_ExceptExport()
    {
        var harness = new MultiSnapshotTestHarness();
        harness.AddSnapshot(new Snapshot(SnapshotV1, null, SnapshotState.Committed, FixedNow, FixedNow));
        harness.AddAudience(new Audience(SnapshotV1, AudienceDefault, "Default", null, false));
        harness.AddAudienceResolution(new AudienceResolution(SnapshotV1, AudienceDefault, AudienceDefault, 1));
        harness.AddNode(new Node(SnapshotV1, RootId, null, "Root", null, 0, false));

        // 25 Kindknoten anlegen
        for (var i = 1; i <= 25; i++)
        {
            var cid = new NodeId(Guid.Parse($"20000000-0000-0000-0000-{i:D12}"));
            harness.AddNode(new Node(SnapshotV1, cid, RootId, $"Child {i}", null, i, false));
            harness.AddContent(new NodeContent(SnapshotV1, cid, AudienceDefault, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, $"Inhalt {i}", false));
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
            new ListChildrenQuery(RootId, new ReadContext(SnapshotId: SnapshotV1), AudienceDefault, Limit: 9999));
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
        // Insgesamt 1 Audience + 1 Resolution + 26 Nodes + 25 Contents = 53 Items
        var totalDiffItemsOnPage = diffResult.Value.Audiences.Count + diffResult.Value.AudienceResolutions.Count + diffResult.Value.Nodes.Count + diffResult.Value.Contents.Count + diffResult.Value.Dependencies.Count;
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
        var exportResult = await exportService.ExportTreeAsync(RootId, new ReadContext(SnapshotId: SnapshotV1), AudienceDefault);
        Assert.True(exportResult.IsSuccess);
        // Enthält alle 25 Child-Überschriften
        for (var i = 1; i <= 25; i++)
        {
            Assert.Contains($"## Child {i}", exportResult.Value!);
        }
    }

    // ── Test Harness (düner Wrapper über die gemeinsamen TestSupport-Fakes) ──

    private sealed class MultiSnapshotTestHarness
    {
        private readonly InMemoryKnowledgeStore _store = new();

        public InMemoryReleaseMutationRepository ReleaseRepo { get; } = new();

        public void AddSnapshot(Snapshot s) => _store.Snapshots.Add(s);
        public void AddNode(Node n) => _store.Nodes.Add(n);
        public void AddAudience(Audience r) => _store.Audiences.Add(r);
        public void AddAudienceResolution(AudienceResolution res) => _store.Resolutions.Add(res);
        public void AddContent(NodeContent c) => _store.Contents.Add(c);

        public SnapshotReadRepositories CreateSnapshotReadRepositories() => new(
            new InMemorySnapshotRepository(_store),
            new InMemoryTransactionRepository(_store),
            new InMemoryHierarchyRepository(_store),
            new InMemoryContentRepository(_store),
            new InMemoryAudienceRepository(_store),
            new InMemoryDependencyRepository(_store));

        public MarkdownExportService CreateExportService() => new(CreateSnapshotReadRepositories());

        public HistoryService CreateHistoryService(RetrievalPolicy? policy = null) =>
            new(CreateSnapshotReadRepositories(), policy ?? StandardPolicy());

        public ReleaseService CreateReleaseService(RetrievalPolicy? policy = null) => new(
            CreateSnapshotReadRepositories(),
            ReleaseRepo,
            new FixedClock(FixedNow),
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
                repos.Audiences,
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
}
