namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Serialisiert fachliche Writes und MCP-Seeds gegen den gemeinsamen
/// Workflowbestand. Die Browser-Assembly serialisiert zusätzlich alle Hosts,
/// damit Cleanup und laufende Browserflüsse dasselbe Testziel nie überlappen.
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
