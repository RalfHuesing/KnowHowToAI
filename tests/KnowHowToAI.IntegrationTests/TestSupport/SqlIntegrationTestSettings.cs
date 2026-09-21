using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.IntegrationTests.TestSupport;

/// <summary>
/// Liest die für echte SQL-Integrationstests bestimmte Datenbankverbindung aus der
/// produktiven App-Konfiguration. Platzhalter im Serverwert werden erst zur Laufzeit
/// expandiert, damit die versionierte Konfiguration keinen konkreten Rechnernamen enthält.
/// </summary>
internal sealed record SqlIntegrationTestSettings
{
    internal const string ProductSectionName = "DatabaseConnection";
    internal const string SectionName = "BrowserTestDatabaseConnection";
    internal const string VisualSectionName = "BrowserVisualTestDatabaseConnection";

    public required string Server { get; init; }
    public required string Database { get; init; }
    public required string UserName { get; init; }
    public required string Password { get; init; }
    public required bool UseWindowsAuthentication { get; init; }

    internal SqlCleanupTarget CleanupTarget => new(Server, Database);

    public static SqlIntegrationTestSettings Load() => LoadSection(SectionName);

    internal static SqlIntegrationTestSettings LoadProduct() => LoadSection(ProductSectionName);

    internal static SqlIntegrationTestSettings LoadVisual() => LoadSection(VisualSectionName);

    private static SqlIntegrationTestSettings LoadSection(string sectionName)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();
        var section = configuration.GetRequiredSection(sectionName);

        var server = GetRequiredValue(section, nameof(Server), sectionName);
        var database = GetRequiredValue(section, nameof(Database), sectionName);
        var userName = GetRequiredValue(section, nameof(UserName), sectionName);
        var password = GetRequiredValue(section, nameof(Password), sectionName);
        var useWindowsAuthentication = bool.TryParse(section[nameof(UseWindowsAuthentication)], out var parsedAuthentication)
            ? parsedAuthentication
            : throw new InvalidOperationException(
                $"Preflight-Fehler: '{sectionName}:{nameof(UseWindowsAuthentication)}' muss ein Boolean sein.");

        return new SqlIntegrationTestSettings
        {
            Server = ResolveServer(server, sectionName),
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

    internal static string ResolveServer(string configuredServer, string sectionName = SectionName)
    {
        var resolvedServer = Environment.ExpandEnvironmentVariables(configuredServer);
        if (string.IsNullOrWhiteSpace(resolvedServer) || resolvedServer.Contains('%', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Preflight-Fehler: '{sectionName}:{nameof(Server)}' enthält einen nicht auflösbaren Platzhalter.");
        }

        return resolvedServer;
    }

    private static string GetRequiredValue(IConfigurationSection section, string key, string sectionName = SectionName)
    {
        var value = section[key];
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Preflight-Fehler: '{sectionName}:{key}' muss gesetzt sein.");
    }
}
