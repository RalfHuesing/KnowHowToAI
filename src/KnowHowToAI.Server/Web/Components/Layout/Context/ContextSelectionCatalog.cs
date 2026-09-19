using System.Globalization;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.History;
using Microsoft.Extensions.Logging;

namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Lädt die optionalen Auswahlwerte des vollständigen Kontextselektors und
/// übersetzt sie an der UI-Grenze in darstellbare Werte.
/// </summary>
public sealed class ContextSelectionCatalog : IContextSelectionCatalog
{
    private const int ContextTransactionLimit = 100;

    private readonly ReleaseService _releaseService;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ILogger<ContextSelectionCatalog> _logger;

    public ContextSelectionCatalog(
        ReleaseService releaseService,
        IDashboardRepository dashboardRepository,
        ILogger<ContextSelectionCatalog> logger)
    {
        _releaseService = releaseService ?? throw new ArgumentNullException(nameof(releaseService));
        _dashboardRepository = dashboardRepository ?? throw new ArgumentNullException(nameof(dashboardRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ContextSelectionOptionsViewModel> LoadAsync(CancellationToken cancellationToken = default)
    {
        var releases = await LoadReleasesAsync(cancellationToken);
        var transactions = await LoadTransactionsAsync(cancellationToken);
        return new ContextSelectionOptionsViewModel(releases, transactions, []);
    }

    private async Task<IReadOnlyList<ContextSelectionReleaseOptionViewModel>> LoadReleasesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _releaseService.ListReleasesAsync(limit: 50, cursor: null, cancellationToken);
            return result.IsSuccess && result.Value is { } page
                ? page.Items.Select(release => new ContextSelectionReleaseOptionViewModel(
                    release.ReleaseId.Value.ToString(CultureInfo.InvariantCulture),
                    release.Name,
                    release.SnapshotId.Value)).ToArray()
                : [];
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Releases konnten im Selektor nicht geladen werden.");
            return [];
        }
    }

    private async Task<IReadOnlyList<ContextSelectionTransactionOptionViewModel>> LoadTransactionsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await _dashboardRepository.ListOpenTransactionsAsync(ContextTransactionLimit, cancellationToken);
            return transactions.Select(transaction => new ContextSelectionTransactionOptionViewModel(
                transaction.TransactionId.Value.ToString("D"),
                $"{transaction.Purpose} ({transaction.Actor})")).ToArray();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Offene Transaktionen konnten im Selektor nicht geladen werden.");
            return [];
        }
    }
}
