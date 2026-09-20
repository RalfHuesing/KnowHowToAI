using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Transactions;

/// <summary>
/// Vollständige, atomar und konsistent gelesene Sicht auf einen offenen Working Snapshot.
/// Enthält Transaktionsmetadaten, ChangeVersion und den gesamten Wissensgraphen.
/// </summary>
public sealed record WorkingSnapshotReadData(
    KnowledgeTransaction Transaction,
    long ChangeVersion,
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<Audience> Audiences,
    IReadOnlyList<AudienceResolution> AudienceResolutions,
    IReadOnlyList<NodeContent> Contents,
    IReadOnlyList<ContentDependency> Dependencies);
