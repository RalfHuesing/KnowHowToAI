using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Server.Configuration;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KnowHowToAI.Server.Hosting;

internal static class ServiceRegistration
{
    public static IServiceCollection AddSqlStorage(this IServiceCollection services)
    {
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<DatabaseConnectionOptions>>().Value.ToStorageConnectionString());
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<KnowHowToAIOptions>>().Value;
            return new SqlStoragePolicy { CommandTimeoutSeconds = options.Storage.CommandTimeoutSeconds };
        });
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<KnowHowToAIOptions>>().Value;
            return new MigrationPolicy
            {
                LockTimeoutSeconds = options.Migrations.LockTimeoutSeconds,
                ApplyOnStartup = options.Migrations.ApplyOnStartup
            };
        });
        services.AddSingleton<SqlConnectionFactory>();
        services.AddSingleton<EmbeddedMigrationCatalog>();
        services.AddSingleton<ISchemaMigrator, SqlSchemaMigrator>();
        services.AddHostedService<SchemaMigrationHostedService>();

        return services;
    }
}
