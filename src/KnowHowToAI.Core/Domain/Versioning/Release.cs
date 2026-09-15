using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Versioning;

public sealed record Release(
    ReleaseId ReleaseId,
    SnapshotId SnapshotId,
    string Name,
    string? Description,
    DateTimeOffset ReleasedAtUtc);
