using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.Features.Search;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Workflow;
using Microsoft.Extensions.DependencyInjection;
using KnowHowToAI.Server.Web.Features.Knowledge.Tree;

namespace KnowHowToAI.Server.Web;

/// <summary>Registriert die technischen Dienste der Blazor-Serveroberfläche.</summary>
internal static class WebServiceRegistration
{
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Flüchtiger Seitenbereichs-Slot pro Circuit; Fachseiten hängen
        // Breadcrumbs, Aktionen und Kontext ein, ohne das Seitenraster zu kennen.
        services.AddScoped<PageRegionState>();

        // Flüchtiger Zustand der einzigen globalen Toastregion pro Circuit;
        // die Region selbst hostet das Layout.
        services.AddScoped<ToastState>();

        // Flüchtiger Circuit-State für den Arbeitskontext (Node, Zielgruppe, Lese-Kontext).
        services.AddScoped<WorkspaceState>();
        services.AddScoped<WorkspaceEditState>();

        // Gemeinsame Begin-/Resume-Koordination für persistente Web-Writes.
        services.AddScoped<WebWriteCoordinator>();

        // Löst URL-Query-Parameter auf Core-ReadContext und KnowledgeContextViewModel auf.
        services.AddScoped<WebReadContextResolver>();
        services.AddScoped<IWebReadContextResolver>(serviceProvider =>
            serviceProvider.GetRequiredService<WebReadContextResolver>());

        // Flüchtiger Circuit-State und Lazy-Loading-Datenadapter für den Wissensbaum.
        services.AddScoped<KnowledgeTreeState>();

        // Schmale Präsentationsgrenze für Wissensseite und nativen Baum.
        services.AddScoped<IKnowledgeTreeWorkspace>(serviceProvider =>
            serviceProvider.GetRequiredService<KnowledgeTreeState>());
        services.AddScoped<TreeMoveCoordinator>();

        // Persistiert die letzte Zielgruppenauswahl im Browser-LocalStorage.
        services.AddScoped<IAudienceStorageService, BrowserAudienceStorageService>();

        // Flüchtiger Circuit-State für den globalen Zielgruppen- und Lesekontext-Selektor.
        services.AddScoped<ContextSelectorState>();

        // UI-Grenzen für Auswahlwerte und kontextabhängige Zielgruppen im Selektor.
        services.AddScoped<ContextSelectionCatalog>();
        services.AddScoped<IContextSelectionCatalog>(serviceProvider =>
            serviceProvider.GetRequiredService<ContextSelectionCatalog>());
        services.AddScoped<ContextSelectionAudienceCatalog>();
        services.AddScoped<IContextSelectionAudienceCatalog>(serviceProvider =>
            serviceProvider.GetRequiredService<ContextSelectionAudienceCatalog>());

        // Zusammengesetzter Präsentations-Read für Suchtreffer und Breadcrumbs.

        return services;
    }
}
