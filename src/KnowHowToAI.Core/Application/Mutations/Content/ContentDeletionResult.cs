using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>
/// Enthält den nach einer Content-Löschung atomar aktualisierten Snapshot-Ausschnitt.
/// </summary>
public sealed record ContentDeletionResult(
    NodeContent ChangedContent,
    IReadOnlyList<NodeContent> Contents,
    IReadOnlyList<ContentDependency> Dependencies);
