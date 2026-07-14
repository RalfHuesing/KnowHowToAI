using KnowHowToAI.Core.Configuration;

namespace KnowHowToAI.Core.Sync;

// DbUp-Migration + Validate + Wipe-and-Dump in einer Transaktion.
// Ablauf: docs/01-Konzept-und-Workflow.md, Phase 4. Edge Cases: docs/04, Abschnitt 4.3/4.4.
public sealed class ImportService
{
    public Task ImportAsync(KnowHowToAiOptions options, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
