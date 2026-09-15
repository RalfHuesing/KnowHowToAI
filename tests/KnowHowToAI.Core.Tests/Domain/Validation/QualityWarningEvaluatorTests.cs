using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Validation;

namespace KnowHowToAI.Core.Tests.Domain.Validation;

[Trait("Category", "Unit")]
public sealed class QualityWarningEvaluatorTests
{
    private static readonly QualityWarningThresholds Thresholds = new ValidationPolicy
    {
        ContentSizeWarningBytes = 4,
        ChildCountWarning = 2,
        HierarchyDepthWarning = 3,
        PossibleEmbeddedHeadingWarning = true
    }.ToQualityWarningThresholds();

    [Fact]
    public void EvaluateContentSize_MeasuresNormalizedUtf8BytesAndKeepsInputUnchanged()
    {
        const string content = "ä\r\nx";

        var warnings = QualityWarningEvaluator.EvaluateContentSize(content, Thresholds);

        var warning = Assert.Single(warnings);
        Assert.Equal(QualityWarningCodes.NodeTooLarge, warning.Code);
        Assert.Equal("4", warning.Details[QualityWarningCodes.ActualBytesDetail]);
        Assert.Equal("4", warning.Details[QualityWarningCodes.ThresholdBytesDetail]);
        Assert.Equal(QualityWarningCodes.CreateChildNodesRecommendation, warning.Details[QualityWarningCodes.RecommendationDetail]);
        Assert.Equal("ä\r\nx", content);
    }

    [Fact]
    public void EvaluateContentSize_BelowThreshold_ReturnsNoWarning()
    {
        var warnings = QualityWarningEvaluator.EvaluateContentSize("abc", Thresholds);

        Assert.Empty(warnings);
    }

    [Fact]
    public void EvaluateChildCount_WarnsAtThresholdAndIgnoresTombstones()
    {
        var children = new[]
        {
            Node(isDeleted: false, nodeSeed: "9f4a2c43-0a77-44be-8f98-f403444d3e9f"),
            Node(isDeleted: false, nodeSeed: "0a77f4a2-2c43-44be-8f98-f403444d3e9f"),
            Node(isDeleted: true, nodeSeed: "342c9f4a-0a77-44be-8f98-f403444d3e9f")
        };

        var warnings = QualityWarningEvaluator.EvaluateChildCount(children, Thresholds);

        var warning = Assert.Single(warnings);
        Assert.Equal(QualityWarningCodes.TooManyChildren, warning.Code);
        Assert.Equal("2", warning.Details[QualityWarningCodes.ActualCountDetail]);
        Assert.Equal("2", warning.Details[QualityWarningCodes.ThresholdCountDetail]);
    }

    [Fact]
    public void EvaluateHierarchyDepth_WarnsAtConfiguredThreshold()
    {
        var warnings = QualityWarningEvaluator.EvaluateHierarchyDepth(depth: 3, Thresholds);

        var warning = Assert.Single(warnings);
        Assert.Equal(QualityWarningCodes.HierarchyTooDeep, warning.Code);
        Assert.Equal("3", warning.Details[QualityWarningCodes.ActualDepthDetail]);
        Assert.Equal("3", warning.Details[QualityWarningCodes.ThresholdDepthDetail]);
    }

    [Fact]
    public void QualityWarnings_ProduceNoValidationErrors()
    {
        var warnings = QualityWarningEvaluator.EvaluateContentSize("ä\r\nx", Thresholds);
        var report = new ValidationReport([], warnings);

        Assert.True(report.IsValid);
        Assert.Single(report.Warnings);
    }

    private static Node Node(bool isDeleted, string nodeSeed) =>
        new(
            new SnapshotId(1),
            new NodeId(Guid.Parse(nodeSeed)),
            ParentNodeId: null,
            "Kind",
            Description: null,
            SortOrder: 0,
            IsDeleted: isDeleted);
}
