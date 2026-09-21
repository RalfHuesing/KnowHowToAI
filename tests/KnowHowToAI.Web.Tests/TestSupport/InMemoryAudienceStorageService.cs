using KnowHowToAI.Server.Web.State;

namespace KnowHowToAI.Web.Tests.TestSupport;

public sealed class InMemoryAudienceStorageService : IAudienceStorageService
{
    public string? LastAudienceId { get; set; }

    public InMemoryAudienceStorageService(string? initialAudienceId = null)
    {
        LastAudienceId = initialAudienceId;
    }

    public ValueTask<string?> GetLastAudienceIdAsync() => ValueTask.FromResult(LastAudienceId);

    public ValueTask SetLastAudienceIdAsync(string audienceId)
    {
        LastAudienceId = audienceId;
        return ValueTask.CompletedTask;
    }
}
