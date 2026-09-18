using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// In-Memory-<see cref="IRoleRepository"/>: filtert Rollen und Auflösungsreihenfolgen
/// des gemeinsamen <see cref="InMemoryKnowledgeStore"/> nach SnapshotId.
/// </summary>
public sealed class InMemoryRoleRepository(InMemoryKnowledgeStore store) : IRoleRepository
{
    public Task<IReadOnlyList<Role>> ListBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Role>>(
            store.Roles.Where(role => role.SnapshotId == snapshotId).ToArray());

    public Task<IReadOnlyList<RoleResolution>> ListResolutionsBySnapshotAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RoleResolution>>(
            store.Resolutions.Where(resolution => resolution.SnapshotId == snapshotId).ToArray());
}
