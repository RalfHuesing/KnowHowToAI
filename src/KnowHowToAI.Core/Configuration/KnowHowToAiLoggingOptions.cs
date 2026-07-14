namespace KnowHowToAI.Core.Configuration;

// Bindung an den "KnowHowToAi:Logging"-Abschnitt in appsettings.json.
// Werte entsprechen Serilog.Events.LogEventLevel bzw. Serilog.RollingInterval als String.
public sealed record KnowHowToAiLoggingOptions
{
    public string MinimumLevel { get; init; } = "Information";
    public string RollingInterval { get; init; } = "Day";
    public int RetainedFileCountLimit { get; init; } = 14;
}
