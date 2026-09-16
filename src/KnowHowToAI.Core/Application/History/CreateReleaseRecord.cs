using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.History;

/// <summary>
/// Kapselt die Parameter für die atomare Anlage eines Release-Verweises im Persistence-Layer.
/// </summary>
public sealed record CreateReleaseRecord(
    string Name,
    SnapshotId SnapshotId,
    DateTimeOffset CreatedAtUtc,
    string? Description = null);
