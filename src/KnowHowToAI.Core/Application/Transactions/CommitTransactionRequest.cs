using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;

namespace KnowHowToAI.Core.Application.Transactions;

/// <summary>Parameter für einen atomaren Commit einschließlich der wirksamen Qualitätsregeln.</summary>
public sealed record CommitTransactionRequest(
    TransactionId TransactionId,
    string? CommitMessage,
    QualityWarningThresholds QualityWarningThresholds,
    bool WarnOnPossibleEmbeddedHeading);
