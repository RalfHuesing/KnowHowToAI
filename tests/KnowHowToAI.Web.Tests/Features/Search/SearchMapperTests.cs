using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Features.Search;

namespace KnowHowToAI.Web.Tests.Features.Search;

[Trait("Category", "Unit")]
public sealed class SearchMapperTests
{
    [Fact]
    public void ToSearchHitViewModel_MapsAllProperties()
    {
        var nodeId = new NodeId(Guid.NewGuid());
        var resolvedAudienceId = new AudienceId("architect");

        var hit = new SearchHit(
            nodeId,
            "Schnittstellen",
            "API-Dokumentation",
            "...gefundener **Text**...",
            "Content",
            Availability.Explicit,
            resolvedAudienceId,
            Freshness.Current,
            SortOrder: 3,
            Findings: ["StaleDerivedContent"]);

        var vm = SearchMapper.ToSearchHitViewModel(hit);

        Assert.Equal(nodeId.Value, vm.NodeId);
        Assert.Equal("Schnittstellen", vm.Title);
        Assert.Equal("API-Dokumentation", vm.Description);
        Assert.Equal("...gefundener **Text**...", vm.Snippet);
        Assert.Equal("Content", vm.HitField);
        Assert.Equal("Explicit", vm.Availability);
        Assert.Equal(resolvedAudienceId.Value, vm.ResolvedAudienceId);
        Assert.Equal("Current", vm.Freshness);
        Assert.Equal(3, vm.SortOrder);
        Assert.Equal(["Schnittstellen"], vm.Breadcrumb);
        Assert.Equal(["StaleDerivedContent"], vm.Findings);
    }

    [Fact]
    public void ToSearchPageViewModel_MapsQueryHitsAndPreservesCursor()
    {
        var nodeId = new NodeId(Guid.NewGuid());
        var hit = new SearchHit(
            nodeId,
            "Titel",
            null,
            "Snippet",
            "Title",
            Availability.None,
            null,
            Freshness.Current);

        var page = new SearchResultPage("Suchbegriff", new[] { hit }, NextCursor: "search-cursor-abc");

        var vm = SearchMapper.ToSearchPageViewModel(page);

        Assert.Equal("Suchbegriff", vm.Query);
        Assert.Equal("search-cursor-abc", vm.NextCursor);
        var item = Assert.Single(vm.Items);
        Assert.Equal(nodeId.Value, item.NodeId);
    }

    [Fact]
    public void ToSearchPageResult_PreservesErrorsAndWarnings()
    {
        var error = new DomainError("InvalidCursor", "Cursor ungültig.");
        var warning = new DomainWarning("SearchWarn", "Suchwarnung.");

        var failed = Result<SearchResultPage>.Failure(error, new[] { warning });
        var result = SearchMapper.ToSearchPageResult(failed);

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidCursor", result.Error!.Code);
        Assert.Single(result.Warnings);
    }
}
