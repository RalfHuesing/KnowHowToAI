using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.History;

/// <summary>
/// Ergebnis fuer get_transaction_changes: vergleicht den Base-Snapshot mit dem
/// Working- oder Committed-Snapshot einer Transaction.
/// </summary>
public sealed record TransactionDiff(
    KnowledgeTransaction Transaction,
    SnapshotDiff Changes);
