using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Dashboard;

/// <summary>
/// Unabhängig ladbare Übersicht über den aktuellen Snapshot und den zuletzt veröffentlichten Release.
/// </summary>
public sealed record DashboardSnapshotSummary(Snapshot CurrentSnapshot, Release? LatestRelease);
