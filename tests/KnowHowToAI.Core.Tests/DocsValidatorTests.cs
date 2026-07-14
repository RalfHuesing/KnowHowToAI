using KnowHowToAI.Core.Validation;

namespace KnowHowToAI.Core.Tests;

public class DocsValidatorTests : IDisposable
{
    private readonly string _docsRoot = Directory.CreateTempSubdirectory("knowhowtoai-docs-").FullName;
    private readonly DocsValidator _validator = new();

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

    private void WriteDoc(string slug, string title) =>
        WriteFile($"{slug}.md", $"---\ntitle: \"{title}\"\n---\nInhalt.");

    private void WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_docsRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }
}
