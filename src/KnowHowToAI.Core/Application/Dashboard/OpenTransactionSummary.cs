using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Dashboard;

/// <summary>
/// Aggregierte Zusammenfassung einer offenen Transaction für das Dashboard inklusive harter Validierungsfehler.
/// </summary>
public sealed record OpenTransactionSummary(
    KnowledgeTransaction Transaction,
    IReadOnlyList<DomainError> ValidationErrors);
