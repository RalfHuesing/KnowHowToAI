using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Domain.Validation;

/// <summary>Vollständiger, bereits gelesener Zustand eines zu prüfenden Working Snapshots.</summary>
public sealed record TransactionValidationRequest(
    IEnumerable<Node> Nodes,
    IEnumerable<Audience> Audiences,
    IEnumerable<AudienceResolution> AudienceResolutions,
    IEnumerable<NodeContent> Contents,
    IEnumerable<ContentDependency> Dependencies,
    QualityWarningThresholds QualityWarningThresholds,
    bool WarnOnPossibleEmbeddedHeading);
