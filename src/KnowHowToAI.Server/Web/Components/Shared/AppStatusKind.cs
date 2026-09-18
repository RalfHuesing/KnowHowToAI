namespace KnowHowToAI.Server.Web.Components.Shared;

/// <summary>
/// Zustandsarten der zentralen Statusdarstellung. Jede Darstellung zeigt
/// Icon und Text; die Farben stammen ausschließlich aus den globalen
/// Tokens in <c>wwwroot/css/app.css</c>. Der deaktivierte Zustand ist kein
/// Statuskind, sondern die zentrale Disabled-Regel in <c>app.css</c>.
/// </summary>
public enum AppStatusKind
{
    Neutral,
    Aktiv,
    Erfolg,
    Warnung,

    /// <summary>Ungespeicherte Änderungen in den Warntoken mit eigenem Icon und Text.</summary>
    Ungespeichert,

    Fehler
}
