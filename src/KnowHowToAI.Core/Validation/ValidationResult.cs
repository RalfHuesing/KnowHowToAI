namespace KnowHowToAI.Core.Validation;

public sealed record ValidationError(string FilePath, string Reason);

public sealed record ValidationResult(
    IReadOnlyList<ValidationError> Errors,
    IReadOnlyList<ValidationError> Warnings)
{
    public bool IsValid => Errors.Count == 0;
}
