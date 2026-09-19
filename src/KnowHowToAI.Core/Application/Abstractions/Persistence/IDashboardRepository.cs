using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>
/// Read-Port für aggregierte Dashboard-Abfragen.
/// Enthält ausschließlich fehlende, effizient aggregierbare Reads für das Wissensdashboard.
/// </summary>
public interface IDashboardRepository
{
    /// <summary>
    /// Liefert den zeitlich zuletzt veröffentlichten Release oder <c>null</c>, wenn noch kein Release existiert.
    /// Sortierung erfolgt nach ReleasedAtUtc absteigend, sekundär nach ReleaseId absteigend.
    /// </summary>
    Task<Release?> GetLatestReleaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert alle aktuell offenen Transactions (State = Open), sortiert nach Erstellungszeit absteigend.
    /// </summary>
    Task<IReadOnlyList<KnowledgeTransaction>> ListOpenTransactionsAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
