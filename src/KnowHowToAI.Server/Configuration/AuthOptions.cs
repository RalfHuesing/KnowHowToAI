namespace KnowHowToAI.Server.Configuration;

/// <summary>
/// Konfigurationsoptionen für die Authentifizierung und den Dummy-Benutzer im authfreien Stand.
/// </summary>
internal sealed record AuthOptions
{
    public string DummyUserName { get; init; } = "System";
}
