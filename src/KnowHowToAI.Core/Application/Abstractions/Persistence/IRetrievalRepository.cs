using KnowHowToAI.Core.Application.Retrieval.Search;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>
/// Read-Port für parametrisierte Textsuche über den versionierten Wissensstand.
/// Implementierungsdetails wie SQL-LIKE-Escaping und Snippet-Generierung liegen
/// ausschließlich in der Storage-Schicht; keine dynamische SQL-Konkatenation.
/// </summary>
public interface IRetrievalRepository
{
    /// <summary>
    /// Sucht nach Nodes gemäß <paramref name="request"/>. Gibt maximal
    /// <see cref="SearchRequest.Limit"/> Ergebnisse zurück.
    /// </summary>
    Task<IReadOnlyList<SearchHit>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default);
}
