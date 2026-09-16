using Serilog.Core;
using Serilog.Events;

namespace KnowHowToAI.Server.Hosting;

/// <summary>
/// Ersetzt in jedem Protokoll-Event strukturierte Eigenschaften mit sensitivem Namen
/// (Geheimnisse, Verbindungszeichenfolgen, vollständige Content-Payloads) durch einen
/// Platzhalter, bevor ein Sink schreibt. Nachrichtentexte werden nicht nachträglich
/// durchsucht; Call-Sites reichen sensitive Werte ausschließlich als strukturierte
/// Eigenschaft mit sensitivem Namen weiter.
/// </summary>
internal sealed class SecretRedactionEnricher : ILogEventEnricher
{
    public const string RedactedValue = "<redacted>";

    private static readonly string[] ExactSensitiveNames =
        ["Password", "Secret", "Token", "Credential", "Credentials", "ConnectionString", "Content", "ContentMd"];

    private static readonly string[] SensitiveNameSuffixes = ["Password", "Secret", "ConnectionString"];

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var propertyName in logEvent.Properties.Keys.ToArray())
        {
            if (IsSensitive(propertyName))
            {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(propertyName, RedactedValue));
            }
        }
    }

    public static bool IsSensitive(string propertyName) =>
        ExactSensitiveNames.Contains(propertyName, StringComparer.Ordinal)
        || SensitiveNameSuffixes.Any(suffix => propertyName.EndsWith(suffix, StringComparison.Ordinal));
}
