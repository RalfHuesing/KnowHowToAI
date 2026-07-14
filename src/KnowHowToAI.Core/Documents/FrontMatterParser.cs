using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KnowHowToAI.Core.Documents;

// Parst eine einzelne .md-Datei (YAML Front Matter + Inhalt) in ein Document.
// Front-Matter-Format: docs/02-Architektur-und-Techstack.md, Abschnitt 3.
public sealed class FrontMatterParser
{
    private static readonly IDeserializer Deserializer =
        new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    private static readonly ISerializer Serializer =
        new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

    public Document Parse(string slug, string fileContent)
    {
        var (yaml, content) = SplitFrontMatter(fileContent);
        var frontMatter = DeserializeFrontMatter(yaml);

        if (string.IsNullOrWhiteSpace(frontMatter.Title))
        {
            throw new InvalidOperationException($"Front Matter von '{slug}' enthält kein 'title'.");
        }

        return new Document
        {
            Slug = slug,
            ParentSlug = SlugRules.GetParentSlug(slug),
            Title = frontMatter.Title,
            Content = content,
            Tags = frontMatter.Tags ?? [],
            Synonyms = frontMatter.Synonyms ?? [],
        };
    }

    public string Render(Document document)
    {
        var frontMatter = new FrontMatterData
        {
            Title = document.Title,
            Tags = document.Tags.Count > 0 ? [.. document.Tags] : null,
            Synonyms = document.Synonyms.Count > 0 ? [.. document.Synonyms] : null,
        };

        var yaml = Serializer.Serialize(frontMatter).TrimEnd('\n');
        return $"---\n{yaml}\n---\n{document.Content}";
    }

    private static (string Yaml, string Content) SplitFrontMatter(string fileContent)
    {
        const string delimiter = "---";
        var normalized = fileContent.Replace("\r\n", "\n");

        if (!normalized.StartsWith(delimiter + "\n", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Datei beginnt nicht mit YAML Front Matter ('---').");
        }

        var closingIndex = normalized.IndexOf("\n" + delimiter, delimiter.Length, StringComparison.Ordinal);
        if (closingIndex < 0)
        {
            throw new InvalidOperationException("YAML Front Matter wurde nicht mit '---' geschlossen.");
        }

        var yaml = normalized[delimiter.Length..closingIndex];
        var content = normalized[(closingIndex + delimiter.Length + 1)..].TrimStart('\n');
        return (yaml, content);
    }

    private static FrontMatterData DeserializeFrontMatter(string yaml)
    {
        try
        {
            return Deserializer.Deserialize<FrontMatterData>(yaml) ?? new FrontMatterData();
        }
        catch (YamlException ex)
        {
            throw new InvalidOperationException($"Ungültiges YAML Front Matter: {ex.Message}", ex);
        }
    }

    private sealed class FrontMatterData
    {
        public string? Title { get; set; }
        public List<string>? Tags { get; set; }
        public List<string>? Synonyms { get; set; }
    }
}
