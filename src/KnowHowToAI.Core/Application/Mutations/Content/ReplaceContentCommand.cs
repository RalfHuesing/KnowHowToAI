using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>Beschreibt den vollständigen Ersatz eines expliziten Zielgruppen-Contents.</summary>
public sealed record ReplaceContentCommand(
    SnapshotId SnapshotId,
    NodeId NodeId,
    AudienceId AudienceId,
    ContentMode ContentMode,
    string ContentMd,
    IReadOnlyList<ContentDependency> Dependencies,
    string NodeTitle,
    bool WarnOnPossibleEmbeddedHeading);
