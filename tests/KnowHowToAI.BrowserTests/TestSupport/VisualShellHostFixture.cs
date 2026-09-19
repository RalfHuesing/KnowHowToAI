namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Hält den minimalen, von Workflow-Smokes getrennten Datenbestand für
/// bytegenaue Shell-Baselines stabil.
/// </summary>
public sealed class VisualShellHostFixture : IAsyncLifetime
{
    private PublishedServerHost? _host;

    public PublishedServerHost Host => _host ?? throw new InvalidOperationException("Der visuelle Serverhost wurde noch nicht initialisiert.");

    public async ValueTask InitializeAsync()
    {
        var host = await PublishedServerHost.StartAsync(BrowserTestDatabaseKind.VisualShell);
        try
        {
            await BrowserKnowledgeSeed.EnsureVisualShellAsync(host.Address);
            _host = host;
        }
        catch
        {
            await host.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is not null)
            await _host.DisposeAsync();
    }
}
