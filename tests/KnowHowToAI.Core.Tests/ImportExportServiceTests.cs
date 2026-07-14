using KnowHowToAI.Core.Documents;
using KnowHowToAI.Core.Sync;

namespace KnowHowToAI.Core.Tests;

public class ImportServiceTests : IDisposable
{
    private readonly string _docsRoot = Directory.CreateTempSubdirectory("knowhowtoai-import-").FullName;

    public void Dispose() => Directory.Delete(_docsRoot, recursive: true);

    [Fact]
    public async Task ImportAsync_InvalidDocs_ReturnsErrorsAndDoesNotReplaceAnything()
    {
        WriteDoc("IT", "Ungültiger Slug");
        var replaceCallCount = 0;
        var service = new ImportService((_, _) =>
        {
            replaceCallCount++;
            return Task.CompletedTask;
        });

        var result = await service.ImportAsync(_docsRoot, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Equal(0, replaceCallCount);
    }

    [Fact]
    public async Task ImportAsync_ValidDocs_ReplacesWithParsedDocuments()
    {
        WriteDoc("it", "IT");
        WriteDoc("it/netzwerk", "Netzwerk");
        IReadOnlyList<Document>? replacedWith = null;
        var service = new ImportService((documents, _) =>
        {
            replacedWith = documents;
            return Task.CompletedTask;
        });

        var result = await service.ImportAsync(_docsRoot, TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
        Assert.NotNull(replacedWith);
        Assert.Equal(2, replacedWith.Count);
        Assert.Contains(replacedWith, d => d.Slug == "it" && d.Title == "IT");
        Assert.Contains(replacedWith, d => d.Slug == "it/netzwerk" && d.Title == "Netzwerk");
    }

    private void WriteDoc(string slug, string title)
    {
        var fullPath = Path.Combine(_docsRoot, $"{slug}.md");
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, $"---\ntitle: \"{title}\"\n---\nInhalt.");
    }
}

public class ExportServiceTests : IDisposable
{
    private const string MarkerFileName = ".knowhowtoai-export-marker.json";
    private readonly string _targetDirectory = Path.Combine(Path.GetTempPath(), $"knowhowtoai-export-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_targetDirectory))
        {
            Directory.Delete(_targetDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExportAsync_NewTargetDirectory_CreatesMarkerAndWritesDocuments()
    {
        var document = new Document { Slug = "it/netzwerk", Title = "Netzwerk", Content = "Inhalt.", Tags = ["a"] };
        var service = new ExportService((_) => Task.FromResult<IReadOnlyList<Document>>([document]));

        await service.ExportAsync(_targetDirectory, MarkerFileName, TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(_targetDirectory, MarkerFileName)));
        var writtenFile = Path.Combine(_targetDirectory, "it", "netzwerk.md");
        Assert.True(File.Exists(writtenFile));

        var parser = new FrontMatterParser();
        var roundTripped = parser.Parse("it/netzwerk", await File.ReadAllTextAsync(writtenFile, TestContext.Current.CancellationToken));
        Assert.Equal("Netzwerk", roundTripped.Title);
        Assert.Equal(["a"], roundTripped.Tags);
    }

    [Fact]
    public async Task ExportAsync_ExistingMarker_WipesOldMarkdownBeforeReExport()
    {
        Directory.CreateDirectory(_targetDirectory);
        await File.WriteAllTextAsync(Path.Combine(_targetDirectory, MarkerFileName), "{}", TestContext.Current.CancellationToken);
        var staleFile = Path.Combine(_targetDirectory, "veraltet.md");
        await File.WriteAllTextAsync(staleFile, "alt", TestContext.Current.CancellationToken);

        var document = new Document { Slug = "neu", Title = "Neu", Content = "Inhalt." };
        var service = new ExportService((_) => Task.FromResult<IReadOnlyList<Document>>([document]));

        await service.ExportAsync(_targetDirectory, MarkerFileName, TestContext.Current.CancellationToken);

        Assert.False(File.Exists(staleFile));
        Assert.True(File.Exists(Path.Combine(_targetDirectory, "neu.md")));
    }

    [Fact]
    public async Task ExportAsync_ForeignFilesWithoutMarker_ThrowsAndDoesNotCallGetAll()
    {
        Directory.CreateDirectory(_targetDirectory);
        await File.WriteAllTextAsync(Path.Combine(_targetDirectory, "fremd.txt"), "fremd", TestContext.Current.CancellationToken);
        var getAllCallCount = 0;
        var service = new ExportService((_) =>
        {
            getAllCallCount++;
            return Task.FromResult<IReadOnlyList<Document>>([]);
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExportAsync(_targetDirectory, MarkerFileName, TestContext.Current.CancellationToken));
        Assert.Equal(0, getAllCallCount);
    }
}
