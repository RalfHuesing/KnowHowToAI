using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Retrieval.Search;

/// <summary>
/// Vollstaendige Such-Anfrage fuer IRetrievalRepository.SearchAsync.
/// Kapselt alle Suchparameter einschliesslich Paging und Snippet-Laenge.
/// </summary>
public sealed record SearchRequest(
    SnapshotId SnapshotId,
    string Text,
    AudienceId? AudienceId,
    int Limit,
    string? Cursor,
    int SnippetMaxChars,
    TransactionId? TransactionId = null,
    SearchFilter? Filter = null);
