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

    [Theory]
    [InlineData("docs", "foo.md", "foo")]
    [InlineData("docs", "sub/bar.md", "sub/bar")]
    [InlineData("docs", "sub/nested/baz.md", "sub/nested/baz")]
    [InlineData("docs", "It.md", "It")]
    public void FromFilePath_StripsRootAndExtension(string root, string relativePath, string expected)
    {
        var fullPath = Path.Combine(root, relativePath);
        Assert.Equal(expected, SlugRules.FromFilePath(root, fullPath));
    }

    [Fact]
    public void FromFilePath_FileOutsideRoot_ProducesRelativeEscape()
    {
        var root = Path.Combine("docs");
        var foreign = Path.Combine("etc", "passwd.md");

        var slug = SlugRules.FromFilePath(root, foreign);

        // FromFilePath normalisiert auf Forward-Slashes (Slug-Konvention).
        Assert.Equal("../etc/passwd", slug);
    }
}
