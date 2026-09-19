using KnowHowToAI.Server.Web.Components.Layout;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.State;
using Microsoft.Extensions.DependencyInjection;

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

        // Flüchtiger Circuit-State für den Arbeitskontext (Node, Rolle, Lese-Kontext).
        services.AddScoped<WorkspaceState>();

        // Löst URL-Query-Parameter auf Core-ReadContext und KnowledgeContextViewModel auf.
        services.AddScoped<WebReadContextResolver>();

        // Flüchtiger Circuit-State und Lazy-Loading-Datenadapter für den Wissensbaum.
        services.AddScoped<KnowledgeTreeState>();

        return services;
    }
}
