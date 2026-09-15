using System.Collections.ObjectModel;

namespace KnowHowToAI.Core.Domain.Common;

/// <summary>
/// Transportneutraler Ergebnisvertrag für erwartete Fachfehler.
/// </summary>
public sealed record Result<T>
{
    private static readonly IReadOnlyDictionary<string, string> EmptyDetails =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));

    private Result(T? value, DomainError? error, IReadOnlyList<DomainWarning> warnings)
    {
        Value = value;
        Error = error;
        Warnings = warnings;
    }

    public T? Value { get; }

    public DomainError? Error { get; }

    public bool IsSuccess => Error is null;

    public string Code => Error?.Code ?? ResultCodes.Success;

    public string? Message => Error?.Message;

    public IReadOnlyDictionary<string, string> Details => Error?.Details ?? EmptyDetails;

    public IReadOnlyList<DomainWarning> Warnings { get; }

    public static Result<T> Success(T? value, IEnumerable<DomainWarning>? warnings = null) =>
        new(value, error: null, CopyWarnings(warnings));

    public static Result<T> Failure(DomainError error, IEnumerable<DomainWarning>? warnings = null)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(default, error, CopyWarnings(warnings));
    }

    private static IReadOnlyList<DomainWarning> CopyWarnings(IEnumerable<DomainWarning>? warnings) =>
        Array.AsReadOnly(warnings?.ToArray() ?? []);
}
