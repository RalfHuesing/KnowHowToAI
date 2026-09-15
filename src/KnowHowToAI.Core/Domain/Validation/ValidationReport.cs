using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Validation;

/// <summary>
/// Unveränderlicher Bericht aus harten Fachfehlern und nicht blockierenden Qualitätshinweisen.
/// </summary>
public sealed record ValidationReport
{
    public ValidationReport(IEnumerable<DomainError> errors, IEnumerable<DomainWarning> warnings)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(warnings);

        Errors = Array.AsReadOnly(errors.ToArray());
        Warnings = Array.AsReadOnly(warnings.ToArray());
    }

    public IReadOnlyList<DomainError> Errors { get; }

    public IReadOnlyList<DomainWarning> Warnings { get; }

    public bool IsValid => Errors.Count == 0;
}
