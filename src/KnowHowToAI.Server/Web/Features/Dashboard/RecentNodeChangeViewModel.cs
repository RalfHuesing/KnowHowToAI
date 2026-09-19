namespace KnowHowToAI.Server.Web.Features.Dashboard;

/// <summary>
/// UI-ViewModel für einen zuletzt geänderten Node im Dashboard.
/// </summary>
public sealed record RecentNodeChangeViewModel(
    Guid NodeId,
    string Title,
    string ChangeKind);
