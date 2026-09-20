using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Domain.Audiences;

/// <summary>
/// Der vollständige, auf einen Snapshot und eine Node begrenzte Eingabekontext der Rollenauflösung.
/// </summary>
public sealed record AudienceResolutionRequest(
    SnapshotId SnapshotId,
    NodeId NodeId,
    AudienceId RequestedAudience,
    IEnumerable<Audience> Audiences,
    IEnumerable<AudienceResolution> ResolutionOrder,
    IEnumerable<NodeContent> Contents);
