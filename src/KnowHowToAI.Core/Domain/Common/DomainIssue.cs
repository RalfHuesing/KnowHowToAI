using System.Collections.ObjectModel;

namespace KnowHowToAI.Core.Domain.Common;

/// <summary>
/// Gemeinsame unveränderliche Daten eines Fachfehlers oder Qualitätshinweises.
/// </summary>
public abstract record DomainIssue
{
    protected DomainIssue(string code, string message, IReadOnlyDictionary<string, string>? details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
        Details = CopyDetails(details);
    }

    public string Code { get; }

    public string Message { get; }

    public IReadOnlyDictionary<string, string> Details { get; }

    private static IReadOnlyDictionary<string, string> CopyDetails(IReadOnlyDictionary<string, string>? details)
    {
        if (details is null || details.Count == 0)
            return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));

        var copy = new Dictionary<string, string>(details.Count, StringComparer.Ordinal);
        foreach (var (key, value) in details)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(value);
            copy.Add(key, value);
        }

        return new ReadOnlyDictionary<string, string>(copy);
    }
}
