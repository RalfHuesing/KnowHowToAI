using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>Enthält den gespeicherten Content samt Revisions- und Freshness-Metadaten.</summary>
public sealed record ContentMutationUseCaseResult(
    NodeContent Content,
    SnapshotId SnapshotId,
    long ChangeVersion,
    Freshness Freshness);
