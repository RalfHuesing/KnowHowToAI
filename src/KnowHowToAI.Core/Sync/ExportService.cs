using KnowHowToAI.Core.Configuration;

namespace KnowHowToAI.Core.Sync;

// Marker-Datei-geschützter Export der DB nach .md-Dateien.
// Regeln: docs/04-Datenmodell-Validierung-Edgecases.md, Abschnitt 4.5 (Export-Marker-Datei).
public sealed class ExportService
{
    public Task ExportAsync(KnowHowToAiOptions options, string targetDirectory, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
