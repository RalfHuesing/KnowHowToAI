using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>Read-Port für Rollen und deren explizite Auflösungsreihenfolgen.</summary>
public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleResolution>> ListResolutionsBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default);
}
