using KnowHowToAI.Exploration.Reporting;
using KnowHowToAI.Server.Mcp.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Exploration;

/// <summary>
/// Haupthandwerkzeug der Szenarien: typisierter Zugriff auf die originalen (internen)
/// MCP-Tool-Klassen per DI, formatierte Envelope-Ausgabe mit Größenreport und
/// Befund-Dokumentation. <see cref="CurrentScenario"/> wird vom Runner gesetzt,
/// sodass Szenarien Befunde ohne Boilerplate journalisieren können.
/// </summary>
public sealed class ExplorationContext(IServiceProvider services, ExplorationJournal journal)
{
    public string? CurrentScenario { get; set; }

    /// <summary>Löst eine original MCP-Tool-Klasse aus dem Host-Container auf.</summary>
    public T Tools<T>() where T : class => services.GetRequiredService<T>();

    public void Info(string message) => journal.Info(CurrentScenario ?? "-", message);

    public void Improvement(string message) => journal.Improvement(CurrentScenario ?? "-", message);

    public void Bug(string message) => journal.Bug(CurrentScenario ?? "-", message);

    public void Line(string text) => Console.WriteLine(text);

    /// <summary>
    /// Serialisiert einen Envelope (lesbares JSON), prüft Parsebarkeit und berichtet
    /// die kompakte UTF-8-Größe – Näherung für die realen Token-Kosten im stdio-Betrieb.
    /// </summary>
    public void ReportEnvelope<TData>(string toolName, McpToolEnvelope<TData> envelope) where TData : class
    {
        var report = EnvelopeJson.Report(envelope);
        Line($"── {toolName} → code={envelope.Code} | {report.CompactByteLength} Bytes (kompakt, UTF-8)");
        Line(report.Json);
    }

    /// <summary>
    /// Kompaktvariante ohne JSON-Dump: Code, Größen- und Warnbericht für Massenaufrufe
    /// (z. B. Content-Import vieler Dateien).
    /// </summary>
    public void ReportEnvelopeBrief<TData>(string toolName, McpToolEnvelope<TData> envelope) where TData : class
    {
        var report = EnvelopeJson.Report(envelope);
        Line($"── {toolName} → code={envelope.Code} | {report.CompactByteLength} Bytes (kompakt, UTF-8)");
        foreach (var warning in envelope.Warnings ?? [])
            Line($"   ⚠ {warning.Code}: {warning.Message}" + (warning.Details is { Count: > 0 } details
                ? $" ({string.Join(", ", details.Select(pair => $"{pair.Key}={pair.Value}"))})"
                : string.Empty));
        if (!envelope.IsSuccess)
            Line($"   message: {envelope.Message}");
    }
}
