namespace KnowHowToAI.Server.Web.Components.Shared;

/// <summary>
/// Stufen des wiederverwendeten Warnvertrags für <see cref="InlineAlert"/>
/// und <see cref="StatusBanner"/>. Jede Stufe zeigt Icon plus Text über die
/// zentrale Statusdarstellung <see cref="AppStatus"/>; die Farbe ist nie der
/// alleinige Informationsträger.
/// </summary>
public enum AlertKind
{
    Info,
    Erfolg,
    Warnung,
    Fehler
}

/// <summary>
/// Ordnet die Warnstufen den Zustandsarten der zentralen
/// Statusdarstellung zu; Info erscheint in den Info-Tokens der
/// Aktiv-Darstellung.
/// </summary>
internal static class AlertKindExtensions
{
    internal static AppStatusKind ToStatusKind(this AlertKind kind) => kind switch
    {
        AlertKind.Erfolg => AppStatusKind.Erfolg,
        AlertKind.Warnung => AppStatusKind.Warnung,
        AlertKind.Fehler => AppStatusKind.Fehler,
        _ => AppStatusKind.Aktiv
    };
}
