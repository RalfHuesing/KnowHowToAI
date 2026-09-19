using Microsoft.Extensions.Configuration;

namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Liest die ausschließlich für Browser-E2E manuell bereitgestellte Datenbank aus
/// der einzigen Serverkonfiguration. Der Host übergibt die Werte nur prozesslokal
/// an die veröffentlichte EXE und protokolliert sie nicht.
/// </summary>
internal sealed record BrowserTestDatabaseSettings
{
    internal const string SectionName = "BrowserTestDatabaseConnection";

    public required string Server { get; init; }

    public required string Database { get; init; }

    public required string UserName { get; init; }

    public required string Password { get; init; }

    public required bool UseWindowsAuthentication { get; init; }

    public static BrowserTestDatabaseSettings Load(string repositoryRoot)
    {
        var appSettingsPath = Path.Combine(
            repositoryRoot,
            "src",
            "KnowHowToAI.Server",
            "appsettings.json");
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: false)
            .Build();

        return FromConfiguration(configuration);
    }

    internal static BrowserTestDatabaseSettings FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(SectionName);
        var server = GetRequiredValue(section, nameof(Server));
        var useWindowsAuthentication = bool.TryParse(
            section[nameof(UseWindowsAuthentication)],
            out var parsedAuthentication)
            ? parsedAuthentication
            : throw new InvalidOperationException(
                $"Browser-Testdatenbank: '{SectionName}:{nameof(UseWindowsAuthentication)}' muss ein Boolean sein.");

        return new BrowserTestDatabaseSettings
        {
            Server = ResolveServer(server),
            Database = GetRequiredValue(section, nameof(Database)),
            UserName = useWindowsAuthentication ? section[nameof(UserName)] ?? string.Empty : GetRequiredValue(section, nameof(UserName)),
            Password = useWindowsAuthentication ? section[nameof(Password)] ?? string.Empty : GetRequiredValue(section, nameof(Password)),
            UseWindowsAuthentication = useWindowsAuthentication
        };
    }

    private static string ResolveServer(string configuredServer)
    {
        var resolvedServer = Environment.ExpandEnvironmentVariables(configuredServer);
        if (string.IsNullOrWhiteSpace(resolvedServer) || resolvedServer.Contains('%', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Browser-Testdatenbank: '{SectionName}:{nameof(Server)}' enthält einen nicht auflösbaren Platzhalter.");
        }

        return resolvedServer;
    }

    private static string GetRequiredValue(IConfigurationSection section, string key)
    {
        var value = section[key];
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Browser-Testdatenbank: '{SectionName}:{key}' muss gesetzt sein.");
    }
}
