using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>Transportneutrale Eingabe zum Anlegen einer Node in einer Transaction.</summary>
public sealed record CreateNodeRequest(
    NodeId? ParentNodeId,
    string Title,
    string? Description,
    int SortOrder);
