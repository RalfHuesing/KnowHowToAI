namespace KnowHowToAI.Core.Domain.Content;

/// <summary>
/// Ergebnis der fachlichen Entscheidung über die Textrevision eines expliziten Contents.
/// </summary>
public sealed record ContentRevisionDecision(string NormalizedContentMd, bool RequiresNewRevision);
