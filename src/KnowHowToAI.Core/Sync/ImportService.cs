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
    private readonly FrontMatterParser _parser = new();

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

        var documents = ReadDocuments(docsRootPath).ToList();
        await replaceAllAsync(documents, cancellationToken);

        logger?.LogInformation(
            "Import abgeschlossen: {ErrorCount} Fehler, {WarningCount} Warnungen, {ElapsedMs}ms",
            validationResult.Errors.Count, validationResult.Warnings.Count, sw.ElapsedMilliseconds);
        return validationResult;
    }

    private IEnumerable<Document> ReadDocuments(string docsRootPath)
    {
        foreach (var filePath in Directory.EnumerateFiles(docsRootPath, "*.md", SearchOption.AllDirectories))
        {
            var slug = SlugRules.FromFilePath(docsRootPath, filePath);
            yield return _parser.Parse(slug, File.ReadAllText(filePath));
        }
    }
}
