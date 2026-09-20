using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Retrieval.Export;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.Retrieval.Export;

[Trait("Category", "Unit")]
public sealed class MarkdownExportServiceTests
{
    private static readonly SnapshotId CurrentSnapshotId = new(10);
    private static readonly AudienceId AudienceDeveloper = new("Developer");
    private static readonly AudienceId AudienceEndUser = new("EndUser");

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
        harness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, false));
        harness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, AudienceDeveloper, AudienceDeveloper, 1));
        harness.AddContent(new NodeContent(
            CurrentSnapshotId,
            RootId,
            AudienceDeveloper,
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            "Dies ist der Root-Inhalt.",
            false));

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), AudienceDeveloper);

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

        harness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, false));
        harness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, AudienceDeveloper, AudienceDeveloper, 1));

        harness.AddContent(new NodeContent(CurrentSnapshotId, RootId, AudienceDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Inhalt Hauptkapitel.", false));
        harness.AddContent(new NodeContent(CurrentSnapshotId, Child1Id, AudienceDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Inhalt Unterabschnitt.", false));
        harness.AddContent(new NodeContent(CurrentSnapshotId, Grandchild1Id, AudienceDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Inhalt Detailpunkt.", false));

        var service = harness.CreateService();

        // 1. Export ab Hauptkapitel (Root) -> Hauptkapitel ist H1, Unterabschnitt H2, Detailpunkt H3
        var fullResult = await service.ExportTreeAsync(RootId, new ReadContext(), AudienceDeveloper);
        Assert.True(fullResult.IsSuccess);
        var expectedFull =
            "# Hauptkapitel\n\nInhalt Hauptkapitel.\n\n" +
            "## Unterabschnitt\n\nInhalt Unterabschnitt.\n\n" +
            "### Detailpunkt\n\nInhalt Detailpunkt.\n";
        Assert.Equal(expectedFull, fullResult.Value);

        // 2. Export ab Unterabschnitt (Teilbaum) -> Unterabschnitt wird H1, Detailpunkt H2 (relative Heading-Level!)
        var subResult = await service.ExportTreeAsync(Child1Id, new ReadContext(), AudienceDeveloper);
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

        harness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, false));
        harness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, AudienceDeveloper, AudienceDeveloper, 1));
        harness.AddContent(new NodeContent(CurrentSnapshotId, RootId, AudienceDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Root Text", false));
        harness.AddContent(new NodeContent(CurrentSnapshotId, Child1Id, AudienceDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Kind 1 Text", false));
        harness.AddContent(new NodeContent(CurrentSnapshotId, Child2Id, AudienceDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Kind 2 Text", false));

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), AudienceDeveloper);

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

        harness.AddAudience(new Audience(CurrentSnapshotId, AudienceEndUser, "EndUser", null, false));
        harness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, AudienceEndUser, AudienceEndUser, 1));

        // Nur Child1 hat Content für EndUser
        harness.AddContent(new NodeContent(CurrentSnapshotId, Child1Id, AudienceEndUser, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Auftragserfassung Inhalt.", false));

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), AudienceEndUser);

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

        harness.AddAudience(new Audience(CurrentSnapshotId, AudienceEndUser, "EndUser", null, false));
        harness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, false));
        harness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, AudienceEndUser, AudienceEndUser, 1));
        harness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, AudienceDeveloper, AudienceDeveloper, 1));

        // Auftragserfassung hat Content für EndUser
        harness.AddContent(new NodeContent(CurrentSnapshotId, Child1Id, AudienceEndUser, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Auftragserfassung für EndUser.", false));
        // Interne API hat nur Content für Developer, KEINEN für EndUser
        harness.AddContent(new NodeContent(CurrentSnapshotId, IrrelevantChildId, AudienceDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Geheime API Dokumentation.", false));

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), AudienceEndUser);

        Assert.True(result.IsSuccess);
        // Interne API darf im EndUser-Export NICHT vorkommen
        Assert.DoesNotContain("Interne API", result.Value);
        var expected = "# Administration\n\n## Auftragserfassung\n\nAuftragserfassung für EndUser.\n";
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public async Task ExportTreeAsync_AudienceResolutionFallback_UsesFallbackContentWhenConfigured()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Root", null, 1, false));
        harness.AddAudience(new Audience(CurrentSnapshotId, AudienceEndUser, "EndUser", null, false));
        harness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, false));

        // EndUser -> Fallback auf Developer
        harness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, AudienceEndUser, AudienceEndUser, 1));
        harness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, AudienceEndUser, AudienceDeveloper, 2));

        // Content existiert nur für Developer
        harness.AddContent(new NodeContent(CurrentSnapshotId, RootId, AudienceDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Developer Content als Fallback.", false));

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), AudienceEndUser);

        Assert.True(result.IsSuccess);
        var expected = "# Root\n\nDeveloper Content als Fallback.\n";
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public async Task ExportTreeAsync_StaleDerivedContent_ExportsContentAndEmitsStaleDerivedContentWarning()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Root", null, 1, false));
        harness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, false));
        harness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, AudienceDeveloper, AudienceDeveloper, 1));

        var oldSourceRev = new ContentRevisionId(Guid.NewGuid());
        var currentSourceRev = new ContentRevisionId(Guid.NewGuid());
        var sourceNodeId = new NodeId(Guid.Parse("99999999-9999-9999-9999-999999999999"));

        // Source Content hat jetzt currentSourceRev
        harness.AddContent(new NodeContent(CurrentSnapshotId, sourceNodeId, AudienceDeveloper, currentSourceRev, ContentMode.Independent, "Source Text", false));

        // Target Content (Root) ist Derived und verweist noch auf oldSourceRev -> transitiv stale!
        harness.AddContent(new NodeContent(CurrentSnapshotId, RootId, AudienceDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Derived, "Abgeleiteter Text.", false));
        harness.AddDependency(new ContentDependency(CurrentSnapshotId, RootId, AudienceDeveloper, sourceNodeId, AudienceDeveloper, oldSourceRev));

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), AudienceDeveloper);

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
        harness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, false));
        harness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, AudienceDeveloper, AudienceDeveloper, 1));
        // Keine Contents hinzugefügt

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), AudienceDeveloper);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value);
    }

    [Fact]
    public async Task ExportTreeAsync_DepthExceeding6Levels_ClampsToH6AndEmitsHierarchyTooDeepWarning()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, false));
        harness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, AudienceDeveloper, AudienceDeveloper, 1));

        // Kette von 8 Ebenen erzeugen
        var previousId = (NodeId?)null;
        var nodeIds = new List<NodeId>();
        for (var i = 1; i <= 8; i++)
        {
            var id = new NodeId(Guid.Parse($"00000000-0000-0000-0000-00000000000{i}"));
            nodeIds.Add(id);
            harness.AddNode(new Node(CurrentSnapshotId, id, previousId, $"Ebene {i}", null, 1, false));
            harness.AddContent(new NodeContent(CurrentSnapshotId, id, AudienceDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, $"Inhalt Ebene {i}.", false));
            previousId = id;
        }

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(nodeIds[0], new ReadContext(), AudienceDeveloper);

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

        var result = await service.ExportTreeAsync(missingNodeId, new ReadContext(), AudienceDeveloper);

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

        var result = await service.ExportTreeAsync(RootId, new ReadContext(IncludeDeleted: false), AudienceDeveloper);

        Assert.False(result.IsSuccess);
        Assert.Equal(NavigationErrorCodes.NodeNotFound, result.Code);
    }

    [Fact]
    public async Task ExportTreeAsync_NormalizesNewlinesToLF_StabileLeerzeilen()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Root", null, 1, false));
        harness.AddNode(new Node(CurrentSnapshotId, Child1Id, RootId, "Child", null, 1, false));
        harness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, false));
        harness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, AudienceDeveloper, AudienceDeveloper, 1));

        // Content enthält Windows-CRLF \r\n und trailing newline
        var crlfContent = "Zeile 1\r\nZeile 2\r\n\r\n";
        harness.AddContent(new NodeContent(CurrentSnapshotId, RootId, AudienceDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, crlfContent, false));
        harness.AddContent(new NodeContent(CurrentSnapshotId, Child1Id, AudienceDeveloper, new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Kind Zeile\r\n", false));

        var service = harness.CreateService();
        var result = await service.ExportTreeAsync(RootId, new ReadContext(), AudienceDeveloper);

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
    public async Task ExportTreeAsync_RequestedAudienceNotFound_ReturnsRequestedAudienceNotFound()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Root", null, 1, false));
        var missingAudience = new AudienceId("MissingAudience");
        var service = harness.CreateService();

        var result = await service.ExportTreeAsync(RootId, new ReadContext(), missingAudience);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.RequestedAudienceNotFound, result.Code);
        Assert.Equal(missingAudience.ToString(), result.Details[AudienceResolutionErrorCodes.RequestedAudienceIdDetail]);
    }

    [Fact]
    public async Task ExportTreeAsync_CandidateAudienceDeleted_ReturnsCandidateAudienceDeleted()
    {
        var harness = new ExportTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Root", null, 1, false));
        var archivedAudience = new AudienceId("Archived");
        harness.AddAudience(new Audience(CurrentSnapshotId, AudienceDeveloper, "Developer", null, false));
        harness.AddAudience(new Audience(CurrentSnapshotId, archivedAudience, "Archived", null, true));
        harness.AddAudienceResolution(new AudienceResolution(CurrentSnapshotId, AudienceDeveloper, archivedAudience, 1));
        var service = harness.CreateService();

        var result = await service.ExportTreeAsync(RootId, new ReadContext(), AudienceDeveloper);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.CandidateAudienceDeleted, result.Code);
        Assert.Equal(archivedAudience.ToString(), result.Details[AudienceResolutionErrorCodes.CandidateAudienceIdDetail]);
    }

    // ── Test Harness (düner Wrapper über die gemeinsamen TestSupport-Fakes) ──

    private sealed class ExportTestHarness(SnapshotId currentSnapshotId)
    {
        private readonly InMemoryKnowledgeStore _store =
            InMemoryKnowledgeStore.WithCurrentCommittedSnapshot(currentSnapshotId, DateTimeOffset.UtcNow);

        public void AddNode(Node node) => _store.Nodes.Add(node);
        public void AddAudience(Audience audience) => _store.Audiences.Add(audience);
        public void AddAudienceResolution(AudienceResolution resolution) => _store.Resolutions.Add(resolution);
        public void AddContent(NodeContent content) => _store.Contents.Add(content);
        public void AddDependency(ContentDependency dependency) => _store.Dependencies.Add(dependency);

        public MarkdownExportService CreateService() => new(
            new SnapshotReadRepositories(
                new InMemorySnapshotRepository(_store),
                new InMemoryTransactionRepository(_store),
                new InMemoryHierarchyRepository(_store),
                new InMemoryContentRepository(_store),
                new InMemoryAudienceRepository(_store),
                new InMemoryDependencyRepository(_store)));
    }
}
