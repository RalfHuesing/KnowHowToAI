using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>Ändert ausschließlich die globalen Navigationsmetadaten einer Node.</summary>
public sealed record UpdateNodeCommand
{
    public NodeId NodeId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }
}
