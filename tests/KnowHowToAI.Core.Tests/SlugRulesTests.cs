using KnowHowToAI.Core.Documents;

namespace KnowHowToAI.Core.Tests;

public class SlugRulesTests
{
    [Theory]
    [InlineData("it")]
    [InlineData("it/netzwerk-routing")]
    [InlineData("core-switch-01")]
    public void IsValidSlug_AcceptsCompliantSlugs(string slug)
    {
        Assert.True(SlugRules.IsValidSlug(slug));
    }

    [Theory]
    [InlineData("It")]
    [InlineData("it/Netzwerk")]
    [InlineData("änderung")]
    [InlineData("it netzwerk")]
    [InlineData("it_netzwerk")]
    [InlineData("")]
    public void IsValidSlug_RejectsNonCompliantSlugs(string slug)
    {
        Assert.False(SlugRules.IsValidSlug(slug));
    }

    [Fact]
    public void GetParentSlug_ReturnsNullForRootSlug()
    {
        Assert.Null(SlugRules.GetParentSlug("it"));
    }

    [Fact]
    public void GetParentSlug_ReturnsParentForNestedSlug()
    {
        Assert.Equal("it/netzwerk", SlugRules.GetParentSlug("it/netzwerk/routing"));
    }
}
