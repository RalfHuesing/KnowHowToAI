using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>Transportneutrale Eingabe zum Ändern der Stammdaten einer Node.</summary>
public sealed record UpdateNodeRequest(
    NodeId NodeId,
    string Title,
    string? Description,
    long? ExpectedChangeVersion = null);
