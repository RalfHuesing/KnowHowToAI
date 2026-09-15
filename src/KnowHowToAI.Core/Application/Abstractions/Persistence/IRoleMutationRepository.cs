using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>
/// Write-Port für Rollen- und Resolution-Order-Mutationen auf einem Working Snapshot.
/// Rollen werden per Soft-Delete/Tombstone entfernt; Resolution Orders werden vollständig
/// atomar ersetzt, da Zuordnungszeilen keine eigene Identität besitzen.
/// </summary>
public interface IRoleMutationRepository
{
    /// <summary>Ersetzt die Rollenliste eines Working Snapshots vollständig.</summary>
    Task SaveRolesAsync(
        SnapshotId snapshotId,
        IReadOnlyList<Role> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ersetzt alle Resolution-Order-Einträge für eine angefragte Rolle atomar.
    /// Bestehende Einträge für <paramref name="requestedRoleId"/> in diesem Snapshot
    /// werden vollständig durch <paramref name="resolutions"/> überschrieben.
    /// </summary>
    Task SaveResolutionsAsync(
        SnapshotId snapshotId,
        RoleId requestedRoleId,
        IReadOnlyList<RoleResolution> resolutions,
        CancellationToken cancellationToken = default);
}
