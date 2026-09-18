namespace KnowHowToAI.Server.Web;

/// <summary>
/// Registriert technische Webendpunkte vor den spaeter folgenden Komponentenrouten.
/// </summary>
internal static class WebEndpointRegistration
{
    private static readonly string[] ReservedHttpMethods =
    [
        Microsoft.AspNetCore.Http.HttpMethods.Get,
        Microsoft.AspNetCore.Http.HttpMethods.Post,
        Microsoft.AspNetCore.Http.HttpMethods.Put,
        Microsoft.AspNetCore.Http.HttpMethods.Patch,
        Microsoft.AspNetCore.Http.HttpMethods.Delete,
        Microsoft.AspNetCore.Http.HttpMethods.Head,
        Microsoft.AspNetCore.Http.HttpMethods.Options
    ];

    public static void MapWebEndpoints(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMethods("/api", ReservedHttpMethods, ReturnReservedNotFoundAsync);
        endpoints.MapMethods("/api/{**reservedPath}", ReservedHttpMethods, ReturnReservedNotFoundAsync);
    }

    private static Task ReturnReservedNotFoundAsync(Microsoft.AspNetCore.Http.HttpContext context)
    {
        context.Response.StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    }
}
