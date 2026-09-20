using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Einheitliche Snapshot-Read-Sicht des gemeinsamen Laders: aufgelöster Read-Kontext
/// plus die fünf Listen-Reads (Hierarchy, Audiences, Resolutions, Contents, Dependencies)
/// mit den per ActiveReadFilter gefilterten Active-Daten (Nodes und Contents).
/// </summary>
public sealed record SnapshotReadData(
    ResolvedReadContext Context,
    SnapshotId SnapshotId,
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<Audience> Audiences,
    IReadOnlyList<AudienceResolution> Resolutions,
    IReadOnlyList<NodeContent> Contents,
    IReadOnlyList<ContentDependency> Dependencies,
    long? ChangeVersion = null);
