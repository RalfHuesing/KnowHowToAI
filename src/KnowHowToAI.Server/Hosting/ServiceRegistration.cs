using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Mutations;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Retrieval.Export;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Application.Runtime;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Server.Configuration;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Migrations;
using KnowHowToAI.Storage.SqlServer.Repositories.History;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.Storage.SqlServer.Repositories.Retrieval;
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
        services.AddSingleton<IWorkingSnapshotReadRepository, SqlWorkingSnapshotReadRepository>();
        services.AddSingleton<IWorkingSnapshotValidationDataRepository, SqlWorkingSnapshotValidationDataRepository>();
        services.AddSingleton<IHierarchyRepository, SqlHierarchyRepository>();
        services.AddSingleton<IContentRepository, SqlContentRepository>();
        services.AddSingleton<IContentMutationRepository, SqlContentMutationRepository>();
        services.AddSingleton<IDependencyRepository, SqlDependencyRepository>();
        services.AddSingleton<INodeMutationRepository, SqlNodeMutationRepository>();
        services.AddSingleton<IRoleRepository, SqlRoleRepository>();
        services.AddSingleton<IRoleMutationRepository, SqlRoleMutationRepository>();
        services.AddSingleton<SqlReleaseRepository>();
        services.AddSingleton<IReleaseRepository>(sp => sp.GetRequiredService<SqlReleaseRepository>());
        services.AddSingleton<IReleaseMutationRepository>(sp => sp.GetRequiredService<SqlReleaseRepository>());
        services.AddSingleton<IRetrievalRepository, SqlRetrievalRepository>();
        services.AddHostedService<SchemaMigrationHostedService>();

        return services;
    }

    /// <summary>
    /// Registriert die Application-Services der Wissens-Engine samt ihrer
    /// konfigurierten Policies und Laufzeitports im DI-Container.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IIdentifierGenerator, GuidIdentifierGenerator>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton(serviceProvider =>
        {
            var validation = serviceProvider
                .GetRequiredService<IOptions<KnowHowToAIOptions>>().Value.Validation;
            return new ValidationPolicy
            {
                ContentSizeWarningBytes = validation.ContentSizeWarningBytes,
                ChildCountWarning = validation.ChildCountWarning,
                HierarchyDepthWarning = validation.HierarchyDepthWarning,
                PossibleEmbeddedHeadingWarning = validation.PossibleEmbeddedHeadingWarning
            };
        });
        services.AddSingleton<TransactionService>();
        services.AddSingleton<NodeMutationService>();
        services.AddSingleton<ContentRevisionService>();
        services.AddSingleton<ContentMutationService>();
        services.AddSingleton<NodeMutationApplicationService>();
        services.AddSingleton<ContentMutationApplicationService>();
        services.AddSingleton<RoleMutationService>();
        services.AddSingleton(serviceProvider =>
        {
            var retrieval = serviceProvider
                .GetRequiredService<IOptions<KnowHowToAIOptions>>().Value.Retrieval;
            return new RetrievalPolicy
            {
                DefaultPageSize = retrieval.DefaultPageSize,
                MaximumPageSize = retrieval.MaximumPageSize,
                SearchPageSize = retrieval.SearchPageSize,
                SearchMaximumPageSize = retrieval.SearchMaximumPageSize,
                SnippetMaximumCharacters = retrieval.SnippetMaximumCharacters
            };
        });
        services.AddSingleton(serviceProvider => new SnapshotReadRepositories(
            serviceProvider.GetRequiredService<ISnapshotRepository>(),
            serviceProvider.GetRequiredService<ITransactionRepository>(),
            serviceProvider.GetRequiredService<IHierarchyRepository>(),
            serviceProvider.GetRequiredService<IContentRepository>(),
            serviceProvider.GetRequiredService<IRoleRepository>(),
            serviceProvider.GetRequiredService<IDependencyRepository>(),
            serviceProvider.GetRequiredService<IWorkingSnapshotReadRepository>()));
        services.AddSingleton(serviceProvider => new SearchRepositories(
            serviceProvider.GetRequiredService<ISnapshotRepository>(),
            serviceProvider.GetRequiredService<ITransactionRepository>(),
            serviceProvider.GetRequiredService<IRetrievalRepository>()));
        services.AddSingleton<NavigationService>();
        services.AddSingleton<SearchService>();
        services.AddSingleton<MarkdownExportService>();
        services.AddSingleton<HistoryService>();
        services.AddSingleton<ReleaseService>();

        return services;
    }
}
