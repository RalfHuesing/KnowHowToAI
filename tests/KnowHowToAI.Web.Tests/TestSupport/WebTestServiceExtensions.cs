using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.Features.Knowledge.Audiences;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.TestSupport;

public static class WebTestServiceExtensions
{
    public static IServiceCollection AddWebPageStates(
        this IServiceCollection services,
        PageRegionState? pageRegionState = null,
        WorkspaceState? workspaceState = null,
        ContextSelectorState? contextSelectorState = null,
        ToastState? toastState = null)
    {
        services.AddSingleton(pageRegionState ?? new PageRegionState());
        services.AddSingleton(workspaceState ?? new WorkspaceState());
        services.AddSingleton<WorkspaceEditState>();
        services.AddSingleton(contextSelectorState ?? new ContextSelectorState());
        services.AddSingleton(toastState ?? new ToastState());
        return services;
    }

    public static IServiceCollection AddKnowledgeTreeWorkspace(
        this IServiceCollection services,
        KnowledgeTreeState treeState)
    {
        services.AddSingleton(treeState);
        services.AddSingleton<IKnowledgeTreeWorkspace>(treeState);
        return services;
    }

    public static IServiceCollection AddKnowledgePageServices(
        this IServiceCollection services,
        NavigationService navigationService,
        ITransactionRepository? transactionRepository = null,
        string defaultAudience = "Developer")
    {
        var defaultStore = new InMemoryKnowledgeStore { CurrentSnapshotId = new SnapshotId(1) };
        defaultStore.Snapshots.Add(new Snapshot(
            new SnapshotId(1), null, SnapshotState.Committed, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
        var treeState = new KnowledgeTreeState(navigationService);
        services.AddSingleton(navigationService);
        services.AddKnowledgeTreeWorkspace(treeState);
        services.AddSingleton(new KnowledgePageContextResolver(
            new InMemorySnapshotRepository(defaultStore),
            transactionRepository ?? new InMemoryTransactionRepository(defaultStore)));
        services.AddSingleton<IAudienceStorageService>(new InMemoryAudienceStorageService(defaultAudience));
        services.AddSingleton<IContextSelectionAudienceCatalog>(new ContextSelectionAudienceCatalog(navigationService));
        return services;
    }

    public static BunitJSModuleInterop SetupAppDialog(this BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule("./Web/Components/Shared/Dialogs/AppDialog.razor.js");
        module.Mode = JSRuntimeMode.Loose;
        return module;
    }
}
