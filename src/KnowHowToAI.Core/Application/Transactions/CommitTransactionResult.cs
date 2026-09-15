using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Transactions;

/// <summary>
/// Ergebnis eines Commit-Versuchs. Validierungsbefunde bleiben auch bei einer
/// fachlichen Ablehnung sichtbar; Infrastrukturfehler werden weiterhin als Exception geworfen.
/// </summary>
public sealed record CommitTransactionResult(
    KnowledgeTransaction? Transaction,
    TransactionValidationReport? ValidationReport,
    DomainError? Error)
{
    public bool IsCommitted => Transaction is not null && Error is null;
}
