using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Application.Navigation;

public sealed record ResolvedNodeContent(
    NodeContent? Content,
    AudienceId RequestedAudience,
    AudienceId? ResolvedAudience,
    Availability Availability,
    bool FallbackUsed,
    Freshness Freshness)
{
    public AudienceId RequestedAudienceId => RequestedAudience;
    public AudienceId? ResolvedAudienceId => ResolvedAudience;
}

public sealed record NodeContentResolutionRequest(
    NodeId? NodeId,
    AudienceId RequestedAudience,
    SnapshotId SnapshotId,
    IReadOnlyList<Audience> Audiences,
    IReadOnlyList<AudienceResolution> Resolutions,
    IReadOnlyList<NodeContent> Contents,
    IReadOnlyList<ContentDependency> Dependencies)
{
    public AudienceId RequestedAudienceId => RequestedAudience;
}

/// <summary>
/// Löst den Node-Content entlang der konfigurierten Zielgruppenreihenfolge auf und
/// berechnet die transitive Freshness des aufgelösten Inhalts.
/// Fehler des AudienceResolvers werden als DomainError weitergegeben.
/// </summary>
public static class NodeContentResolver
{
    public static Result<ResolvedNodeContent> Resolve(NodeContentResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Audiences);
        ArgumentNullException.ThrowIfNull(request.Resolutions);
        ArgumentNullException.ThrowIfNull(request.Contents);
        ArgumentNullException.ThrowIfNull(request.Dependencies);

        var nodeContents = request.NodeId.HasValue
            ? request.Contents.Where(content => content.NodeId == request.NodeId.Value).ToArray()
            : Array.Empty<NodeContent>();

        var result = AudienceResolver.Resolve(new AudienceResolutionRequest(
            request.SnapshotId,
            request.NodeId ?? default,
            request.RequestedAudience,
            request.Audiences,
            request.Resolutions,
            nodeContents));

        if (!result.IsSuccess)
            return Result<ResolvedNodeContent>.Failure(result.Error!);

        var resolved = result.Value!;
        if (resolved.Availability == Availability.None)
        {
            return Result<ResolvedNodeContent>.Success(new ResolvedNodeContent(
                null,
                request.RequestedAudience,
                null,
                Availability.None,
                false,
                Freshness.Unknown));
        }

        var content = nodeContents.FirstOrDefault(candidate => candidate.AudienceId == resolved.ResolvedAudience);
        var freshness = content is not null
            ? FreshnessEvaluator.Evaluate(content, request.Contents, request.Dependencies)
            : Freshness.Unknown;

        return Result<ResolvedNodeContent>.Success(new ResolvedNodeContent(
            content,
            request.RequestedAudience,
            resolved.ResolvedAudience,
            resolved.Availability,
            resolved.FallbackUsed,
            freshness));
    }
}
