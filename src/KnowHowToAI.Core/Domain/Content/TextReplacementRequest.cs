namespace KnowHowToAI.Core.Domain.Content;

/// <summary>
/// Beschreibt den punktuellen, vollständig zu validierenden Content-Ersatz.
/// </summary>
public sealed record TextReplacementRequest(
    string ExistingContent,
    string OldText,
    string NewText,
    string NodeTitle,
    bool WarnOnPossibleEmbeddedHeading);
