namespace KnowHowToAI.Core.Validation;

public sealed record ValidationError(string FilePath, string Reason);

public sealed class ValidationResult
{
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];
    public bool IsValid => Errors.Count == 0;
}
