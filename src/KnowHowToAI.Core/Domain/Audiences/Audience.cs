using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Audiences;

public sealed record Audience(
    SnapshotId SnapshotId,
    AudienceId AudienceId,
    string Name,
    string? Description,
    bool IsDeleted) : ITombstoned;
