namespace KnowHowToAI.TestSupport;

/// <summary>
/// Wegwerf-Verzeichnis unter <c>temp/&lt;prefix&gt;_&lt;random&gt;</c> im Repository.
/// Das Verzeichnis wird bei Erzeugung angelegt und räumt sich beim Verlassen
/// (Dispose) selbst rekursiv ab; der Löschversuch wartet den kurzen
/// Image-Section-Nachlauf frisch beendeter Prozesse über wenige Wiederholungen ab,
/// statt den Test am Cleanup scheitern zu lassen. Dispose ist idempotent.
/// </summary>
public sealed class TestTempDirectory : IDisposable
{
    private TestTempDirectory(string fullPath) => FullPath = fullPath;

    /// <summary>Absoluter Pfad des angelegten Verzeichnisses.</summary>
    public string FullPath { get; }

    /// <summary>
    /// Legt <c>temp/&lt;prefix&gt;_&lt;random&gt;</c> unter dem Repository-Root an;
    /// der Präfix ordnet das Verzeichnis später dem aufrufenden Test zu.
    /// </summary>
    public static TestTempDirectory Create(string prefix)
    {
        var fullPath = System.IO.Path.Combine(
            TestRepositoryRoot.Resolve(),
            "temp",
            $"{prefix}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(fullPath);
        return new TestTempDirectory(fullPath);
    }

    /// <summary>Pfad einer Datei innerhalb des Wegwerf-Verzeichnisses.</summary>
    public string FilePath(string fileName) => System.IO.Path.Combine(FullPath, fileName);

    public void Dispose()
    {
        if (!Directory.Exists(FullPath))
            return;

        DeleteDirectoryWithRetry(FullPath);
    }

    private static void DeleteDirectoryWithRetry(string path)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (Exception exception) when (
                attempt < 5 &&
                exception is UnauthorizedAccessException or IOException)
            {
                Thread.Sleep(millisecondsTimeout: 200);
            }
        }
    }
}
