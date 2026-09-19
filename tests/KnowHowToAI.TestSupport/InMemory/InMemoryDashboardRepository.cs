using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.TestSupport;

public sealed class InMemoryDashboardRepository : IDashboardRepository
{
    private Release? _latestRelease;
    private readonly List<KnowledgeTransaction> _openTransactions = [];

    public InMemoryDashboardRepository(
        Release? latestRelease = null,
        IEnumerable<KnowledgeTransaction>? openTransactions = null)
    {
        _latestRelease = latestRelease;
        if (openTransactions is not null)
        {
            _openTransactions.AddRange(openTransactions);
        }
    }

    public void SetLatestRelease(Release? release) => _latestRelease = release;

    public void AddOpenTransaction(KnowledgeTransaction transaction) => _openTransactions.Add(transaction);

    public Task<Release?> GetLatestReleaseAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_latestRelease);

    public Task<IReadOnlyList<KnowledgeTransaction>> ListOpenTransactionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<KnowledgeTransaction>>(_openTransactions.ToArray());
}
