using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace KnowHowToAI.Server.Web.State;

/// <summary>
/// Persistiert die letzte Rollenauswahl im Browser-LocalStorage unter dem Schlüssel <c>knowhowtoai.lastRoleId</c>.
/// Fängt Prerendering-, Trennungs- und Browser-Sicherheitsausnahmen ab und protokolliert sie.
/// </summary>
public sealed class BrowserRoleStorageService : IRoleStorageService
{
    public const string StorageKey = "knowhowtoai.lastRoleId";

    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<BrowserRoleStorageService> _logger;

    public BrowserRoleStorageService(IJSRuntime jsRuntime, ILogger<BrowserRoleStorageService> logger)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<string?> GetLastRoleIdAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogDebug(ex, "localStorage ist während des Prerenderings nicht verfügbar.");
            return null;
        }
        catch (JSException ex)
        {
            _logger.LogDebug(ex, "localStorage konnte nicht gelesen werden.");
            return null;
        }
    }

    public async ValueTask SetLastRoleIdAsync(string roleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, roleId).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogDebug(ex, "localStorage ist während des Prerenderings nicht verfügbar.");
        }
        catch (JSException ex)
        {
            _logger.LogDebug(ex, "localStorage konnte nicht geschrieben werden.");
        }
    }
}
