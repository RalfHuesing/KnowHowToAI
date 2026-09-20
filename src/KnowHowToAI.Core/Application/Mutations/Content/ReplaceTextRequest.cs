using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Content;

/// <summary>Transportneutrale Eingabe für eine punktuelle Text-Ersetzung.</summary>
public sealed record ReplaceTextRequest(
    NodeId NodeId,
    RoleId RoleId,
    string OldText,
    string NewText,
    long? ExpectedChangeVersion = null);
