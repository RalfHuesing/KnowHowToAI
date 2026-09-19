namespace KnowHowToAI.Server.Web.Features.Dashboard;

/// <summary>
/// UI-ViewModel für eine offene Transaction im Dashboard.
/// </summary>
public sealed record OpenTransactionItemViewModel(
    Guid TransactionId,
    long BaseSnapshotId,
    long WorkingSnapshotId,
    string? Purpose,
    string? Actor,
    string? Client,
    DateTimeOffset CreatedAtUtc,
    long ChangeVersion,
    bool IsOlderThan7Days,
    IReadOnlyList<DashboardDiagnosticViewModel> ValidationErrors);
