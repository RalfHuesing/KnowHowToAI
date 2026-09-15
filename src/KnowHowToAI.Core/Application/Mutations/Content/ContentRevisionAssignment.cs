using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>
/// Normalisierter Text mit der für seine Speicherung gültigen logischen Revision.
/// </summary>
public sealed record ContentRevisionAssignment(
    string NormalizedContentMd,
    ContentRevisionId ContentRevisionId,
    bool HasNewRevision);
