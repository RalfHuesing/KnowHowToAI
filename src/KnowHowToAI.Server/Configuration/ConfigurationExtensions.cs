using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KnowHowToAI.Server.Configuration;

/// <summary>
/// Registriert die KnowHowToAI-Options einmalig im DI-Container und aktiviert Fail-fast-Validierung beim Start.
/// </summary>
internal static class ConfigurationExtensions
{
    /// <summary>
    /// Bindet <see cref="KnowHowToAIOptions"/> aus dem Konfigurationsabschnitt "KnowHowToAI",
    /// registriert <see cref="KnowHowToAIOptionsValidator"/> und fordert Validierung beim Start.
    ///
    /// Override-Reihenfolge (aufsteigend, später gewinnt):
    ///   1. appsettings.json
    ///   2. appsettings.{Environment}.json
    ///   3. Environment-Variablen mit Doppelunterstrich als Trennzeichen (z.B. KnowHowToAI__Retrieval__MaximumPageSize=200)
    ///   4. Kommandozeilenargumente
    ///
    /// Die separate <c>DatabaseConnection</c>-Sektion wird bewusst nicht hier gebunden.
    /// Sie stammt ausschließlich aus der versionierten App-Konfiguration und erhält
    /// keine alternative Connection-String-Umgebungsvariable.
    /// </summary>
    public static IServiceCollection AddKnowHowToAIOptions(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services
            .AddOptions<KnowHowToAIOptions>()
            .Bind(configuration.GetSection(KnowHowToAIOptions.SectionName))
            .ValidateOnStart();

        services
            .AddOptions<DatabaseConnectionOptions>()
            .Bind(configuration.GetSection(DatabaseConnectionOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<KnowHowToAIOptions>, KnowHowToAIOptionsValidator>();
        services.AddSingleton<IValidateOptions<DatabaseConnectionOptions>, DatabaseConnectionOptionsValidator>();

        return services;
    }
}
