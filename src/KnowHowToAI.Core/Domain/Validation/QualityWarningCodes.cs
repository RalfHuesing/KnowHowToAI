namespace KnowHowToAI.Core.Domain.Validation;

public static class QualityWarningCodes
{
    public const string NodeTooLarge = "NodeTooLarge";
    public const string TooManyChildren = "TooManyChildren";
    public const string HierarchyTooDeep = "HierarchyTooDeep";

    public const string ActualBytesDetail = "actualBytes";
    public const string ThresholdBytesDetail = "thresholdBytes";
    public const string ActualCountDetail = "actualCount";
    public const string ThresholdCountDetail = "thresholdCount";
    public const string ActualDepthDetail = "actualDepth";
    public const string ThresholdDepthDetail = "thresholdDepth";
    public const string RecommendationDetail = "recommendation";
    public const string CreateChildNodesRecommendation = "CreateChildNodes";
}
