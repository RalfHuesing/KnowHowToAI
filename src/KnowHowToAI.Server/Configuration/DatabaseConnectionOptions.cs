namespace KnowHowToAI.Server.Configuration;

/// <summary>
/// Bindbare, ausschließlich aus der separaten <c>DatabaseConnection</c>-Sektion stammende
/// Datenbankverbindung. Der Composition Root überführt sie nach der Validierung in den
/// Storage-Vertrag; nachgelagerte Schichten kennen keine Konfigurationswerte.
/// </summary>
internal sealed record DatabaseConnectionOptions
{
    public const string SectionName = "DatabaseConnection";

    public string Server { get; init; } = string.Empty;

    public string Database { get; init; } = string.Empty;

    public string UserName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public bool UseWindowsAuthentication { get; init; }
}
