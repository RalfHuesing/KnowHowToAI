using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;

namespace KnowHowToAI.Web.Tests.TestSupport;

public static class WebTestServiceExtensions
{
    /// <summary>
    /// Registriert die grundlegenden UI-Zustände (PageRegionState, WorkspaceState, ContextSelectorState, ToastState).
    /// </summary>
    public static IServiceCollection AddWebPageStates(
        this IServiceCollection services,
        PageRegionState? pageRegionState = null,
        WorkspaceState? workspaceState = null,
        ContextSelectorState? contextSelectorState = null,
        ToastState? toastState = null)
    {
        services.AddSingleton(pageRegionState ?? new PageRegionState());
        services.AddSingleton(workspaceState ?? new WorkspaceState());
        services.AddSingleton(contextSelectorState ?? new ContextSelectorState());
        services.AddSingleton(toastState ?? new ToastState());
        return services;
    }

    /// <summary>
    /// Registriert KnowledgeTreeState sowohl als konkreten Typ als auch als IKnowledgeTreeWorkspace.
    /// </summary>
    public static IServiceCollection AddKnowledgeTreeWorkspace(
        this IServiceCollection services,
        KnowledgeTreeState treeState)
    {
        services.AddSingleton(treeState);
        services.AddSingleton<IKnowledgeTreeWorkspace>(treeState);
        return services;
    }

    /// <summary>
    /// Registriert alle für Knowledge-Pages notwendigen Services in einem Aufruf:
    /// NavigationService, KnowledgeTreeState, IKnowledgeTreeWorkspace, WebReadContextResolver,
    /// IAudienceStorageService und IContextSelectionAudienceCatalog.
    /// </summary>
    public static IServiceCollection AddKnowledgePageServices(
        this IServiceCollection services,
        NavigationService navigationService,
        IReleaseRepository? releaseRepository = null,
        ITransactionRepository? transactionRepository = null,
        string defaultAudience = "Developer")
    {
        var treeState = new KnowledgeTreeState(navigationService);
        var contextResolver = new WebReadContextResolver(
            releaseRepository ?? new InMemoryReleaseRepository(),
            transactionRepository ?? new InMemoryTransactionRepository(new InMemoryKnowledgeStore()));
        services.AddSingleton(navigationService);
        services.AddKnowledgeTreeWorkspace(treeState);
        services.AddSingleton(contextResolver);
        services.AddSingleton<IWebReadContextResolver>(contextResolver);
        services.AddSingleton<IAudienceStorageService>(new InMemoryAudienceStorageService(defaultAudience));
        services.AddSingleton<IContextSelectionAudienceCatalog>(new ContextSelectionAudienceCatalog(navigationService));
        return services;
    }

    /// <summary>
    /// Registriert alle für Search-Pages notwendigen Services:
    /// NavigationService, SearchService, WebReadContextResolver, IAudienceStorageService, IContextSelectionAudienceCatalog.
    /// </summary>
    public static IServiceCollection AddSearchPageServices(
        this IServiceCollection services,
        NavigationService navigationService,
        SearchService searchService,
        IReleaseRepository? releaseRepository = null,
        ITransactionRepository? transactionRepository = null,
        string defaultAudience = "Developer")
    {
        var contextResolver = new WebReadContextResolver(
            releaseRepository ?? new InMemoryReleaseRepository(),
            transactionRepository ?? new InMemoryTransactionRepository(new InMemoryKnowledgeStore()));
        services.AddSingleton(navigationService);
        services.AddSingleton(searchService);
        services.AddSingleton(contextResolver);
        services.AddSingleton<IWebReadContextResolver>(contextResolver);
        services.AddSingleton<IAudienceStorageService>(new InMemoryAudienceStorageService(defaultAudience));
        services.AddSingleton<IContextSelectionAudienceCatalog>(new ContextSelectionAudienceCatalog(navigationService));
        return services;
    }

    /// <summary>
    /// Konfiguriert das Loose-JS-Modul für AppDialog.razor.js.
    /// </summary>
    public static BunitJSModuleInterop SetupAppDialog(this BunitJSInterop jsInterop)
    {
        var module = jsInterop.SetupModule("./Web/Components/Shared/Dialogs/AppDialog.razor.js");
        module.Mode = JSRuntimeMode.Loose;
        return module;
    }
}
