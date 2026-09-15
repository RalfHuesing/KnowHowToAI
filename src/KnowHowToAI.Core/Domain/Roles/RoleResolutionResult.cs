using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Roles;

/// <summary>
/// Metadata eines expliziten Inhalts, ohne dessen möglicherweise große Payload zu duplizieren.
/// </summary>
public sealed record ResolvedContentMetadata(
    NodeId NodeId,
    RoleId RoleId,
    ContentRevisionId ContentRevisionId,
    ContentMode ContentMode,
    int ContentLength);

/// <summary>
/// Transparenter Ausgang einer Rollenauflösung; fehlende Konfiguration ist kein impliziter Fallback.
/// </summary>
public sealed record RoleResolutionResult(
    RoleId RequestedRole,
    RoleId? ResolvedRole,
    Availability Availability,
    bool FallbackUsed,
    bool ResolutionConfigured,
    ResolvedContentMetadata? Content);
