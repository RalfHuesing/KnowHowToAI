using Microsoft.Extensions.Options;

namespace KnowHowToAI.Server.Configuration;

/// <summary>
/// Validiert die Protokollierungs-Konfiguration beim Start fail-fast und ohne Secrets.
/// </summary>
internal sealed class LoggingOptionsValidator : IValidateOptions<LoggingOptions>
{
    private static readonly string[] ValidMinimumLevels =
        ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"];

    public ValidateOptionsResult Validate(string? name, LoggingOptions options)
    {
        var errors = new List<string>();

        if (!ValidMinimumLevels.Contains(options.MinimumLevel, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add(
                $"Logging:MinimumLevel muss einen der Werte {string.Join(", ", ValidMinimumLevels)} haben, ist aber '{options.MinimumLevel}'.");
        }

        if (options.FilePath is { } filePath && string.IsNullOrWhiteSpace(filePath))
        {
            errors.Add("Logging:FilePath ist gesetzt, aber leer. Entferne den Schlüssel für reinen stderr-Kanal.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
