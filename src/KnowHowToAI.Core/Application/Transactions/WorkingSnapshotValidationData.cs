using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Application.Transactions;

/// <summary>Vollständige, atomar gelesene Sicht auf einen zu validierenden Working Snapshot.</summary>
public sealed record WorkingSnapshotValidationData(
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<Audience> Audiences,
    IReadOnlyList<AudienceResolution> AudienceResolutions,
    IReadOnlyList<NodeContent> Contents,
    IReadOnlyList<ContentDependency> Dependencies,
    long ChangeVersion = 0);
