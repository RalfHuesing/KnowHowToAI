using System.Text;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Retrieval.Export;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Http;

namespace KnowHowToAI.Server.Web.Endpoints;

/// <summary>
/// Stellt den vorhandenen Markdown-Teilbaumexport als zweckgebundenen Browserdownload bereit.
/// </summary>
internal static class MarkdownDownloadEndpoint
{
    private const int MaximumFileNameBaseLength = 120;
    private const string MarkdownMediaType = "text/markdown; charset=utf-8";

    public static void Map(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/downloads/markdown", DownloadAsync);
    }

    private static async Task<IResult> DownloadAsync(
        [AsParameters] MarkdownDownloadRequest request,
        WebReadContextResolver readContextResolver,
        NavigationService navigationService,
        MarkdownExportService markdownExportService,
        HttpContext httpContext)
    {
        if (!Guid.TryParseExact(request.NodeId, "D", out var parsedNodeId))
        {
            return Problem(
                httpContext,
                Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest,
                NavigationErrorCodes.InvalidNodeId,
                "Die Node-ID für den Markdown-Export ist ungültig.");
        }

        if (string.IsNullOrWhiteSpace(request.RoleId))
        {
            return Problem(
                httpContext,
                Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest,
                RoleMutationErrorCodes.RoleIdRequired,
                "Für den Markdown-Export muss eine Rolle ausgewählt sein.");
        }

        var contextResult = await readContextResolver
            .ResolveAsync(request.TransactionId, request.SnapshotId, request.ReleaseId, httpContext.RequestAborted)
            .ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Problem(httpContext, contextResult.Error!);

        var rootNodeId = new NodeId(parsedNodeId);
        var readContext = contextResult.Value!.ReadContext;
        var requestedRole = new RoleId(request.RoleId);
        var nodeResult = await navigationService
            .GetNodeAsync(rootNodeId, readContext, requestedRole, httpContext.RequestAborted)
            .ConfigureAwait(false);
        if (!nodeResult.IsSuccess)
            return Problem(httpContext, nodeResult.Error!);

        var exportResult = await markdownExportService
            .ExportTreeAsync(rootNodeId, readContext, requestedRole, httpContext.RequestAborted)
            .ConfigureAwait(false);
        if (!exportResult.IsSuccess)
            return Problem(httpContext, exportResult.Error!);

        httpContext.Response.Headers.CacheControl = "no-store";
        return Results.File(
            Encoding.UTF8.GetBytes(exportResult.Value!),
            MarkdownMediaType,
            CreateFileName(nodeResult.Value!.Node!.Title, request.RoleId, parsedNodeId));
    }

    private static IResult Problem(HttpContext httpContext, DomainError error) =>
        Problem(httpContext, MapStatusCode(error.Code), error.Code, error.Message);

    private static IResult Problem(HttpContext httpContext, int statusCode, string code, string detail) =>
        Results.Problem(
            statusCode: statusCode,
            title: "Markdown-Export fehlgeschlagen",
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
                ["correlationId"] = httpContext.TraceIdentifier
            });

    private static int MapStatusCode(string errorCode) => errorCode switch
    {
        NavigationErrorCodes.NodeNotFound or
        NavigationErrorCodes.SnapshotNotFound or
        NavigationErrorCodes.TransactionNotFound or
        ReadContextErrorCodes.SnapshotNotFound or
        ReadContextErrorCodes.TransactionNotFound or
        ReleaseErrorCodes.ReleaseNotFound or
        RoleResolutionErrorCodes.CandidateRoleDeleted or
        RoleResolutionErrorCodes.CandidateRoleNotFound or
        RoleResolutionErrorCodes.RequestedRoleDeleted or
        RoleResolutionErrorCodes.RequestedRoleNotFound => Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound,
        NavigationErrorCodes.SnapshotNotCommitted or
        NavigationErrorCodes.TransactionClosed or
        ReadContextErrorCodes.SnapshotNotCommitted or
        ReadContextErrorCodes.TransactionClosed => Microsoft.AspNetCore.Http.StatusCodes.Status409Conflict,
        _ => Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest
    };

    private static string CreateFileName(string title, string roleId, Guid nodeId)
    {
        var titlePart = SanitizeFileNamePart(title);
        var rolePart = SanitizeFileNamePart(roleId);
        var baseName = string.IsNullOrEmpty(titlePart)
            ? nodeId.ToString("D")
            : titlePart;

        if (!string.IsNullOrEmpty(rolePart))
            baseName = $"{baseName}-{rolePart}";

        return $"{baseName[..Math.Min(baseName.Length, MaximumFileNameBaseLength)]}.md";
    }

    private static string SanitizeFileNamePart(string value)
    {
        var result = new StringBuilder(value.Length);
        var previousWasDash = false;
        foreach (var character in value)
        {
            if (IsInvalidFileNameCharacter(character) || character == '-')
            {
                if (!previousWasDash && result.Length > 0)
                    result.Append('-');

                previousWasDash = true;
                continue;
            }

            result.Append(character);
            previousWasDash = character == '-';
        }

        return result.ToString().Trim('-');
    }

    private static bool IsInvalidFileNameCharacter(char character) =>
        char.IsControl(character) || character is '<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*';
}
