using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Server.Configuration;
using Microsoft.Extensions.Options;

namespace KnowHowToAI.Server.Hosting;

/// <summary>
/// Dummy-Implementierung von <see cref="ICurrentUserService"/> für den authfreien Stand.
/// Liefert den konfigurierten Benutzernamen aus KnowHowToAI:Auth:DummyUserName (Default: "System").
/// </summary>
internal sealed class DummyCurrentUserService : ICurrentUserService
{
    private readonly CurrentUser _currentUser;

    public DummyCurrentUserService(IOptions<KnowHowToAIOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var name = string.IsNullOrWhiteSpace(options.Value.Auth.DummyUserName)
            ? "System"
            : options.Value.Auth.DummyUserName;
        _currentUser = new CurrentUser("dummy", name);
    }

    public CurrentUser GetCurrentUser() => _currentUser;

    public string GetCurrentUserName() => _currentUser.Name;
}
