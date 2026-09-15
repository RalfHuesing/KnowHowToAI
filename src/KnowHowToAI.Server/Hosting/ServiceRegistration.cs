using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Server.Configuration;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Migrations;
using KnowHowToAI.Storage.SqlServer.Repositories.History;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.Storage.SqlServer.Repositories.Snapshots;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;
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
        services.AddSingleton<ISnapshotRepository, SqlSnapshotRepository>();
        services.AddSingleton<ITransactionRepository, SqlTransactionRepository>();
        services.AddSingleton<IWorkingSnapshotValidationDataRepository, SqlWorkingSnapshotValidationDataRepository>();
        services.AddSingleton<IHierarchyRepository, SqlHierarchyRepository>();
        services.AddSingleton<IContentRepository, SqlContentRepository>();
        services.AddSingleton<IDependencyRepository, SqlDependencyRepository>();
        services.AddSingleton<IRoleRepository, SqlRoleRepository>();
        services.AddSingleton<IReleaseRepository, SqlReleaseRepository>();
        services.AddHostedService<SchemaMigrationHostedService>();

        return services;
    }
}
