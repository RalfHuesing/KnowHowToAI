using System.Text.Json;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// Schreibt persistente Mess- und Abnahmeartefakte (bewusst nicht disposable:
/// die Berichte dokumentieren Messwerte über den Testlauf hinaus) als
/// formatiertes JSON nach <c>temp/&lt;dateiname&gt;</c> im Repository und
/// liefert den Pfad zur Kontrolle im Test.
/// </summary>
public static class TestMeasurementReports
{
    private static readonly JsonSerializerOptions ReportOptions = new() { WriteIndented = true };

    public static string WriteJson(string fileName, object report)
    {
        var reportPath = System.IO.Path.Combine(
            TestRepositoryRoot.Resolve(),
            "temp",
            fileName);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, ReportOptions));
        return reportPath;
    }
}
