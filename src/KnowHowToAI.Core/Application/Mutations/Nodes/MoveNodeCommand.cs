using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>
/// Beschreibt die neue Parent-Beziehung und Position einer existierenden Node.
/// </summary>
public sealed record MoveNodeCommand(NodeId NodeId, NodeId? ParentNodeId, int SortOrder);
