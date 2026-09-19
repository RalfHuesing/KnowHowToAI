using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Server.Web.State;

/// <summary>
/// Schmale Grenze zur Auflösung von URL-Query-Parametern auf Core-ReadContext und KnowledgeContextViewModel.
/// </summary>
public interface IWebReadContextResolver
{
    Task<Result<WebReadContextResolution>> ResolveAsync(
        string? transactionIdRaw,
        string? snapshotIdRaw,
        string? releaseIdRaw,
        CancellationToken cancellationToken = default);
}
