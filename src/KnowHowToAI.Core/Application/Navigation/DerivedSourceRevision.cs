using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Metadata-first provenance of one source used by resolved derived content.
/// </summary>
public sealed record DerivedSourceRevision(
    NodeId SourceNodeId,
    AudienceId SourceAudienceId,
    ContentRevisionId StoredContentRevisionId,
    Freshness Freshness);
