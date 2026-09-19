namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Startet die veröffentlichte Server-EXE einmal für die gesamte Testkollektion
/// und gibt den Prozess nach Abschluss aller Tests frei.
/// </summary>
public sealed class SmokeHostFixture : IAsyncLifetime
{
    private PublishedServerHost? _host;

    public PublishedServerHost Host => _host ?? throw new InvalidOperationException("Der Serverhost wurde noch nicht initialisiert.");

    public async ValueTask InitializeAsync()
    {
        var host = await PublishedServerHost.StartAsync();
        try
        {
            await BrowserKnowledgeSeed.EnsureWorkflowAsync(host.Address);
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
        {
            await _host.DisposeAsync();
        }
    }
}
