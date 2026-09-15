using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>
/// Enthält den geänderten Content und den vollständig aktualisierten Snapshot-Ausschnitt.
/// </summary>
public sealed record ContentMutationResult(NodeContent ChangedContent, IReadOnlyList<NodeContent> Contents);
