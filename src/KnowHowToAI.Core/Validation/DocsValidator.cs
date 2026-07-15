using System.Text.RegularExpressions;
using KnowHowToAI.Core.Documents;

namespace KnowHowToAI.Core.Validation;

// YAML-Check, Slug-Check, Orphan-Check, Content-Link-Check — alle Fehler sammeln statt beim ersten abzubrechen.
// Regeln: docs/04-Datenmodell-Validierung-Edgecases.md, Abschnitt 3.
public sealed partial class DocsValidator(int maxContentLengthWarning = 8000)
{
    private readonly FrontMatterParser _parser = new();

    public ValidationResult Validate(string docsRootPath)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationError>();
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
                var document = _parser.Parse(slug, File.ReadAllText(filePath));
                ValidateContentLinks(relativePath, document.Content, errors);
                ValidateContentLength(relativePath, document.Content, warnings);
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

        return new ValidationResult { Errors = errors, Warnings = warnings };
    }

    // Erkennt Datei-/Pfad-Referenzen statt Slug-Referenzen: file://-Links und Links auf .md/.markdown
    // (relativ oder absolut) sind im SQL-Cache-Modell wirkungslos, da dort kein Dateisystem existiert
    // — Navigation läuft ausschließlich über list_children/get_doc/Slugs. Siehe docs/05-Roadmap.md, "v2+".
    private static void ValidateContentLinks(string relativePath, string content, List<ValidationError> errors)
    {
        foreach (Match match in MarkdownLinkRegex().Matches(content))
        {
            var target = match.Groups["target"].Value.Trim();
            var targetWithoutTitle = target.Split(' ', 2)[0];
            var targetWithoutFragment = targetWithoutTitle.Split('#', '?')[0];

            var isFileScheme = targetWithoutTitle.StartsWith("file://", StringComparison.OrdinalIgnoreCase);
            var isMarkdownFile = targetWithoutFragment.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                || targetWithoutFragment.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase);

            if (isFileScheme || isMarkdownFile)
            {
                errors.Add(new ValidationError(relativePath, $"Datei-/Pfad-Referenz statt Slug-Referenz in content: '{target}'."));
            }
        }
    }

    // Default-Schwelle 8.000 Zeichen (siehe docs/05-Roadmap.md, "v2+") — konfigurierbar über
    // KnowHowToAi:Validation:MaxContentLengthWarning, siehe KnowHowToAiValidationOptions.
    private void ValidateContentLength(string relativePath, string content, List<ValidationError> warnings)
    {
        if (content.Length > maxContentLengthWarning)
        {
            warnings.Add(new ValidationError(relativePath, $"content ist {content.Length} Zeichen lang (Schwelle: {maxContentLengthWarning}) — Aufteilung in mehrere Slugs erwägen."));
        }
    }

    [GeneratedRegex(@"\[[^\]]*\]\((?<target>[^)]+)\)")]
    private static partial Regex MarkdownLinkRegex();
}
