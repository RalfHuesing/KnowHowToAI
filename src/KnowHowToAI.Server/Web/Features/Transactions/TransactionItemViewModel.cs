namespace KnowHowToAI.Server.Web.Features.Transactions;

/// <summary>
/// UI-ViewModel für eine offene Transaction in der Übersicht.
/// </summary>
public sealed record TransactionItemViewModel(
    Guid TransactionId,
    long BaseSnapshotId,
    long WorkingSnapshotId,
    string? Purpose,
    string? Actor,
    string? Client,
    DateTimeOffset CreatedAtUtc,
    long ChangeVersion,
    bool IsOlderThan7Days);
