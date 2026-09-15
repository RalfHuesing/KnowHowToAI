namespace KnowHowToAI.Core.Domain.Validation;

/// <summary>
/// Bereits validierte, konfigurierbare Grenzen für nicht blockierende Qualitätswarnungen.
/// </summary>
public sealed record QualityWarningThresholds(
    int ContentSizeWarningBytes,
    int ChildCountWarning,
    int HierarchyDepthWarning);
