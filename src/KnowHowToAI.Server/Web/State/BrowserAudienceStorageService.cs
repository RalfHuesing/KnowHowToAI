using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace KnowHowToAI.Server.Web.State;

/// <summary>
/// Persistiert die letzte Zielgruppenauswahl im Browser-LocalStorage unter dem Schlüssel <c>knowhowtoai.lastAudienceId</c>.
/// Fängt Prerendering-, Trennungs- und Browser-Sicherheitsausnahmen ab und protokolliert sie.
/// </summary>
public sealed class BrowserAudienceStorageService : IAudienceStorageService
{
    public const string StorageKey = "knowhowtoai.lastAudienceId";

    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<BrowserAudienceStorageService> _logger;

    public BrowserAudienceStorageService(IJSRuntime jsRuntime, ILogger<BrowserAudienceStorageService> logger)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<string?> GetLastAudienceIdAsync()
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

    public async ValueTask SetLastAudienceIdAsync(string audienceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audienceId);

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, audienceId).ConfigureAwait(false);
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
