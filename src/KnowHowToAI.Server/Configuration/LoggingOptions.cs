namespace KnowHowToAI.Server.Configuration;

/// <summary>
/// Bindbare Protokollierungs-Konfiguration. Alle Protokollausgaben gehen nach stderr;
/// optional wird zusätzlich eine rotierende Datei geschrieben. Defaults stehen in
/// appsettings.json.
/// </summary>
internal sealed record LoggingOptions
{
    public const string SectionName = "Logging";

    /// <summary>Kleinstes protokolliertes Serilog-Level (Verbose, Debug, Information, Warning, Error, Fatal).</summary>
    public string MinimumLevel { get; init; } = string.Empty;

    /// <summary>
    /// Optionale Zieldatei für zusätzliche Protokollausgaben. <c>null</c> bedeutet reinen stderr-Kanal.
    /// </summary>
    public string? FilePath { get; init; }
}
