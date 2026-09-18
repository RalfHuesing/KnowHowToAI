using KnowHowToAI.Server.Web.Components.Layout;
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

        return services;
    }
}
