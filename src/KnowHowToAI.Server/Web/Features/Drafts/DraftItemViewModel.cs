namespace KnowHowToAI.Server.Web.Features.Drafts;

internal sealed record DraftItemViewModel(
    Guid TransactionId,
    string? Purpose,
    string? Actor,
    DateTimeOffset CreatedAtUtc,
    long ChangeVersion);
