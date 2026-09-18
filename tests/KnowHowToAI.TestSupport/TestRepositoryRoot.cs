namespace KnowHowToAI.TestSupport;

/// <summary>
/// Ermittelt das Repository-Root (das Verzeichnis mit KnowHowToAI.slnx) ausgehend
/// vom Test-Binärverzeichnis, damit Tests pfadunabhängig Repo-Artefakte wie Doku,
/// Messberichte oder temp-Verzeichnisse adressieren.
/// </summary>
public static class TestRepositoryRoot
{
    /// <summary>Absoluter Pfad zum Verzeichnis, das KnowHowToAI.slnx enthält.</summary>
    public static string Resolve()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory);
             current is not null;
             current = current.Parent)
        {
            if (File.Exists(System.IO.Path.Combine(current.FullName, "KnowHowToAI.slnx")))
                return current.FullName;
        }

        throw new DirectoryNotFoundException(
            "Das Repository-Root mit KnowHowToAI.slnx wurde nicht gefunden.");
    }
}
