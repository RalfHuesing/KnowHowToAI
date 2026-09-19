using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Navigation;
using KnowHowToAI.Server.Mcp.Mapping;
using KnowHowToAI.Server.Web.Features.Search;

namespace KnowHowToAI.Web.Tests.Features.Search;

/// <summary>
/// M3.8-T1: Prüft die fachliche Parität von UI- und MCP-Leseergebnissen für
/// Search, Snippets, Findings und Treffermetadaten gegen gemeinsame Core-Use-Cases.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SearchReadParityTests
{
    private static readonly RoleId RoleDeveloper = new("Developer");
    private static readonly RoleId RoleDefault = new("Default");

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
