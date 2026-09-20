using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>
/// Beschreibt die Tombstone-Löschung des expliziten Contents einer Zielgruppe.
/// </summary>
public sealed record DeleteContentCommand(NodeId NodeId, AudienceId AudienceId);
