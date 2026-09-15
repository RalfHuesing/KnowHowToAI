using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>
/// Beschreibt die gewünschte Sortierposition einer existierenden Node unter demselben Parent.
/// </summary>
public sealed record ReorderNodeCommand(NodeId NodeId, int SortOrder);
