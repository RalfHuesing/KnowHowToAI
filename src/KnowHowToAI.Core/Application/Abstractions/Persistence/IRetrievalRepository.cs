using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>
/// Read-Port für parametrisierte Textsuche über den versionierten Wissensstand.
/// Implementierungsdetails wie SQL-LIKE-Escaping und Snippet-Generierung liegen
/// ausschließlich in der Storage-Schicht; keine dynamische SQL-Konkatenation.
/// Fachliche Fehler (z. B. fehlende oder geschlossene Working-Transaction) werden
/// als stabiler <see cref="DomainError"/> zurückgegeben, niemals als Exception.
/// </summary>
public interface IRetrievalRepository
{
    /// <summary>
    /// Sucht nach Nodes gemäß <paramref name="request"/>. Gibt maximal
    /// <see cref="SearchRequest.Limit"/> Ergebnisse zurück.
    /// </summary>
    Task<Result<SearchRepositoryResult>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default);
}
