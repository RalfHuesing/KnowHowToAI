using Microsoft.Extensions.Configuration;

namespace KnowHowToAI.BrowserTests.TestSupport;

/// <summary>
/// Liest die ausschließlich für Browser-E2E manuell bereitgestellte Datenbank aus
/// der einzigen Serverkonfiguration. Der Host übergibt die Werte nur prozesslokal
/// an die veröffentlichte EXE und protokolliert sie nicht.
/// </summary>
internal sealed record BrowserTestDatabaseSettings
{
    internal const string WorkflowSectionName = "BrowserTestDatabaseConnection";
    internal const string VisualShellSectionName = "BrowserVisualTestDatabaseConnection";

    public required string Server { get; init; }

    public required string Database { get; init; }

    public required string UserName { get; init; }

    public required string Password { get; init; }

    public required bool UseWindowsAuthentication { get; init; }

    public static BrowserTestDatabaseSettings LoadWorkflow(string repositoryRoot) =>
        Load(repositoryRoot, WorkflowSectionName);

    public static BrowserTestDatabaseSettings LoadVisualShell(string repositoryRoot) =>
        Load(repositoryRoot, VisualShellSectionName);

    private static BrowserTestDatabaseSettings Load(string repositoryRoot, string sectionName)
    {
        var appSettingsPath = Path.Combine(
            repositoryRoot,
            "src",
            "KnowHowToAI.Server",
            "appsettings.json");
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: false)
            .Build();

        return FromConfiguration(configuration, sectionName);
    }

    internal static BrowserTestDatabaseSettings FromConfiguration(
        IConfiguration configuration,
        string sectionName)
    {
        var section = configuration.GetRequiredSection(sectionName);
        var server = GetRequiredValue(section, sectionName, nameof(Server));
        var useWindowsAuthentication = bool.TryParse(
            section[nameof(UseWindowsAuthentication)],
            out var parsedAuthentication)
            ? parsedAuthentication
            : throw new InvalidOperationException(
                $"Browser-Testdatenbank: '{sectionName}:{nameof(UseWindowsAuthentication)}' muss ein Boolean sein.");

        return new BrowserTestDatabaseSettings
        {
            Server = ResolveServer(server, sectionName),
            Database = GetRequiredValue(section, sectionName, nameof(Database)),
            UserName = useWindowsAuthentication ? section[nameof(UserName)] ?? string.Empty : GetRequiredValue(section, sectionName, nameof(UserName)),
            Password = useWindowsAuthentication ? section[nameof(Password)] ?? string.Empty : GetRequiredValue(section, sectionName, nameof(Password)),
            UseWindowsAuthentication = useWindowsAuthentication
        };
    }

    private static string ResolveServer(string configuredServer, string sectionName)
    {
        var resolvedServer = Environment.ExpandEnvironmentVariables(configuredServer);
        if (string.IsNullOrWhiteSpace(resolvedServer) || resolvedServer.Contains('%', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Browser-Testdatenbank: '{sectionName}:{nameof(Server)}' enthält einen nicht auflösbaren Platzhalter.");
        }

        return resolvedServer;
    }

    private static string GetRequiredValue(IConfigurationSection section, string sectionName, string key)
    {
        var value = section[key];
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Browser-Testdatenbank: '{sectionName}:{key}' muss gesetzt sein.");
    }
}
