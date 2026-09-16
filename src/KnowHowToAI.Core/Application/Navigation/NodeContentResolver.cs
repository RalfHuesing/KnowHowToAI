using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.Navigation;

public sealed record ResolvedNodeContent(
    NodeContent? Content,
    RoleId? ResolvedRoleId,
    Availability Availability,
    bool FallbackUsed);

public sealed record NodeContentResolutionRequest(
    NodeId NodeId,
    RoleId RequestedRoleId,
    SnapshotId SnapshotId,
    IReadOnlyList<Role> Roles,
    IReadOnlyList<RoleResolution> Resolutions,
    IReadOnlyList<NodeContent> Contents);

/// <summary>Löst den Node-Content entlang der konfigurierten Rollenreihenfolge auf.</summary>
public static class NodeContentResolver
{
    public static ResolvedNodeContent ResolveOrUnavailable(NodeContentResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var nodeContents = request.Contents.Where(content => content.NodeId == request.NodeId).ToArray();
        var result = RoleResolver.Resolve(new RoleResolutionRequest(
            request.SnapshotId,
            request.NodeId,
            request.RequestedRoleId,
            request.Roles,
            request.Resolutions,
            nodeContents));

        if (!result.IsSuccess || result.Value!.Availability == Availability.None)
            return new ResolvedNodeContent(null, null, Availability.None, false);

        var resolved = result.Value;
        var content = nodeContents.FirstOrDefault(candidate => candidate.RoleId == resolved.ResolvedRole);
        return new ResolvedNodeContent(content, resolved.ResolvedRole, resolved.Availability, resolved.FallbackUsed);
    }
}
