using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>Transportneutrale Eingabe für einen vollständigen Content-Replace.</summary>
public sealed record ReplaceContentRequest(
    NodeId NodeId,
    RoleId RoleId,
    ContentMode ContentMode,
    string ContentMd,
    IReadOnlyList<ContentDependencySource> Sources,
    long ExpectedChangeVersion);

/// <summary>Bezeichnet eine beim Ableiten verwendete explizite Source-Revision.</summary>
public sealed record ContentDependencySource(
    NodeId NodeId,
    RoleId RoleId,
    ContentRevisionId ContentRevisionId);
