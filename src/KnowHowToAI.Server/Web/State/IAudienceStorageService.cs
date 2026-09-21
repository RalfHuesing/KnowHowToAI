namespace KnowHowToAI.Server.Web.State;

/// <summary>
/// Abstraktion für das Speichern und Laden der zuletzt gewählten Zielgruppe im Browser (localStorage).
/// Verhindert direkte Kopplung an JS-Interop in Fachkomponenten und ermöglicht FastTests.
/// </summary>
public interface IAudienceStorageService
{
    /// <summary>
    /// Liest die zuletzt gewählte Zielgruppe (Schlüssel <c>knowhowtoai.lastAudienceId</c>) aus dem Browser.
    /// </summary>
    ValueTask<string?> GetLastAudienceIdAsync();

    /// <summary>
    /// Speichert die gewählte Zielgruppe (Schlüssel <c>knowhowtoai.lastAudienceId</c>) im Browser.
    /// </summary>
    ValueTask SetLastAudienceIdAsync(string audienceId);
}
