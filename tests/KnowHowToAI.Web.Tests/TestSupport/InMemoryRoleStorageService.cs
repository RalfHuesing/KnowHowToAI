using KnowHowToAI.Server.Web.State;

namespace KnowHowToAI.Web.Tests.TestSupport;

public sealed class InMemoryRoleStorageService : IRoleStorageService
{
    public string? LastRoleId { get; set; }

    public InMemoryRoleStorageService(string? initialRoleId = null)
    {
        LastRoleId = initialRoleId;
    }

    public ValueTask<string?> GetLastRoleIdAsync() => ValueTask.FromResult(LastRoleId);

    public ValueTask SetLastRoleIdAsync(string roleId)
    {
        LastRoleId = roleId;
        return ValueTask.CompletedTask;
    }
}
