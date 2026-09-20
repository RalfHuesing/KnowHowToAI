using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Audiences;

/// <summary>
/// Metadata eines expliziten Inhalts, ohne dessen möglicherweise große Payload zu duplizieren.
/// </summary>
public sealed record ResolvedContentMetadata(
    NodeId NodeId,
    AudienceId AudienceId,
    ContentRevisionId ContentRevisionId,
    ContentMode ContentMode,
    int ContentLength);

/// <summary>
/// Transparenter Ausgang einer Rollenauflösung; fehlende Konfiguration ist kein impliziter Fallback.
/// </summary>
public sealed record AudienceResolutionResult(
    AudienceId RequestedAudience,
    AudienceId? ResolvedAudience,
    Availability Availability,
    bool FallbackUsed,
    bool ResolutionConfigured,
    ResolvedContentMetadata? Content);
