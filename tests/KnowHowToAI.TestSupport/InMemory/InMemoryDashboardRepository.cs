using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.TestSupport;

public sealed class InMemoryDashboardRepository : IDashboardRepository
{
    private Release? _latestRelease;
    private readonly List<KnowledgeTransaction> _openTransactions = [];
    private Exception? _listOpenFailure;

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

    public void SetListOpenFailure(Exception? exception) => _listOpenFailure = exception;

    public Task<Release?> GetLatestReleaseAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_latestRelease);

    public Task<IReadOnlyList<KnowledgeTransaction>> ListOpenTransactionsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (_listOpenFailure is not null)
        {
            return Task.FromException<IReadOnlyList<KnowledgeTransaction>>(_listOpenFailure);
        }

        return Task.FromResult<IReadOnlyList<KnowledgeTransaction>>(_openTransactions.Take(limit).ToArray());
    }
}
