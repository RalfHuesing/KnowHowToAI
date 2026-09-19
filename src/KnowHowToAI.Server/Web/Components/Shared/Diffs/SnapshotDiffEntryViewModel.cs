namespace KnowHowToAI.Server.Web.Components.Shared.Diffs;

/// <summary>Unveränderliche, fachlich lesbare Darstellung einer einzelnen Nettoänderung.</summary>
public sealed record SnapshotDiffEntryViewModel(
    string Kind,
    string EntityType,
    string PrimaryId,
    string? SecondaryId = null,
    string? Detail = null,
    string? Before = null,
    string? After = null);
