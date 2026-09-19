namespace KnowHowToAI.Server.Web.Features.History;

/// <summary>Ergebnis der Release-Erstellung für die anschließende Darstellung in der Historie.</summary>
public sealed record CreateReleaseViewModel(
    long ReleaseId,
    long SnapshotId,
    string Name,
    IReadOnlyList<string> Findings);
