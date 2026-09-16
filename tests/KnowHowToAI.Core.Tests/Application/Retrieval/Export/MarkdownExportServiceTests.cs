using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Retrieval.Export;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Tests.Application.Retrieval.Export;

[Trait("Category", "Unit")]
public sealed class MarkdownExportServiceTests
{
    private static readonly SnapshotId CurrentSnapshotId = new(10);
    private static readonly RoleId RoleDeveloper = new("Developer");
    private static readonly RoleId RoleEndUser = new("EndUser");

    private static readonly NodeId RootId = new(Guid.Parse("10000000-0000-0000-0000-000000000000"));
    private static readonly NodeId Child1Id = new(Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly NodeId Child2Id = new(Guid.Parse("10000000-0000-0000-0000-000000000002"));
    private static readonly NodeId Grandchild1Id = new(Guid.Parse("10000000-0000-0000-0000-000000000003"));
    private static readonly NodeId IrrelevantChildId = new(Guid.Parse("10000000-0000-0000-0000-000000000099"));

    [Fact]
    public async Task ExportTreeAsync_SingleNodeWithContent_ExportsHeading1AndContent()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Root Title", null, 0, false));
        harness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));
        harness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleDeveloper, RoleDeveloper, 1));
        harness.AddContent(new NodeContent(
            CurrentSnapshotId,
            RootId,
            RoleDeveloper,
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            "Dies ist der Root-Inhalt.",
            false));

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), RoleDeveloper);

        Assert.True(result.IsSuccess);
        var expected = "# Root Title\n\nDies ist der Root-Inhalt.\n";
        Assert.Equal(expected, result.Value);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task ExportTreeAsync_Hierarchy_SelectedRootBecomesH1AndDescendantsReceiveRelativeLevels()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        // Root hat global ParentNodeId null, Child1 ist Kind von Root, Grandchild ist Kind von Child1
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Hauptkapitel", null, 1, false));
        harness.AddNode(new Node(CurrentSnapshotId, Child1Id, RootId, "Unterabschnitt", null, 1, false));
        harness.AddNode(new Node(CurrentSnapshotId, Grandchild1Id, Child1Id, "Detailpunkt", null, 1, false));

        harness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));
        harness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleDeveloper, RoleDeveloper, 1));

        harness.AddContent(new NodeContent(CurrentSnapshotId, RootId, RoleDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Inhalt Hauptkapitel.", false));
        harness.AddContent(new NodeContent(CurrentSnapshotId, Child1Id, RoleDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Inhalt Unterabschnitt.", false));
        harness.AddContent(new NodeContent(CurrentSnapshotId, Grandchild1Id, RoleDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Inhalt Detailpunkt.", false));

        var service = harness.CreateService();

        // 1. Export ab Hauptkapitel (Root) -> Hauptkapitel ist H1, Unterabschnitt H2, Detailpunkt H3
        var fullResult = await service.ExportTreeAsync(RootId, new ReadContext(), RoleDeveloper);
        Assert.True(fullResult.IsSuccess);
        var expectedFull =
            "# Hauptkapitel\n\nInhalt Hauptkapitel.\n\n" +
            "## Unterabschnitt\n\nInhalt Unterabschnitt.\n\n" +
            "### Detailpunkt\n\nInhalt Detailpunkt.\n";
        Assert.Equal(expectedFull, fullResult.Value);

        // 2. Export ab Unterabschnitt (Teilbaum) -> Unterabschnitt wird H1, Detailpunkt H2 (relative Heading-Level!)
        var subResult = await service.ExportTreeAsync(Child1Id, new ReadContext(), RoleDeveloper);
        Assert.True(subResult.IsSuccess);
        var expectedSub =
            "# Unterabschnitt\n\nInhalt Unterabschnitt.\n\n" +
            "## Detailpunkt\n\nInhalt Detailpunkt.\n";
        Assert.Equal(expectedSub, subResult.Value);
    }

    [Fact]
    public async Task ExportTreeAsync_DeterministicSiblingOrdering_SortsBySortOrderThenNodeId()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Root", null, 0, false));
        // Child2 (SortOrder 20), Child1 (SortOrder 10)
        harness.AddNode(new Node(CurrentSnapshotId, Child2Id, RootId, "Zweites Kind", null, 20, false));
        harness.AddNode(new Node(CurrentSnapshotId, Child1Id, RootId, "Erstes Kind", null, 10, false));

        harness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));
        harness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleDeveloper, RoleDeveloper, 1));
        harness.AddContent(new NodeContent(CurrentSnapshotId, RootId, RoleDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Root Text", false));
        harness.AddContent(new NodeContent(CurrentSnapshotId, Child1Id, RoleDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Kind 1 Text", false));
        harness.AddContent(new NodeContent(CurrentSnapshotId, Child2Id, RoleDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Kind 2 Text", false));

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), RoleDeveloper);

        Assert.True(result.IsSuccess);
        var expected =
            "# Root\n\nRoot Text\n\n" +
            "## Erstes Kind\n\nKind 1 Text\n\n" +
            "## Zweites Kind\n\nKind 2 Text\n";
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public async Task ExportTreeAsync_StructuralNodeWithoutContent_IncludedIfDescendantHasContent()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        // Root hat KEINEN Content für EndUser, Child1 hat Content für EndUser
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Administration", null, 1, false));
        harness.AddNode(new Node(CurrentSnapshotId, Child1Id, RootId, "Auftragserfassung", null, 1, false));

        harness.AddRole(new Role(CurrentSnapshotId, RoleEndUser, "EndUser", null, false));
        harness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleEndUser, RoleEndUser, 1));

        // Nur Child1 hat Content für EndUser
        harness.AddContent(new NodeContent(CurrentSnapshotId, Child1Id, RoleEndUser, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Auftragserfassung Inhalt.", false));

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), RoleEndUser);

        Assert.True(result.IsSuccess);
        // Administration wird als Strukturknoten (H1) ausgegeben, gefolgt von Auftragserfassung (H2)
        var expected = "# Administration\n\n## Auftragserfassung\n\nAuftragserfassung Inhalt.\n";
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public async Task ExportTreeAsync_IrrelevantBranches_Omitted()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Administration", null, 1, false));
        harness.AddNode(new Node(CurrentSnapshotId, Child1Id, RootId, "Auftragserfassung", null, 1, false));
        harness.AddNode(new Node(CurrentSnapshotId, IrrelevantChildId, RootId, "Interne API", null, 2, false));

        harness.AddRole(new Role(CurrentSnapshotId, RoleEndUser, "EndUser", null, false));
        harness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));
        harness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleEndUser, RoleEndUser, 1));
        harness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleDeveloper, RoleDeveloper, 1));

        // Auftragserfassung hat Content für EndUser
        harness.AddContent(new NodeContent(CurrentSnapshotId, Child1Id, RoleEndUser, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Auftragserfassung für EndUser.", false));
        // Interne API hat nur Content für Developer, KEINEN für EndUser
        harness.AddContent(new NodeContent(CurrentSnapshotId, IrrelevantChildId, RoleDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Geheime API Dokumentation.", false));

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), RoleEndUser);

        Assert.True(result.IsSuccess);
        // Interne API darf im EndUser-Export NICHT vorkommen
        Assert.DoesNotContain("Interne API", result.Value);
        var expected = "# Administration\n\n## Auftragserfassung\n\nAuftragserfassung für EndUser.\n";
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public async Task ExportTreeAsync_RoleResolutionFallback_UsesFallbackContentWhenConfigured()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Root", null, 1, false));
        harness.AddRole(new Role(CurrentSnapshotId, RoleEndUser, "EndUser", null, false));
        harness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));

        // EndUser -> Fallback auf Developer
        harness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleEndUser, RoleEndUser, 1));
        harness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleEndUser, RoleDeveloper, 2));

        // Content existiert nur für Developer
        harness.AddContent(new NodeContent(CurrentSnapshotId, RootId, RoleDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Developer Content als Fallback.", false));

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), RoleEndUser);

        Assert.True(result.IsSuccess);
        var expected = "# Root\n\nDeveloper Content als Fallback.\n";
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public async Task ExportTreeAsync_StaleDerivedContent_ExportsContentAndEmitsStaleDerivedContentWarning()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Root", null, 1, false));
        harness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));
        harness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleDeveloper, RoleDeveloper, 1));

        var oldSourceRev = new ContentRevisionId(Guid.NewGuid());
        var currentSourceRev = new ContentRevisionId(Guid.NewGuid());
        var sourceNodeId = new NodeId(Guid.Parse("99999999-9999-9999-9999-999999999999"));

        // Source Content hat jetzt currentSourceRev
        harness.AddContent(new NodeContent(CurrentSnapshotId, sourceNodeId, RoleDeveloper, currentSourceRev, ContentMode.Independent, "Source Text", false));

        // Target Content (Root) ist Derived und verweist noch auf oldSourceRev -> transitiv stale!
        harness.AddContent(new NodeContent(CurrentSnapshotId, RootId, RoleDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Derived, "Abgeleiteter Text.", false));
        harness.AddDependency(new ContentDependency(CurrentSnapshotId, RootId, RoleDeveloper, sourceNodeId, RoleDeveloper, oldSourceRev));

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), RoleDeveloper);

        Assert.True(result.IsSuccess);
        var expected = "# Root\n\nAbgeleiteter Text.\n";
        Assert.Equal(expected, result.Value);
        Assert.Contains(result.Warnings, w => w.Code == QualityWarningCodes.StaleDerivedContent);
    }

    [Fact]
    public async Task ExportTreeAsync_EmptyTreeWithoutContent_ReturnsEmptyString()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Root", null, 1, false));
        harness.AddNode(new Node(CurrentSnapshotId, Child1Id, RootId, "Child", null, 1, false));
        harness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));
        harness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleDeveloper, RoleDeveloper, 1));
        // Keine Contents hinzugefügt

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), RoleDeveloper);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public async Task ExportTreeAsync_DepthExceeding6Levels_ClampsToH6AndEmitsHierarchyTooDeepWarning()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));
        harness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleDeveloper, RoleDeveloper, 1));

        // Kette von 8 Ebenen erzeugen
        var previousId = (NodeId?)null;
        var nodeIds = new List<NodeId>();
        for (var i = 1; i <= 8; i++)
        {
            var id = new NodeId(Guid.Parse($"00000000-0000-0000-0000-00000000000{i}"));
            nodeIds.Add(id);
            harness.AddNode(new Node(CurrentSnapshotId, id, previousId, $"Ebene {i}", null, 1, false));
            harness.AddContent(new NodeContent(CurrentSnapshotId, id, RoleDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, $"Inhalt Ebene {i}.", false));
            previousId = id;
        }

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(nodeIds[0], new ReadContext(), RoleDeveloper);

        Assert.True(result.IsSuccess);
        var output = result.Value!;

        // Ebenen 1 bis 6: # bis ######
        Assert.Contains("# Ebene 1", output);
        Assert.Contains("## Ebene 2", output);
        Assert.Contains("### Ebene 3", output);
        Assert.Contains("#### Ebene 4", output);
        Assert.Contains("##### Ebene 5", output);
        Assert.Contains("###### Ebene 6", output);
        // Ebenen 7 und 8: auf ###### geklammert (kein #######)
        Assert.Contains("###### Ebene 7", output);
        Assert.Contains("###### Ebene 8", output);
        Assert.DoesNotContain("#######", output);

        // Warnung HierarchyTooDeep mit actualDepth = 8
        var depthWarning = Assert.Single(result.Warnings, w => w.Code == QualityWarningCodes.HierarchyTooDeep);
        Assert.Equal("8", depthWarning.Details[QualityWarningCodes.ActualDepthDetail]);
        Assert.Equal("6", depthWarning.Details[QualityWarningCodes.ThresholdDepthDetail]);
    }

    [Fact]
    public async Task ExportTreeAsync_NodeNotFound_ReturnsNodeNotFound()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        var missingNodeId = new NodeId(Guid.NewGuid());
        var service = harness.CreateService();

        var result = await service.ExportTreeAsync(missingNodeId, new ReadContext(), RoleDeveloper);

        Assert.False(result.IsSuccess);
        Assert.Equal(NavigationErrorCodes.NodeNotFound, result.Code);
        Assert.Equal(missingNodeId.ToString(), result.Details[NavigationErrorCodes.NodeIdDetail]);
    }

    [Fact]
    public async Task ExportTreeAsync_DeletedNode_WithoutIncludeDeleted_ReturnsNodeNotFound()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Gelöschter Root", null, 1, IsDeleted: true));
        var service = harness.CreateService();

        var result = await service.ExportTreeAsync(RootId, new ReadContext(IncludeDeleted: false), RoleDeveloper);

        Assert.False(result.IsSuccess);
        Assert.Equal(NavigationErrorCodes.NodeNotFound, result.Code);
    }

    [Fact]
    public async Task ExportTreeAsync_NormalizesNewlinesToLF_StabileLeerzeilen()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Root", null, 1, false));
        harness.AddNode(new Node(CurrentSnapshotId, Child1Id, RootId, "Child", null, 1, false));
        harness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));
        harness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleDeveloper, RoleDeveloper, 1));

        // Content enthält Windows-CRLF \r\n und trailing newline
        var crlfContent = "Zeile 1\r\nZeile 2\r\n\r\n";
        harness.AddContent(new NodeContent(CurrentSnapshotId, RootId, RoleDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, crlfContent, false));
        harness.AddContent(new NodeContent(CurrentSnapshotId, Child1Id, RoleDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Kind Zeile\r\n", false));

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), RoleDeveloper);

        Assert.True(result.IsSuccess);
        var output = result.Value!;

        // Keine CRLF im Gesamtergebnis
        Assert.DoesNotContain("\r", output);

        var expected =
            "# Root\n\nZeile 1\nZeile 2\n\n" +
            "## Child\n\nKind Zeile\n";
        Assert.Equal(expected, output);
    }

    [Fact]
    public async Task ExportTreeAsync_RequestedRoleNotFound_ReturnsRequestedRoleNotFound()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Root", null, 1, false));
        var missingRole = new RoleId("MissingRole");
        var service = harness.CreateService();

        var result = await service.ExportTreeAsync(RootId, new ReadContext(), missingRole);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleNotFound, result.Code);
        Assert.Equal(missingRole.ToString(), result.Details[RoleResolutionErrorCodes.RequestedRoleIdDetail]);
    }

    [Fact]
    public async Task ExportTreeAsync_CandidateRoleDeleted_ReturnsCandidateRoleDeleted()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Root", null, 1, false));
        var archivedRole = new RoleId("Archived");
        harness.AddRole(new Role(CurrentSnapshotId, RoleDeveloper, "Developer", null, false));
        harness.AddRole(new Role(CurrentSnapshotId, archivedRole, "Archived", null, true));
        harness.AddRoleResolution(new RoleResolution(CurrentSnapshotId, RoleDeveloper, archivedRole, 1));
        var service = harness.CreateService();

        var result = await service.ExportTreeAsync(RootId, new ReadContext(), RoleDeveloper);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.CandidateRoleDeleted, result.Code);
        Assert.Equal(archivedRole.ToString(), result.Details[RoleResolutionErrorCodes.CandidateRoleIdDetail]);
    }

    // ── Test Harness & Fakes ────────────────────────────────────────────────

    private sealed class ExportTestHarness
    {
        private SnapshotId _currentSnapshotId;
        private readonly List<Snapshot> _snapshots = [];
        private readonly Dictionary<TransactionId, KnowledgeTransaction> _transactions = new();
        private readonly List<Node> _nodes = [];
        private readonly List<Role> _roles = [];
        private readonly List<RoleResolution> _resolutions = [];
        private readonly List<NodeContent> _contents = [];
        private readonly List<ContentDependency> _dependencies = [];

        public ExportTestHarness(SnapshotId currentSnapshotId)
        {
            _currentSnapshotId = currentSnapshotId;
            _snapshots.Add(new Snapshot(currentSnapshotId, null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        }

        public void AddNode(Node node) => _nodes.Add(node);
        public void AddRole(Role role) => _roles.Add(role);
        public void AddRoleResolution(RoleResolution resolution) => _resolutions.Add(resolution);
        public void AddContent(NodeContent content) => _contents.Add(content);
        public void AddDependency(ContentDependency dependency) => _dependencies.Add(dependency);

        public MarkdownExportService CreateService() => new(
            new SnapshotReadRepositories(
                new SnapshotRepoFake(_snapshots, () => _currentSnapshotId),
                new TransactionRepoFake(_transactions),
                new HierarchyRepoFake(_nodes),
                new ContentRepoFake(_contents),
                new RoleRepoFake(_roles, _resolutions),
                new DependencyRepoFake(_dependencies)));
    }

    private sealed class SnapshotRepoFake : ISnapshotRepository
    {
        private readonly List<Snapshot> _snapshots;
        private readonly Func<SnapshotId> _currentSnapshotIdProvider;

        public SnapshotRepoFake(List<Snapshot> snapshots, Func<SnapshotId> currentSnapshotIdProvider)
        {
            _snapshots = snapshots;
            _currentSnapshotIdProvider = currentSnapshotIdProvider;
        }

        public Task<Snapshot?> FindAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshots.FirstOrDefault(s => s.SnapshotId == snapshotId));

        public Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default)
        {
            var currentId = _currentSnapshotIdProvider();
            return Task.FromResult(_snapshots.First(s => s.SnapshotId == currentId));
        }
    }

    private sealed class TransactionRepoFake : ITransactionRepository
    {
        private readonly Dictionary<TransactionId, KnowledgeTransaction> _transactions;

        public TransactionRepoFake(Dictionary<TransactionId, KnowledgeTransaction> transactions)
        {
            _transactions = transactions;
        }

        public Task<KnowledgeTransaction> BeginAsync(KnowHowToAI.Core.Application.Transactions.BeginTransactionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_transactions.GetValueOrDefault(transactionId));
        public Task<KnowHowToAI.Core.Application.Transactions.CommitTransactionResult> CommitAsync(KnowHowToAI.Core.Application.Transactions.CommitTransactionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class HierarchyRepoFake : IHierarchyRepository
    {
        private readonly List<Node> _nodes;
        public HierarchyRepoFake(List<Node> nodes) => _nodes = nodes;

        public Task<IReadOnlyList<Node>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Node>>(_nodes.Where(n => n.SnapshotId == snapshotId).ToArray());
    }

    private sealed class RoleRepoFake : IRoleRepository
    {
        private readonly List<Role> _roles;
        private readonly List<RoleResolution> _resolutions;

        public RoleRepoFake(List<Role> roles, List<RoleResolution> resolutions)
        {
            _roles = roles;
            _resolutions = resolutions;
        }

        public Task<IReadOnlyList<Role>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Role>>(_roles.Where(r => r.SnapshotId == snapshotId).ToArray());

        public Task<IReadOnlyList<RoleResolution>> ListResolutionsBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoleResolution>>(_resolutions.Where(r => r.SnapshotId == snapshotId).ToArray());
    }

    private sealed class ContentRepoFake : IContentRepository
    {
        private readonly List<NodeContent> _contents;
        public ContentRepoFake(List<NodeContent> contents) => _contents = contents;

        public Task<IReadOnlyList<NodeContent>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NodeContent>>(_contents.Where(c => c.SnapshotId == snapshotId).ToArray());
    }

    private sealed class DependencyRepoFake : IDependencyRepository
    {
        private readonly List<ContentDependency> _dependencies;
        public DependencyRepoFake(List<ContentDependency> dependencies) => _dependencies = dependencies;

        public Task<IReadOnlyList<ContentDependency>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ContentDependency>>(_dependencies.Where(d => d.SnapshotId == snapshotId).ToArray());
    }
}
