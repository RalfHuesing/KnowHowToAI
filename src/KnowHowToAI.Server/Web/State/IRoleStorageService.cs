namespace KnowHowToAI.Server.Web.State;

/// <summary>
/// Abstraktion für das Speichern und Laden der zuletzt gewählten Rolle im Browser (localStorage).
/// Verhindert direkte Kopplung an JS-Interop in Fachkomponenten und ermöglicht FastTests.
/// </summary>
public interface IRoleStorageService
{
    /// <summary>
    /// Liest die zuletzt gewählte Rolle (Schlüssel <c>knowhowtoai.lastRoleId</c>) aus dem Browser.
    /// </summary>
    ValueTask<string?> GetLastRoleIdAsync();

    /// <summary>
    /// Speichert die gewählte Rolle (Schlüssel <c>knowhowtoai.lastRoleId</c>) im Browser.
    /// </summary>
    ValueTask SetLastRoleIdAsync(string roleId);
}
