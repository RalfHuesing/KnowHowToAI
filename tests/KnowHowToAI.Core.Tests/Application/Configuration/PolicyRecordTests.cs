using KnowHowToAI.Core.Application.Policies;

namespace KnowHowToAI.Core.Tests.Application.Configuration;

/// <summary>
/// Unit-Tests für ValidationPolicy und RetrievalPolicy.
/// Belegen, dass die Records korrekte Feldinitialiserung und Immutability aufweisen.
/// Policy-Records erhalten ihre Werte ausschließlich injiziert; kein IConfiguration-Zugriff.
/// </summary>
[Trait("Category", "Unit")]
public sealed class PolicyRecordTests
{
    // ---------------------------------------------------------------------------
    // ValidationPolicy
    // ---------------------------------------------------------------------------

    [Fact]
    public void ValidationPolicy_DefaultInitialization_AllFieldsAccessible()
    {
        var policy = new ValidationPolicy
        {
            ContentSizeWarningBytes = 4096,
            ChildCountWarning = 25,
            HierarchyDepthWarning = 8,
            PossibleEmbeddedHeadingWarning = true
        };

        Assert.Equal(4096, policy.ContentSizeWarningBytes);
        Assert.Equal(25, policy.ChildCountWarning);
        Assert.Equal(8, policy.HierarchyDepthWarning);
        Assert.True(policy.PossibleEmbeddedHeadingWarning);
    }

    [Fact]
    public void ValidationPolicy_WithCustomValues_AllFieldsReflected()
    {
        var policy = new ValidationPolicy
        {
            ContentSizeWarningBytes = 8192,
            ChildCountWarning = 50,
            HierarchyDepthWarning = 12,
            PossibleEmbeddedHeadingWarning = false
        };

        Assert.Equal(8192, policy.ContentSizeWarningBytes);
        Assert.Equal(50, policy.ChildCountWarning);
        Assert.Equal(12, policy.HierarchyDepthWarning);
        Assert.False(policy.PossibleEmbeddedHeadingWarning);
    }

    [Fact]
    public void ValidationPolicy_IsImmutable_WithEquality()
    {
        var a = new ValidationPolicy { ContentSizeWarningBytes = 4096, ChildCountWarning = 25, HierarchyDepthWarning = 8, PossibleEmbeddedHeadingWarning = true };
        var b = new ValidationPolicy { ContentSizeWarningBytes = 4096, ChildCountWarning = 25, HierarchyDepthWarning = 8, PossibleEmbeddedHeadingWarning = true };

        Assert.Equal(a, b);
    }

    // ---------------------------------------------------------------------------
    // RetrievalPolicy
    // ---------------------------------------------------------------------------

    [Fact]
    public void RetrievalPolicy_DefaultInitialization_AllFieldsAccessible()
    {
        var policy = new RetrievalPolicy
        {
            DefaultPageSize = 20,
            MaximumPageSize = 100,
            SearchPageSize = 10,
            SearchMaximumPageSize = 50,
            SnippetMaximumCharacters = 300
        };

        Assert.Equal(20, policy.DefaultPageSize);
        Assert.Equal(100, policy.MaximumPageSize);
        Assert.Equal(10, policy.SearchPageSize);
        Assert.Equal(50, policy.SearchMaximumPageSize);
        Assert.Equal(300, policy.SnippetMaximumCharacters);
    }

    [Fact]
    public void RetrievalPolicy_IsImmutable_WithEquality()
    {
        var a = new RetrievalPolicy { DefaultPageSize = 20, MaximumPageSize = 100, SearchPageSize = 10, SearchMaximumPageSize = 50, SnippetMaximumCharacters = 300 };
        var b = new RetrievalPolicy { DefaultPageSize = 20, MaximumPageSize = 100, SearchPageSize = 10, SearchMaximumPageSize = 50, SnippetMaximumCharacters = 300 };

        Assert.Equal(a, b);
    }

    [Fact]
    public void RetrievalPolicy_WithDifferentValues_NotEqual()
    {
        var a = new RetrievalPolicy { DefaultPageSize = 20, MaximumPageSize = 100, SearchPageSize = 10, SearchMaximumPageSize = 50, SnippetMaximumCharacters = 300 };
        var b = a with { MaximumPageSize = 200 };

        Assert.NotEqual(a, b);
    }
}
