namespace KnowHowToAI.BrowserTests.TestSupport;

internal static class BrowserHostBootstrap
{
    internal static async Task<PublishedServerHost> StartSeededWorkflowAsync()
    {
        var host = await PublishedServerHost.StartAsync();
        try
        {
            await BrowserKnowledgeSeed.EnsureWorkflowAsync(host.Address);
            return host;
        }
        catch
        {
            await host.DisposeAsync();
            throw;
        }
    }
}
