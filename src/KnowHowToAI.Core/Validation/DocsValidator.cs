using KnowHowToAI.Core.Documents;

namespace KnowHowToAI.Core.Validation;

// YAML-Check, Slug-Check, Orphan-Check — alle Fehler sammeln statt beim ersten abzubrechen.
// Regeln: docs/04-Datenmodell-Validierung-Edgecases.md, Abschnitt 3.
public sealed class DocsValidator
{
    private readonly FrontMatterParser _parser = new();

    public ValidationResult Validate(string docsRootPath)
    {
        var errors = new List<ValidationError>();
        var slugs = new HashSet<string>();

        foreach (var filePath in Directory.EnumerateFiles(docsRootPath, "*.md", SearchOption.AllDirectories))
        {
            var slug = SlugRules.FromFilePath(docsRootPath, filePath);
            var relativePath = $"{slug}.md";

            if (!SlugRules.IsValidSlug(slug))
            {
                errors.Add(new ValidationError(relativePath, $"Ungültiger Slug '{slug}': nur a-z, 0-9, '-' und '/' erlaubt."));
                continue;
            }

            slugs.Add(slug);

            try
            {
                _parser.Parse(slug, File.ReadAllText(filePath));
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new ValidationError(relativePath, ex.Message));
            }
        }

        foreach (var slug in slugs)
        {
            var parentSlug = SlugRules.GetParentSlug(slug);
            while (parentSlug is not null)
            {
                if (!slugs.Contains(parentSlug))
                {
                    errors.Add(new ValidationError($"{slug}.md", $"Fehlendes übergeordnetes Dokument '{parentSlug}.md'."));
                }

                parentSlug = SlugRules.GetParentSlug(parentSlug);
            }
        }

        return new ValidationResult { Errors = errors };
    }
}
