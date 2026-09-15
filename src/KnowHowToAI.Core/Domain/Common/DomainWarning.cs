namespace KnowHowToAI.Core.Domain.Common;

/// <summary>
/// Maschinenlesbarer Qualitätshinweis, der eine fachlich gültige Mutation nicht verhindert.
/// </summary>
public sealed record DomainWarning : DomainIssue
{
    public DomainWarning(string code, string message, IReadOnlyDictionary<string, string>? details = null)
        : base(code, message, details)
    {
    }
}
