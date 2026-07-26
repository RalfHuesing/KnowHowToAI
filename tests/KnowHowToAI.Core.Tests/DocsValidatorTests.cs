using KnowHowToAI.Core.Validation;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowHowToAI.Core.Tests;

public class DocsValidatorTests : IDisposable
{
    private readonly string _docsRoot = Directory.CreateTempSubdirectory("knowhowtoai-docs-").FullName;
    private readonly DocsValidator _validator = new(logger: NullLogger<DocsValidator>.Instance);

    public void Dispose() => Directory.Delete(_docsRoot, recursive: true);

    [Fact]
    public void Validate_EmptyDirectory_ReturnsNoErrors()
    {
        var result = _validator.Validate(_docsRoot);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ValidHierarchy_ReturnsNoErrors()
    {
        WriteDoc("it", "IT");
        WriteDoc("it/netzwerk", "Netzwerk");
        WriteDoc("it/netzwerk/routing", "Routing");

        var result = _validator.Validate(_docsRoot);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_InvalidSlugSegment_ReportsError()
    {
        WriteDoc("IT", "Großbuchstabe im Slug");

        var result = _validator.Validate(_docsRoot);

        var error = Assert.Single(result.Errors);
        Assert.Contains("Ungültiger Slug", error.Reason);
    }

    [Fact]
    public void Validate_MissingTitle_ReportsError()
    {
        WriteFile("kein-titel.md", "---\ntags: [a]\n---\nInhalt.");

        var result = _validator.Validate(_docsRoot);

        var error = Assert.Single(result.Errors);
        Assert.Contains("title", error.Reason);
    }

    [Fact]
    public void Validate_MissingParentDocuments_ReportsOrphanErrorForEachAncestor()
    {
        WriteDoc("it/netzwerk/routing", "Routing ohne Eltern");

        var result = _validator.Validate(_docsRoot);

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.Reason.Contains("it/netzwerk.md"));
        Assert.Contains(result.Errors, e => e.Reason.Contains("'it.md'"));
    }

    [Fact]
    public void Validate_CollectsErrorsAcrossMultipleFiles()
    {
        WriteDoc("IT", "Ungültiger Slug");
        WriteDoc("other/child", "Orphan");

        var result = _validator.Validate(_docsRoot);

        Assert.Equal(2, result.Errors.Count);
    }

    [Theory]
    [InlineData("[Erfassung](file:///c:/Daten/erfassung-sitzungen.md)")]
    [InlineData("[Erfassung](erfassung-sitzungen.md)")]
    [InlineData("[Erfassung](../docs/erfassung-sitzungen.markdown)")]
    [InlineData("[Erfassung](erfassung-sitzungen.md#abschnitt)")]
    public void Validate_ContentContainsFileOrMarkdownLink_ReportsError(string link)
    {
        WriteDoc("it", "IT", link);

        var result = _validator.Validate(_docsRoot);

        var error = Assert.Single(result.Errors);
        Assert.Contains("Datei-/Pfad-Referenz", error.Reason);
    }

    [Fact]
    public void Validate_ContentContainsSlugAndHttpLinks_ReturnsNoErrors()
    {
        WriteDoc("it", "IT", "[Netzwerk](it/netzwerk) und [Doku](https://example.com/handbuch)");

        var result = _validator.Validate(_docsRoot);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ContentAtThreshold_ReturnsNoWarning()
    {
        var validator = new DocsValidator(maxContentLengthWarning: 10, logger: NullLogger<DocsValidator>.Instance);
        WriteDoc("it", "IT", new string('a', 10));

        var result = validator.Validate(_docsRoot);

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Validate_ContentAboveThreshold_ReportsWarningButStaysValid()
    {
        var validator = new DocsValidator(maxContentLengthWarning: 10, logger: NullLogger<DocsValidator>.Instance);
        WriteDoc("it", "IT", new string('a', 11));

        var result = validator.Validate(_docsRoot);

        var warning = Assert.Single(result.Warnings);
        Assert.Contains("11 Zeichen", warning.Reason);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EmptyContent_NoWarning()
    {
        var validator = new DocsValidator(maxContentLengthWarning: 10, logger: NullLogger<DocsValidator>.Instance);
        WriteDoc("it", "IT", string.Empty);

        var result = validator.Validate(_docsRoot);

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Validate_VeryShortContent_NoWarning()
    {
        var validator = new DocsValidator(maxContentLengthWarning: 10, logger: NullLogger<DocsValidator>.Instance);
        WriteDoc("it", "IT", "a");

        var result = validator.Validate(_docsRoot);

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Validate_ZeroWarningThreshold_WarnsEverythingNonEmpty()
    {
        var validator = new DocsValidator(maxContentLengthWarning: 0, logger: NullLogger<DocsValidator>.Instance);
        WriteDoc("it", "IT", "a");

        var result = validator.Validate(_docsRoot);

        var warning = Assert.Single(result.Warnings);
        Assert.Contains("1 Zeichen", warning.Reason);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_OnlyNonMdFiles_ReturnsNoErrorsAndIgnoresThem()
    {
        WriteFile("notiz.txt", "Kein Markdown.");
        WriteFile("bild.png", "PNG-Binary.");

        var result = _validator.Validate(_docsRoot);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    private void WriteDoc(string slug, string title, string content = "Inhalt.") =>
        WriteFile($"{slug}.md", $"---\ntitle: \"{title}\"\n---\n{content}");

    private void WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_docsRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }
}
