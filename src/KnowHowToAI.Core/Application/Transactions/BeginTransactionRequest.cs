using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Transactions;

/// <summary>
/// Transportneutraler Auftrag zum Eröffnen einer KnowHowTo-AI-Transaction.
/// Die Auditfelder beschreiben den Aufrufer, nicht den versionierten Wissensstand.
/// </summary>
public sealed record BeginTransactionRequest(
    TransactionId TransactionId,
    string? Purpose = null,
    string? Actor = null,
    string? Client = null);
