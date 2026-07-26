using KnowHowToAI.Core.Documents;
using KnowHowToAI.Core.Sync;
using Microsoft.Extensions.Logging.Abstractions;

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
        var service = new ImportService(
            (_, _) =>
            {
                replaceCallCount++;
                return Task.CompletedTask;
            },
            maxContentLengthWarning: 8000,
            logger: NullLogger<ImportService>.Instance);

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
        var service = new ImportService(
            (documents, _) =>
            {
                replacedWith = documents;
                return Task.CompletedTask;
            },
            maxContentLengthWarning: 8000,
            logger: NullLogger<ImportService>.Instance);

        var result = await service.ImportAsync(_docsRoot, TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
        Assert.NotNull(replacedWith);
        Assert.Equal(2, replacedWith.Count);
        Assert.Contains(replacedWith, d => d.Slug == "it" && d.Title == "IT");
        Assert.Contains(replacedWith, d => d.Slug == "it/netzwerk" && d.Title == "Netzwerk");
    }

    [Fact]
    public async Task ImportAsync_ReplaceThrows_PropagatesException()
    {
        WriteDoc("it", "IT");
        var service = new ImportService(
            (_, _) => throw new InvalidOperationException("SQL-Fehler"),
            maxContentLengthWarning: 8000,
            logger: NullLogger<ImportService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ImportAsync(_docsRoot, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ImportAsync_CancellationRequestedBeforeReplace_PropagatesOperationCanceled()
    {
        WriteDoc("it", "IT");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var service = new ImportService(
            (_, _) => Task.CompletedTask,
            maxContentLengthWarning: 8000,
            logger: NullLogger<ImportService>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(_docsRoot, cts.Token));
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
        var document = new Document("it/netzwerk", "Netzwerk", "Inhalt.", ParentSlug: "it", Tags: ["a"], Synonyms: []);
        var service = new ExportService(
            (_) => Task.FromResult<IReadOnlyList<Document>>([document]),
            NullLogger<ExportService>.Instance);

        await service.ExportAsync(_targetDirectory, MarkerFileName, TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(_targetDirectory, MarkerFileName)));
        var writtenFile = Path.Combine(_targetDirectory, "it", "netzwerk.md");
        Assert.True(File.Exists(writtenFile));

        var roundTripped = FrontMatterParser.Parse("it/netzwerk", await File.ReadAllTextAsync(writtenFile, TestContext.Current.CancellationToken));
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

        var document = new Document("neu", "Neu", "Inhalt.", null, [], []);
        var service = new ExportService(
            (_) => Task.FromResult<IReadOnlyList<Document>>([document]),
            NullLogger<ExportService>.Instance);

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
        var service = new ExportService(
            (_) =>
            {
                getAllCallCount++;
                return Task.FromResult<IReadOnlyList<Document>>([]);
            },
            NullLogger<ExportService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExportAsync(_targetDirectory, MarkerFileName, TestContext.Current.CancellationToken));
        Assert.Equal(0, getAllCallCount);
    }

    [Fact]
    public async Task ExportAsync_GetAllThrows_PropagatesException()
    {
        var service = new ExportService(
            (_) => throw new InvalidOperationException("DB-Fehler"),
            NullLogger<ExportService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ExportAsync(_targetDirectory, MarkerFileName, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ExportAsync_EmptyList_WritesMarkerOnly()
    {
        var service = new ExportService(
            (_) => Task.FromResult<IReadOnlyList<Document>>([]),
            NullLogger<ExportService>.Instance);

        await service.ExportAsync(_targetDirectory, MarkerFileName, TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(_targetDirectory, MarkerFileName)));
        var mdFiles = Directory.EnumerateFiles(_targetDirectory, "*.md", SearchOption.AllDirectories).ToList();
        Assert.Empty(mdFiles);
    }
}
