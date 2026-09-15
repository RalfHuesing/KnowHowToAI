using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>
/// Beschreibt eine punktuelle Änderung des expliziten Contents einer Rolle.
/// </summary>
public sealed record ReplaceTextCommand(
    NodeId NodeId,
    RoleId RoleId,
    string OldText,
    string NewText,
    string NodeTitle,
    bool WarnOnPossibleEmbeddedHeading);
