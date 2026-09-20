using KnowHowToAI.Core.Application.Policies;

namespace KnowHowToAI.TestSupport;

public static class TestPolicies
{
    public static ValidationPolicy DefaultValidation { get; } = new()
    {
        ContentSizeWarningBytes = 4096,
        ChildCountWarning = 100,
        HierarchyDepthWarning = 8,
        PossibleEmbeddedHeadingWarning = true
    };

    public static RetrievalPolicy DefaultRetrieval { get; } = new()
    {
        DefaultPageSize = 10,
        MaximumPageSize = 100,
        SearchPageSize = 10,
        SearchMaximumPageSize = 100,
        SnippetMaximumCharacters = 100
    };
}
