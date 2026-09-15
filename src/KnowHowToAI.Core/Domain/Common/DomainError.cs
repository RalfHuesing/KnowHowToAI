namespace KnowHowToAI.Core.Domain.Common;

/// <summary>
/// Stabile, maschinenlesbare Beschreibung eines erwarteten Fachfehlers.
/// </summary>
public sealed record DomainError : DomainIssue
{
    public DomainError(string code, string message, IReadOnlyDictionary<string, string>? details = null)
        : base(code, message, details)
    {
    }
}
