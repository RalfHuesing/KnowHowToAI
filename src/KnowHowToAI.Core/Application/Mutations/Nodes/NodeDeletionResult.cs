using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>
/// Enthält den konsistent tombstoned Node- und Content-Zustand einer globalen Löschung.
/// </summary>
public sealed record NodeDeletionResult(
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<NodeContent> Contents,
    IReadOnlyList<ContentDependency> Dependencies);
