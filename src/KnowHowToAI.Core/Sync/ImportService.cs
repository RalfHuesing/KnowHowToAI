using KnowHowToAI.Core.Documents;
using KnowHowToAI.Core.Validation;

namespace KnowHowToAI.Core.Sync;

// Validate + Wipe-and-Dump. Der eigentliche SQL-Zugriff kommt als Delegate von außen (z. B.
// SqlDocumentsStore.ReplaceAllAsync) — so ist die Orchestrierung ohne echten SQL Server testbar.
// Schema-Migration läuft VOR diesem Aufruf in der Cli-Schicht (siehe docs/03, Abschnitt 3).
public sealed class ImportService(Func<IReadOnlyList<Document>, CancellationToken, Task> replaceAllAsync)
{
    private readonly DocsValidator _validator = new();
    private readonly FrontMatterParser _parser = new();

    // Gibt bei Validierungsfehlern die ValidationResult mit den Fehlern zurück, ohne replaceAllAsync
    // aufzurufen. Bei Erfolg eine ValidationResult ohne Fehler, nachdem der Import durchgelaufen ist.
    public async Task<ValidationResult> ImportAsync(string docsRootPath, CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Validate(docsRootPath);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        var documents = ReadDocuments(docsRootPath).ToList();
        await replaceAllAsync(documents, cancellationToken);

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
