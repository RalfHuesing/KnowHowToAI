using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.Navigation;

public sealed record ResolvedNodeContent(
    NodeContent? Content,
    RoleId RequestedRole,
    RoleId? ResolvedRole,
    Availability Availability,
    bool FallbackUsed,
    Freshness Freshness)
{
    public RoleId RequestedRoleId => RequestedRole;
    public RoleId? ResolvedRoleId => ResolvedRole;
}

public sealed record NodeContentResolutionRequest(
    NodeId? NodeId,
    RoleId RequestedRole,
    SnapshotId SnapshotId,
    IReadOnlyList<Role> Roles,
    IReadOnlyList<RoleResolution> Resolutions,
    IReadOnlyList<NodeContent> Contents,
    IReadOnlyList<ContentDependency> Dependencies)
{
    public RoleId RequestedRoleId => RequestedRole;
}

/// <summary>
/// Löst den Node-Content entlang der konfigurierten Rollenreihenfolge auf und
/// berechnet die transitive Freshness des aufgelösten Inhalts.
/// Fehler des RoleResolvers werden als DomainError weitergegeben.
/// </summary>
public static class NodeContentResolver
{
    public static Result<ResolvedNodeContent> Resolve(NodeContentResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Roles);
        ArgumentNullException.ThrowIfNull(request.Resolutions);
        ArgumentNullException.ThrowIfNull(request.Contents);
        ArgumentNullException.ThrowIfNull(request.Dependencies);

        var nodeContents = request.NodeId.HasValue
            ? request.Contents.Where(content => content.NodeId == request.NodeId.Value).ToArray()
            : Array.Empty<NodeContent>();

        var result = RoleResolver.Resolve(new RoleResolutionRequest(
            request.SnapshotId,
            request.NodeId ?? default,
            request.RequestedRole,
            request.Roles,
            request.Resolutions,
            nodeContents));

        if (!result.IsSuccess)
            return Result<ResolvedNodeContent>.Failure(result.Error!);

        var resolved = result.Value!;
        if (resolved.Availability == Availability.None)
        {
            return Result<ResolvedNodeContent>.Success(new ResolvedNodeContent(
                null,
                request.RequestedRole,
                null,
                Availability.None,
                false,
                Freshness.Unknown));
        }

        var content = nodeContents.FirstOrDefault(candidate => candidate.RoleId == resolved.ResolvedRole);
        var freshness = content is not null
            ? FreshnessEvaluator.Evaluate(content, request.Contents, request.Dependencies)
            : Freshness.Unknown;

        return Result<ResolvedNodeContent>.Success(new ResolvedNodeContent(
            content,
            request.RequestedRole,
            resolved.ResolvedRole,
            resolved.Availability,
            resolved.FallbackUsed,
            freshness));
    }
}
