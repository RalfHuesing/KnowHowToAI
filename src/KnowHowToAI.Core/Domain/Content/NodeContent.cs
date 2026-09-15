using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Domain.Content;

public sealed record NodeContent(
    SnapshotId SnapshotId,
    NodeId NodeId,
    RoleId RoleId,
    ContentRevisionId ContentRevisionId,
    ContentMode ContentMode,
    string ContentMd,
    bool IsDeleted);
