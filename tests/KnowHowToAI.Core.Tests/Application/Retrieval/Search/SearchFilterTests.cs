using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Tests.Application.Retrieval.Search;

[Trait("Category", "Unit")]
public sealed class SearchFilterTests
{
    [Fact]
    public void Matches_UsesOrWithinFacetAndAndBetweenFacets()
    {
        var developer = new RoleId("Developer");
        var shared = new RoleId("Shared");
        var filter = new SearchFilter(
            [developer, shared],
            [Availability.Explicit, Availability.Fallback],
            [Freshness.Stale],
            ["StaleDerivedContent"]);
        var matching = CreateHit(shared, Availability.Fallback, Freshness.Stale, ["StaleDerivedContent"]);
        var wrongFreshness = CreateHit(developer, Availability.Explicit, Freshness.Current, ["StaleDerivedContent"]);

        Assert.True(filter.Matches(matching));
        Assert.False(filter.Matches(wrongFreshness));
    }

    [Fact]
    public void EmptyFilter_MatchesEveryHitAndHasStableFingerprint()
    {
        var filter = new SearchFilter();

        Assert.True(filter.IsEmpty);
        Assert.True(filter.Matches(CreateHit(new RoleId("Developer"), Availability.Explicit, Freshness.Current, [])));
        Assert.Equal(filter.Fingerprint, new SearchFilter().Fingerprint);
    }

    private static SearchHit CreateHit(
        RoleId roleId,
        Availability availability,
        Freshness freshness,
        IReadOnlyList<string> findings) => new(
        new NodeId(Guid.NewGuid()), "Titel", null, null, "Title", availability, roleId, freshness, Findings: findings);
}
