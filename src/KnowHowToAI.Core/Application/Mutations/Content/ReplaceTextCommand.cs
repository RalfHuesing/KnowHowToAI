using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>
/// Beschreibt eine punktuelle Änderung des expliziten Contents einer Zielgruppe.
/// </summary>
public sealed record ReplaceTextCommand(
    NodeId NodeId,
    AudienceId AudienceId,
    string OldText,
    string NewText,
    string NodeTitle,
    bool WarnOnPossibleEmbeddedHeading);
