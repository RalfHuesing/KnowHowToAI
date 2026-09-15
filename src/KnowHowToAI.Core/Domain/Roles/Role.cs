using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Roles;

public sealed record Role(
    SnapshotId SnapshotId,
    RoleId RoleId,
    string Name,
    string? Description,
    bool IsDeleted);
