namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Serialisiert nur fachliche Writes gegen den gemeinsamen Workflowbestand.
/// Read-only Browser-Smokes bleiben davon unabhängig parallel ausführbar.
/// </summary>
internal static class BrowserWorkflowDatabaseGate
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task<IDisposable> AcquireAsync()
    {
        await Gate.WaitAsync();
        return new Lease();
    }

    private sealed class Lease : IDisposable
    {
        public void Dispose() => Gate.Release();
    }
}
