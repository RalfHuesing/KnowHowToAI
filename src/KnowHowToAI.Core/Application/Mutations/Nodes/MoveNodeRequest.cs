using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>Beschreibt eine positionsgenaue Strukturänderung mit optionaler Versionsprüfung.</summary>
public sealed record MoveNodeRequest(
    NodeId NodeId,
    NodeId? ParentNodeId,
    int SortOrder,
    long? ExpectedChangeVersion = null);
