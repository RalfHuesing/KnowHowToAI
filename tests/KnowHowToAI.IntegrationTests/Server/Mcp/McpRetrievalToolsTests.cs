using System.Text.Json;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Server.Mcp.Tools.Retrieval;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Handler-Vertragstests der Retrieval-Tools (search, export_tree): dünne Delegation
/// an SearchService und MarkdownExportService mit protokollkonformer Error-Struktur,
/// Paging-Grenzen und Export-Ausgabe. Keine SQL- oder Server-Infrastruktur.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpRetrievalToolsTests
{
    private static readonly SnapshotId CurrentSnapshotId = new(100);
    private static readonly RoleId RoleDeveloper = new("Developer");
    private static readonly NodeId RootId = new(Guid.Parse("40000000-0000-0000-0000-000000000000"));
    private static readonly NodeId ChildId = new(Guid.Parse("40000000-0000-0000-0000-000000000001"));
    private static readonly NodeId UnknownNodeId = new(Guid.Parse("40000000-0000-0000-0000-000000009999"));

    [Fact]
    public async Task Search_DelegatesQueryAndMapsHitsWithMetadataFirst()
    {
        var repository = new ScriptedRetrievalRepository { Response = SearchResponse(TwoHits()) };
        var tools = CreateSearchTools(repository);
        var cursor = new SearchCursor(CurrentSnapshotId, null, "Auftrag", null, 1, 0, RootId).Encode();

        var envelope = await tools.Search("Auftrag", cursor: cursor);

        var request = Assert.Single(repository.Requests);
        Assert.Equal("Auftrag", request.Text);
        Assert.Null(request.RoleId);
        Assert.Equal(cursor, request.Cursor);
        Assert.Equal(CurrentSnapshotId, request.SnapshotId);
        Assert.True(envelope.IsSuccess);
        Assert.Equal("Auftrag", envelope.Data!.Query);
        Assert.Equal(2, envelope.Data.Items.Count);
        Assert.Equal(RootId.ToString(), envelope.Data.Items[0].NodeId);
        Assert.Equal("Auftragserfassung", envelope.Data.Items[0].Title);
        Assert.Equal("Title", envelope.Data.Items[0].HitField);
        Assert.Null(envelope.Data.Items[0].Snippet);
        Assert.Equal(nameof(Availability.Explicit), envelope.Data.Items[0].Availability);
        Assert.Equal("Content", envelope.Data.Items[1].HitField);
        Assert.NotNull(envelope.Data.Items[1].Snippet);
        Assert.Null(envelope.Data.NextCursor);
    }

    [Fact]
    public async Task Search_MissingLimit_UsesConfiguredSearchPageSize()
    {
        var repository = new ScriptedRetrievalRepository { Response = SearchResponse(FiveHits()) };
        var tools = CreateSearchTools(repository);

        var envelope = await tools.Search("Auftrag");

        Assert.True(envelope.IsSuccess);
        Assert.Equal(2, envelope.Data!.Items.Count);
        Assert.NotNull(envelope.Data.NextCursor);
        Assert.Equal(3, Assert.Single(repository.Requests).Limit);
    }

    [Fact]
    public async Task Search_LimitAboveMaximum_IsClampedToSearchMaximumPageSize()
    {
        var repository = new ScriptedRetrievalRepository { Response = SearchResponse(FiveHits()) };
        var tools = CreateSearchTools(repository);

        var envelope = await tools.Search("Auftrag", limit: 9999);

        Assert.True(envelope.IsSuccess);
        Assert.Equal(3, envelope.Data!.Items.Count);
        Assert.NotNull(envelope.Data.NextCursor);
        Assert.Equal(4, Assert.Single(repository.Requests).Limit);
    }

    [Fact]
    public async Task Search_EmptyText_ReturnsEmptySuccessPageWithoutRepositoryCall()
    {
        var repository = new ScriptedRetrievalRepository();
        var tools = CreateSearchTools(repository);

        var envelope = await tools.Search("   ");

        Assert.True(envelope.IsSuccess);
        Assert.Empty(envelope.Data!.Items);
        Assert.Null(envelope.Data.NextCursor);
        Assert.Empty(repository.Requests);
    }

    [Fact]
    public async Task Search_BothSelectors_ReturnsInvalidReadContextWithoutRepositoryCall()
    {
        var repository = new ScriptedRetrievalRepository();
        var tools = CreateSearchTools(repository);

        var envelope = await tools.Search(
            "Auftrag",
            transactionId: "0d0b1f5a-4e12-4c1e-9f31-5d3e2a8d7b90",
            snapshotId: "100");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, envelope.Code);
        Assert.Null(envelope.Data);
        Assert.Empty(repository.Requests);
    }

    [Fact]
    public async Task Search_UnknownRole_ReturnsStableRoleErrorEnvelope()
    {
        var repository = new ScriptedRetrievalRepository { Response = SearchResponse(TwoHits()) };
        var tools = CreateSearchTools(repository);

        var envelope = await tools.Search("Auftrag", roleId: "Nonexistent");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleNotFound, envelope.Code);
        Assert.Equal(
            "Nonexistent",
            envelope.Details![RoleResolutionErrorCodes.RequestedRoleIdDetail]);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task Search_InvalidCursor_ReturnsStableInvalidCursorEnvelope()
    {
        var repository = new ScriptedRetrievalRepository();
        var tools = CreateSearchTools(repository);

        var envelope = await tools.Search("Auftrag", cursor: "kaputter-cursor");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(SearchErrorCodes.InvalidCursor, envelope.Code);
        Assert.Equal("kaputter-cursor", envelope.Details![SearchErrorCodes.CursorDetail]);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task ExportTree_MapsMarkdownWithRelativeHeadingLevels()
    {
        var harness = CreateExportHarness(withContent: true, chainLength: 2);
        var tools = CreateExportTools(harness);

        var envelope = await tools.ExportTree(RootId.ToString(), RoleDeveloper.Value);

        Assert.True(envelope.IsSuccess);
        Assert.Equal(
            "# Hauptkapitel\n\nInhalt Hauptkapitel.\n\n## Unterabschnitt 2\n\nInhalt Unterabschnitt 2.\n",
            envelope.Data!.Markdown);
        Assert.Null(envelope.Warnings);
    }

    [Fact]
    public async Task ExportTree_HierarchyTooDeep_AddsQualityWarningOnEnvelopeLevel()
    {
        var harness = CreateExportHarness(withContent: true, chainLength: 7);
        var tools = CreateExportTools(harness);

        var envelope = await tools.ExportTree(RootId.ToString(), RoleDeveloper.Value);

        Assert.True(envelope.IsSuccess);
        var warning = Assert.Single(envelope.Warnings!);
        Assert.Equal(QualityWarningCodes.HierarchyTooDeep, warning.Code);
        Assert.Equal("7", warning.Details![QualityWarningCodes.ActualDepthDetail]);
    }

    [Fact]
    public async Task ExportTree_NotExportableRoot_ReturnsSuccessWithEmptyMarkdown()
    {
        var harness = CreateExportHarness(withContent: false, chainLength: 1);
        var tools = CreateExportTools(harness);

        var envelope = await tools.ExportTree(RootId.ToString(), RoleDeveloper.Value);

        Assert.True(envelope.IsSuccess);
        Assert.Equal(string.Empty, envelope.Data!.Markdown);
        Assert.Null(envelope.Warnings);
    }

    [Fact]
    public async Task ExportTree_UnknownNodeId_ReturnsStableNodeNotFoundEnvelope()
    {
        var harness = CreateExportHarness(withContent: true, chainLength: 1);
        var tools = CreateExportTools(harness);

        var envelope = await tools.ExportTree(UnknownNodeId.ToString(), RoleDeveloper.Value);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(NavigationErrorCodes.NodeNotFound, envelope.Code);
        Assert.Equal(UnknownNodeId.ToString(), envelope.Details![NavigationErrorCodes.NodeIdDetail]);
        Assert.Null(envelope.Data);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task ExportTree_MalformedNodeId_IsRejectedAsInvalidNodeId(string rawNodeId)
    {
        var harness = CreateExportHarness(withContent: true, chainLength: 1);
        var tools = CreateExportTools(harness);

        var envelope = await tools.ExportTree(rawNodeId, RoleDeveloper.Value);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidNodeId, envelope.Code);
        Assert.Equal(rawNodeId, envelope.Details![NavigationErrorCodes.RootNodeIdDetail]);
    }

    [Fact]
    public async Task ExportTree_BothSelectors_ReturnsInvalidReadContext()
    {
        var harness = CreateExportHarness(withContent: true, chainLength: 1);
        var tools = CreateExportTools(harness);

        var envelope = await tools.ExportTree(
            RootId.ToString(),
            RoleDeveloper.Value,
            transactionId: "0d0b1f5a-4e12-4c1e-9f31-5d3e2a8d7b90",
            snapshotId: "100");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, envelope.Code);
    }

    [Fact]
    public async Task SearchAndExportEnvelopes_SerializeWithStableCamelCaseFieldNames()
    {
        var searchEnvelope = await CreateSearchTools(new ScriptedRetrievalRepository
        {
            Response = SearchResponse(TwoHits())
        }).Search("Auftrag");
        var exportEnvelope = await CreateExportTools(CreateExportHarness(withContent: true, chainLength: 2))
            .ExportTree(RootId.ToString(), RoleDeveloper.Value);

        var searchJson = JsonSerializer.Serialize(searchEnvelope);
        var exportJson = JsonSerializer.Serialize(exportEnvelope);

        using var search = JsonDocument.Parse(searchJson);
        Assert.Equal("Auftrag", search.RootElement.GetProperty("data").GetProperty("query").GetString());
        Assert.Equal(RootId.ToString(),
            search.RootElement.GetProperty("data").GetProperty("items")[0].GetProperty("nodeId").GetString());
        Assert.DoesNotContain("\"message\"", searchJson);

        using var export = JsonDocument.Parse(exportJson);
        Assert.True(export.RootElement.GetProperty("data").GetProperty("markdown").GetString()!
            .StartsWith("# Hauptkapitel", StringComparison.Ordinal));
        Assert.DoesNotContain("\"warnings\"", exportJson);
    }

    private static RetrievalTools CreateSearchTools(ScriptedRetrievalRepository repository)
    {
        const int searchPageSize = 2;
        const int searchMaximumPageSize = 3;
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        return new RetrievalTools(
            harness.CreateSearchService(repository, searchPageSize, searchMaximumPageSize),
            harness.CreateExportService(),
            new RetrievalPolicy
            {
                DefaultPageSize = 10,
                MaximumPageSize = 100,
                SearchPageSize = searchPageSize,
                SearchMaximumPageSize = searchMaximumPageSize,
                SnippetMaximumCharacters = 100
            });
    }

    private static RetrievalTools CreateExportTools(NavigationTestHarness harness) =>
        new(harness.CreateSearchService(new ScriptedRetrievalRepository()), harness.CreateExportService(), SmallPolicy());

    private static RetrievalPolicy SmallPolicy() => new()
    {
        DefaultPageSize = 2,
        MaximumPageSize = 3,
        SearchPageSize = 2,
        SearchMaximumPageSize = 3,
        SnippetMaximumCharacters = 100
    };

    private static NavigationTestHarness CreateExportHarness(bool withContent, int chainLength)
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        var parentIds = new List<NodeId?> { null };
        for (var depth = 1; depth <= chainLength; depth++)
        {
            var nodeId = depth == 1 ? RootId : new NodeId(
                Guid.Parse($"40000000-0000-0000-0000-{depth:D12}"));
            var title = depth == 1 ? "Hauptkapitel" : $"Unterabschnitt {depth}";
            harness.AddNode(new Node(CurrentSnapshotId, nodeId, parentIds[^1], title, null, depth, false));
            if (withContent)
            {
                harness.AddContent(new NodeContent(
                    CurrentSnapshotId, nodeId, RoleDeveloper,
                    new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent,
                    depth == 1 ? "Inhalt Hauptkapitel." : $"Inhalt Unterabschnitt {depth}.", false));
            }

            parentIds.Add(nodeId);
        }

        return harness;
    }

    private static Result<SearchRepositoryResult> SearchResponse(IReadOnlyList<SearchHit> hits) =>
        Result<SearchRepositoryResult>.Success(new SearchRepositoryResult(hits));

    private static SearchHit[] TwoHits() =>
    [
        new SearchHit(
            RootId, "Auftragserfassung", "Aufträge erfassen.", null,
            "Title", Availability.Explicit, RoleDeveloper, Freshness.Current, SortOrder: 1),
        new SearchHit(
            ChildId, "Preisfindung", "Berechnung von Preisen.", "... Auftrag ...",
            "Content", Availability.Explicit, RoleDeveloper, Freshness.Current, SortOrder: 2)
    ];

    private static SearchHit[] FiveHits() =>
        Enumerable.Range(1, 5)
            .Select(index => new SearchHit(
                new NodeId(Guid.Parse($"41000000-0000-0000-0000-{index:D12}")),
                $"Treffer {index}", null, null,
                "Title", Availability.Explicit, null, Freshness.Unknown, SortOrder: index))
            .ToArray();

    private sealed class ScriptedRetrievalRepository : IRetrievalRepository
    {
        public List<SearchRequest> Requests { get; } = [];

        public Result<SearchRepositoryResult> Response { get; init; } =
            Result<SearchRepositoryResult>.Success(new SearchRepositoryResult([]));

        public Task<Result<SearchRepositoryResult>> SearchAsync(
            SearchRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(Response);
        }
    }
}
