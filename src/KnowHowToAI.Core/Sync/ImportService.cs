using System.Diagnostics;
using KnowHowToAI.Core.Documents;
using KnowHowToAI.Core.Validation;
using Microsoft.Extensions.Logging;

namespace KnowHowToAI.Core.Sync;

// Validate + Wipe-and-Dump. Der eigentliche SQL-Zugriff kommt als Delegate von außen (z. B.
// SqlDocumentsStore.ReplaceAllAsync) — so ist die Orchestrierung ohne echten SQL Server testbar.
// Schema-Migration läuft VOR diesem Aufruf in der Cli-Schicht (siehe docs/03, Abschnitt 3).
public sealed class ImportService(
    Func<IReadOnlyList<Document>, CancellationToken, Task> replaceAllAsync,
    int maxContentLengthWarning = 8000,
    ILogger<ImportService>? logger = null)
{
    private readonly DocsValidator _validator = new(maxContentLengthWarning);

    // Gibt bei Validierungsfehlern die ValidationResult mit den Fehlern zurück, ohne replaceAllAsync
    // aufzurufen. Bei Erfolg eine ValidationResult ohne Fehler, nachdem der Import durchgelaufen ist.
    public async Task<ValidationResult> ImportAsync(string docsRootPath, CancellationToken cancellationToken = default)
    {
        logger?.LogInformation("Import startet: docsRoot='{DocsRoot}'", docsRootPath);
        var sw = Stopwatch.StartNew();
        var validationResult = _validator.Validate(docsRootPath);
        if (!validationResult.IsValid)
        {
            logger?.LogInformation(
                "Import abgeschlossen (Validation fehlgeschlagen): {ErrorCount} Fehler, {WarningCount} Warnungen, {ElapsedMs}ms",
                validationResult.Errors.Count, validationResult.Warnings.Count, sw.ElapsedMilliseconds);
            return validationResult;
        }

        var documents = await ReadDocumentsAsync(docsRootPath, cancellationToken);
        await replaceAllAsync(documents, cancellationToken);

        logger?.LogInformation(
            "Import abgeschlossen: {ErrorCount} Fehler, {WarningCount} Warnungen, {ElapsedMs}ms",
            validationResult.Errors.Count, validationResult.Warnings.Count, sw.ElapsedMilliseconds);
        return validationResult;
    }

    // async File-IO verhindert Thread-Block bei großen Dateien. Directory.EnumerateFiles
    // wird mit ToList() materialisiert, weil der Enumerator sonst während des await blockiert
    // wäre; bei sehr großen docs-roots den Materialisierungs-Schritt ggf. später durch
    // Channel-Reader ersetzen (Backlog).
    private async Task<IReadOnlyList<Document>> ReadDocumentsAsync(string docsRootPath, CancellationToken cancellationToken)
    {
        var filePaths = Directory.EnumerateFiles(docsRootPath, "*.md", SearchOption.AllDirectories).ToList();
        var documents = new List<Document>(filePaths.Count);
        foreach (var filePath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var slug = SlugRules.FromFilePath(docsRootPath, filePath);
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            documents.Add(FrontMatterParser.Parse(slug, content));
        }
        return documents;
    }
}
