namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Startet für den Initial-Root-Smoke einen eigenen, frisch bereinigten
/// Workflow-Host und legt anschließend ausschließlich den deterministischen
/// Browser-Ausgangsbestand über MCP an.
/// </summary>
public sealed class RootNodeHostFixture : IAsyncLifetime
{
    private PublishedServerHost? _host;

    public PublishedServerHost Host => _host ?? throw new InvalidOperationException("Der Root-Node-Testhost wurde noch nicht initialisiert.");

    public async ValueTask InitializeAsync()
    {
        _host = await BrowserHostBootstrap.StartSeededWorkflowAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is not null)
            await _host.DisposeAsync();
    }
}
