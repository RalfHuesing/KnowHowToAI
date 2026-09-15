using Microsoft.Extensions.Options;

namespace KnowHowToAI.Server.Configuration;

/// <summary>
/// Validiert die Datenbankverbindung beim Start, ohne dabei Geheimnisse in Fehlern zu spiegeln.
/// </summary>
internal sealed class DatabaseConnectionOptionsValidator : IValidateOptions<DatabaseConnectionOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseConnectionOptions options)
    {
        var errors = new List<string>();
        var server = Environment.ExpandEnvironmentVariables(options.Server);

        if (string.IsNullOrWhiteSpace(options.Server))
            errors.Add("DatabaseConnection:Server muss gesetzt sein.");
        else if (string.IsNullOrWhiteSpace(server) || server.Contains('%', StringComparison.Ordinal))
            errors.Add("DatabaseConnection:Server enthält einen nicht auflösbaren Windows-Umgebungsplatzhalter.");

        if (string.IsNullOrWhiteSpace(options.Database))
            errors.Add("DatabaseConnection:Database muss gesetzt sein.");

        if (!options.UseWindowsAuthentication && string.IsNullOrWhiteSpace(options.UserName))
            errors.Add("DatabaseConnection:UserName muss bei SQL-Authentifizierung gesetzt sein.");

        if (!options.UseWindowsAuthentication && string.IsNullOrWhiteSpace(options.Password))
            errors.Add("DatabaseConnection:Password muss bei SQL-Authentifizierung gesetzt sein.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
