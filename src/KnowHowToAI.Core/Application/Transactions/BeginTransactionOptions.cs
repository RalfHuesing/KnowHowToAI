namespace KnowHowToAI.Core.Application.Transactions;

/// <summary>Transportneutrale Audit-Metadaten für das Eröffnen einer Transaction.</summary>
public sealed record BeginTransactionOptions(
    string? Purpose = null,
    string? Actor = null,
    string? Client = null);
