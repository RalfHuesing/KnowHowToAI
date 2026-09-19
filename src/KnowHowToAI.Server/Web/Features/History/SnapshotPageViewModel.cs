namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>Unveränderliche UI-Seite einer cursorbasierten Snapshot-Historie.</summary>
public sealed record SnapshotPageViewModel(
    IReadOnlyList<SnapshotViewModel> Items,
    string? NextCursor);
