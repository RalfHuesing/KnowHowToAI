using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Navigation;
using KnowHowToAI.Server.Mcp.Mapping;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Search;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Search;

/// <summary>
/// M3.8-T1: Prüft die fachliche Parität von UI- und MCP-Leseergebnissen für
/// Search, Snippets, Findings und Treffermetadaten gegen gemeinsame Core-Use-Cases.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SearchReadParityTests : BunitContext
{
    private static readonly SnapshotId SnapshotId = new(1);
    private static readonly RoleId RoleDeveloper = new("Developer");
    private static readonly RoleId RoleDefault = new("Default");

    private sealed class FakeReleaseRepository : IReleaseRepository
    {
        public Task<Release?> FindAsync(ReleaseId releaseId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Release?>(null);
    }

    [Fact]
    public async Task Search_CommonUseCaseResult_ReachesSearchPageAndMcpContract()
    {
        var rootNodeId = new NodeId(Guid.Parse("20000000-0000-0000-0000-000000000010"));
        var hitNodeId = new NodeId(Guid.Parse("20000000-0000-0000-0000-000000000011"));
        var harness = new NavigationTestHarness(SnapshotId);
        harness.AddNode(new Node(SnapshotId, rootNodeId, null, "Wissensbasis", null, 0, false));
        harness.AddNode(new Node(SnapshotId, hitNodeId, rootNodeId, "Produktpfad", "Parität", 1, false));
        var retrieval = new InMemoryRetrievalRepository(SnapshotId);
        retrieval.ConfigureActiveRole(RoleDeveloper);
        retrieval.ResultsToReturn =
        [
            new SearchHit(
                hitNodeId,
                "Produktpfad",
                "Parität",
                "Gemeinsamer Suchtreffer",
                "Content",
                Availability.Explicit,
                RoleDeveloper,
                Freshness.Current,
                1,
                ["VerifiedFinding"])
        ];
        var navigationService = harness.CreateService();
        var searchService = harness.CreateSearchService(retrieval);
        var query = new SearchQuery("Gemeinsam", RoleId: RoleDeveloper);
        var applicationResult = await searchService.SearchAsync(query, new ReadContext());
        var mcp = McpRetrievalMapper.ToEnvelope(applicationResult).Data!;

        Services.AddSingleton(navigationService);
        Services.AddSingleton(searchService);
        Services.AddSingleton(new WorkspaceState());
        Services.AddSingleton(new PageRegionState());
        Services.AddSingleton<IWebReadContextResolver>(new WebReadContextResolver(new FakeReleaseRepository(), harness.CreateRepositories().Transactions));
        Services.AddSingleton<IRoleStorageService>(new InMemoryRoleStorageService(RoleDeveloper.Value));
        Services.AddSingleton(new ContextSelectorState());
        Services.AddSingleton<IContextSelectionRoleCatalog>(new ContextSelectionRoleCatalog(navigationService));
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/search?roleId={RoleDeveloper.Value}");

        var cut = Render<SearchPage>();
        await cut.InvokeAsync(() => cut.Find("[data-testid='search-text']").Change(query.Text));
        await cut.InvokeAsync(() => cut.Find("[data-testid='search-submit']").Click());

        var mcpHit = Assert.Single(mcp.Items);
        cut.WaitForAssertion(() =>
        {
            var renderedHit = cut.Find($"[data-testid='search-result-{mcpHit.NodeId}']").TextContent;
            Assert.Contains(mcpHit.Title, renderedHit, StringComparison.Ordinal);
            Assert.Contains(mcpHit.Snippet!, renderedHit, StringComparison.Ordinal);
            Assert.Contains("Wissensbasis › Produktpfad", renderedHit, StringComparison.Ordinal);
            Assert.Contains("VerifiedFinding", renderedHit, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void SearchResults_UiAndMcpShowIdenticalHitsAndMetadata()
    {
        var node1Id = new NodeId(Guid.Parse("20000000-0000-0000-0000-000000000001"));
        var node2Id = new NodeId(Guid.Parse("20000000-0000-0000-0000-000000000002"));
        var node3Id = new NodeId(Guid.Parse("20000000-0000-0000-0000-000000000003"));

        var hits = new List<SearchHit>
        {
            new(
                node1Id,
                "Architektur-Leitfaden",
                "Systembausteine und Schichtung",
                "...gefundener **Architektur**-Text...",
                HitField: "Content",
                Availability.Explicit,
                RoleDeveloper,
                Freshness.Current,
                SortOrder: 1),
            new(
                node2Id,
                "API-Referenz",
                "Endpunkte und Verträge",
                "...gefundene **API**-Übersicht...",
                HitField: "Title",
                Availability.Fallback,
                RoleDefault,
                Freshness.Stale,
                SortOrder: 2),
            new(
                node3Id,
                "Betriebsleitfaden",
                "Deployment und Logging",
                "...gefundener **Betrieb**-Auszug...",
                HitField: "Description",
                Availability.None,
                ResolvedRoleId: null,
                Freshness.Current,
                SortOrder: 3)
        };

        var page = new SearchResultPage("Suchanfrage", hits, NextCursor: "search-cursor-xyz");
        var result = Result<SearchResultPage>.Success(page);

        // MCP-Mapping
        var mcpEnvelope = McpRetrievalMapper.ToEnvelope(result);
        Assert.True(mcpEnvelope.IsSuccess);
        var mcpData = mcpEnvelope.Data!;

        // UI-Mapping
        var uiVm = SearchMapper.ToSearchPageViewModel(page);

        // Paritäts-Prüfung
        Assert.Equal(mcpData.Query, uiVm.Query);
        Assert.Equal(mcpData.NextCursor, uiVm.NextCursor);
        Assert.Equal(mcpData.Items.Count, uiVm.Items.Count);

        for (var i = 0; i < hits.Count; i++)
        {
            var mcpHit = mcpData.Items[i];
            var uiHit = uiVm.Items[i];

            Assert.Equal(mcpHit.NodeId, uiHit.NodeId.ToString("D"));
            Assert.Equal(mcpHit.Title, uiHit.Title);
            Assert.Equal(mcpHit.Description, uiHit.Description);
            Assert.Equal(mcpHit.Snippet, uiHit.Snippet);
            Assert.Equal(mcpHit.HitField, uiHit.HitField);
            Assert.Equal(mcpHit.Availability, uiHit.Availability);
            Assert.Equal(mcpHit.ResolvedRole, uiHit.ResolvedRoleId);
            Assert.Equal(mcpHit.Freshness, uiHit.Freshness);
        }
    }

    [Fact]
    public void SearchResults_WithFindingsAndWarnings_UiAndMcpPreserveAllDiagnostics()
    {
        var nodeId = new NodeId(Guid.Parse("20000000-0000-0000-0000-000000000001"));
        var hit = new SearchHit(
            nodeId,
            "Diagnose-Node",
            "Mit Findings",
            "Snippet",
            HitField: "Content",
            Availability.Explicit,
            RoleDeveloper,
            Freshness.Stale,
            SortOrder: 1,
            Findings: ["StaleDerivedContent"]);

        var page = new SearchResultPage("Diagnose", [hit], NextCursor: null);
        var warning = new DomainWarning("SearchQuality", "Suchwarnung bezüglich Trefferqualität.");
        var result = Result<SearchResultPage>.Success(page, [warning]);

        // MCP-Mapping
        var mcpEnvelope = McpRetrievalMapper.ToEnvelope(result);
        Assert.True(mcpEnvelope.IsSuccess);

        // UI-Mapping
        var uiResult = SearchMapper.ToSearchPageResult(result);
        Assert.True(uiResult.IsSuccess);

        // Warnungen müssen in beiden Transportmodellen vorhanden sein
        Assert.NotNull(mcpEnvelope.Warnings);
        var mcpWarning = Assert.Single(mcpEnvelope.Warnings);
        Assert.Equal("SearchQuality", mcpWarning.Code);
        Assert.Equal(warning.Message, mcpWarning.Message);

        var uiWarning = Assert.Single(uiResult.Warnings);
        Assert.Equal("SearchQuality", uiWarning.Code);
        Assert.Equal(warning.Message, uiWarning.Message);

        // UI bewahrt zusätzlich strukturierte Findings für die Darstellung auf
        Assert.Equal(["StaleDerivedContent"], uiResult.Value!.Items[0].Findings);
    }

    [Fact]
    public void SearchError_InvalidCursor_UiAndMcpPreserveErrorCode()
    {
        var error = new DomainError("InvalidCursor", "Der übergebene Cursor ist ungültig oder abgelaufen.");
        var failedResult = Result<SearchResultPage>.Failure(error);

        var mcpEnvelope = McpRetrievalMapper.ToEnvelope(failedResult);
        var uiResult = SearchMapper.ToSearchPageResult(failedResult);

        Assert.False(mcpEnvelope.IsSuccess);
        Assert.Equal("InvalidCursor", mcpEnvelope.Code);
        Assert.False(uiResult.IsSuccess);
        Assert.Equal("InvalidCursor", uiResult.Error!.Code);
    }
}
