using System.Diagnostics;
using KnowHowToAI.Core.Documents;
using Microsoft.Extensions.Logging;

namespace KnowHowToAI.Core.Sync;

// Marker-Datei-geschützter Export der DB nach .md-Dateien. Der eigentliche SQL-Zugriff kommt als
// Delegate von außen (z. B. SqlDocumentsStore.GetAllAsync) — so ist die Marker-Logik ohne echten
// SQL Server testbar. Regeln: docs/04-Datenmodell-Validierung-Edgecases.md, Abschnitt 4.4.
public sealed class ExportService(
    Func<CancellationToken, Task<IReadOnlyList<Document>>> getAllAsync,
    ILogger<ExportService>? logger = null)
{
    private readonly FrontMatterParser _parser = new();

    public async Task ExportAsync(string targetDirectory, string exportMarkerFileName, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation(
            "Export startet: target='{Target}', markerFile='{MarkerFile}'",
            targetDirectory, exportMarkerFileName);
        var sw = Stopwatch.StartNew();

        PrepareTargetDirectory(targetDirectory, exportMarkerFileName);

        var documents = await getAllAsync(cancellationToken);
        foreach (var document in documents)
        {
            var filePath = Path.Combine(targetDirectory, document.Slug.Replace('/', Path.DirectorySeparatorChar) + ".md");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, _parser.Render(document), cancellationToken);
        }

        logger?.LogInformation(
            "Export abgeschlossen: {DocumentCount} Dokumente, {ElapsedMs}ms",
            documents.Count, sw.ElapsedMilliseconds);
    }

    private static void PrepareTargetDirectory(string targetDirectory, string exportMarkerFileName)
    {
        var markerPath = Path.Combine(targetDirectory, exportMarkerFileName);
        var targetHasEntries = Directory.Exists(targetDirectory) && Directory.EnumerateFileSystemEntries(targetDirectory).Any();

        if (!targetHasEntries)
        {
            Directory.CreateDirectory(targetDirectory);
            File.WriteAllText(markerPath, $$"""{"tool":"KnowHowToAI.Cli","createdAt":"{{DateTimeOffset.UtcNow:O}}"}""");
            return;
        }

        if (!File.Exists(markerPath))
        {
            throw new InvalidOperationException(
                $"Zielverzeichnis '{targetDirectory}' enthält Dateien ohne Marker-Datei '{exportMarkerFileName}' — Abbruch. " +
                "Bitte ein leeres Verzeichnis angeben oder die Marker-Datei manuell anlegen, wenn der Inhalt sicher überschrieben werden darf.");
        }

        foreach (var mdFile in Directory.EnumerateFiles(targetDirectory, "*.md", SearchOption.AllDirectories))
        {
            File.Delete(mdFile);
        }
    }
}
