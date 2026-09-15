using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>
/// Beschreibt die globale Node-Löschung, optional einschließlich des gesamten Subtrees.
/// </summary>
public sealed record DeleteNodeCommand(NodeId NodeId, bool DeleteSubtree);
