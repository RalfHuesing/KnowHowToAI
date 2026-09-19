namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>
/// Hält den Zustand eines aktiven, asynchronen Knoten-Lade-Requests für isolierte Stornierung und Renn-Vermeidung.
/// </summary>
internal sealed record ActiveNodeRequest(
    long RequestId,
    int ContextGeneration,
    CancellationTokenSource Cts);
