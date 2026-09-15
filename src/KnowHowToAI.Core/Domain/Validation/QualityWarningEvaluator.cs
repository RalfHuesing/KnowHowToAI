using System.Globalization;
using System.Text;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.Core.Domain.Validation;

/// <summary>
/// Erzeugt nicht blockierende Qualitätswarnungen für bereits fachlich gültigen Wissensinhalt.
/// </summary>
public static class QualityWarningEvaluator
{
    public static IReadOnlyList<DomainWarning> EvaluateContentSize(
        string content,
        QualityWarningThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(thresholds);

        var normalizedContent = ContentNormalizer.Normalize(content);
        var actualBytes = Encoding.UTF8.GetByteCount(normalizedContent);
        if (actualBytes < thresholds.ContentSizeWarningBytes)
            return [];

        return [new DomainWarning(
            QualityWarningCodes.NodeTooLarge,
            "Der Content überschreitet die konfigurierte Größenwarnschwelle und sollte in Child-Nodes gegliedert werden.",
            new Dictionary<string, string>
            {
                [QualityWarningCodes.ActualBytesDetail] = actualBytes.ToString(CultureInfo.InvariantCulture),
                [QualityWarningCodes.ThresholdBytesDetail] = thresholds.ContentSizeWarningBytes.ToString(CultureInfo.InvariantCulture),
                [QualityWarningCodes.RecommendationDetail] = QualityWarningCodes.CreateChildNodesRecommendation
            })];
    }

    public static IReadOnlyList<DomainWarning> EvaluateChildCount(
        IEnumerable<Node> children,
        QualityWarningThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(children);
        ArgumentNullException.ThrowIfNull(thresholds);

        var actualCount = children.Count(child => !child.IsDeleted);
        if (actualCount < thresholds.ChildCountWarning)
            return [];

        return [new DomainWarning(
            QualityWarningCodes.TooManyChildren,
            "Der Node hat ungewöhnlich viele aktive direkte Kinder.",
            new Dictionary<string, string>
            {
                [QualityWarningCodes.ActualCountDetail] = actualCount.ToString(CultureInfo.InvariantCulture),
                [QualityWarningCodes.ThresholdCountDetail] = thresholds.ChildCountWarning.ToString(CultureInfo.InvariantCulture)
            })];
    }

    public static IReadOnlyList<DomainWarning> EvaluateHierarchyDepth(
        int depth,
        QualityWarningThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);

        if (depth < thresholds.HierarchyDepthWarning)
            return [];

        return [new DomainWarning(
            QualityWarningCodes.HierarchyTooDeep,
            "Der Node liegt ungewöhnlich tief in der Hierarchie.",
            new Dictionary<string, string>
            {
                [QualityWarningCodes.ActualDepthDetail] = depth.ToString(CultureInfo.InvariantCulture),
                [QualityWarningCodes.ThresholdDepthDetail] = thresholds.HierarchyDepthWarning.ToString(CultureInfo.InvariantCulture)
            })];
    }
}
