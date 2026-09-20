using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace KnowHowToAI.IntegrationTests.TestSupport;

/// <summary>
/// Liest die für echte SQL-Integrationstests bestimmte Datenbankverbindung aus der
/// produktiven App-Konfiguration. Platzhalter im Serverwert werden erst zur Laufzeit
/// expandiert, damit die versionierte Konfiguration keinen konkreten Rechnernamen enthält.
/// </summary>
internal sealed record SqlIntegrationTestSettings
{
    private const string SectionName = "DatabaseConnection";

    public required string Server { get; init; }
    public required string Database { get; init; }
    public required string UserName { get; init; }
    public required string Password { get; init; }
    public required bool UseWindowsAuthentication { get; init; }

    public static SqlIntegrationTestSettings Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();
        var section = configuration.GetRequiredSection(SectionName);

        var server = GetRequiredValue(section, nameof(Server));
        var database = GetRequiredValue(section, nameof(Database));
        var userName = GetRequiredValue(section, nameof(UserName));
        var password = GetRequiredValue(section, nameof(Password));
        var useWindowsAuthentication = bool.TryParse(section[nameof(UseWindowsAuthentication)], out var parsedAuthentication)
            ? parsedAuthentication
            : throw new InvalidOperationException(
                $"Preflight-Fehler: '{SectionName}:{nameof(UseWindowsAuthentication)}' muss ein Boolean sein.");

        return new SqlIntegrationTestSettings
        {
            Server = ResolveServer(server),
            Database = database,
            UserName = userName,
            Password = password,
            UseWindowsAuthentication = useWindowsAuthentication
        };
    }

    public string CreateDatabaseConnectionString()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = Server,
            InitialCatalog = Database,
            IntegratedSecurity = UseWindowsAuthentication,
            TrustServerCertificate = true
        };

        if (!UseWindowsAuthentication)
        {
            builder.UserID = UserName;
            builder.Password = Password;
        }

        return builder.ConnectionString;
    }

    internal static string ResolveServer(string configuredServer)
    {
        var resolvedServer = Environment.ExpandEnvironmentVariables(configuredServer);
        if (string.IsNullOrWhiteSpace(resolvedServer) || resolvedServer.Contains('%', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Preflight-Fehler: '{SectionName}:{nameof(Server)}' enthält einen nicht auflösbaren Platzhalter.");
        }

        return resolvedServer;
    }

    private static string GetRequiredValue(IConfigurationSection section, string key)
    {
        var value = section[key];
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Preflight-Fehler: '{SectionName}:{key}' muss gesetzt sein.");
    }
}
