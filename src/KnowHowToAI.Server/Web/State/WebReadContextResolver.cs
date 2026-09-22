using System.Globalization;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;

namespace KnowHowToAI.Server.Web.State;

/// <summary>
/// Ergebnis der Auflösung eines Lese-Kontexts an der Web-Grenze.
/// Enthält den Core-ReadContext für Application-Aufrufe sowie das
/// ViewModel für die globale Wissenskontextleiste.
/// </summary>
public sealed record WebReadContextResolution(
    ReadContext ReadContext,
    KnowledgeContextViewModel ContextViewModel,
    long? ChangeVersion = null,
    long? LoadedSnapshotId = null);

/// <summary>
/// Löst URL-Query-Parameter (transactionId, snapshotId, releaseId) an der Web-Grenze
/// auf den passenden Core-ReadContext und das zugehörige KnowledgeContextViewModel auf.
/// Stellt den gegenseitigen Ausschluss der Selektoren sicher und löst ReleaseId
/// und TransactionId asynchron über deren Repositories auf.
/// </summary>
public sealed class WebReadContextResolver : IWebReadContextResolver
{
    private readonly IReleaseRepository _releaseRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ISnapshotRepository? _snapshotRepository;

    public WebReadContextResolver(
        IReleaseRepository releaseRepository,
        ITransactionRepository transactionRepository,
        ISnapshotRepository? snapshotRepository = null)
    {
        _releaseRepository = releaseRepository ?? throw new ArgumentNullException(nameof(releaseRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _snapshotRepository = snapshotRepository;
    }

    public async Task<Result<WebReadContextResolution>> ResolveAsync(
        string? transactionIdRaw,
        string? snapshotIdRaw,
        string? releaseIdRaw,
        CancellationToken cancellationToken = default)
    {
        var exclusionCheck = ValidateMutualExclusion(transactionIdRaw, snapshotIdRaw, releaseIdRaw);
        if (!exclusionCheck.IsSuccess)
            return Result<WebReadContextResolution>.Failure(exclusionCheck.Error!);

        if (!string.IsNullOrWhiteSpace(transactionIdRaw))
            return await ResolveTransactionAsync(transactionIdRaw, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(snapshotIdRaw))
            return ResolveSnapshot(snapshotIdRaw);

        if (!string.IsNullOrWhiteSpace(releaseIdRaw))
            return await ResolveReleaseAsync(releaseIdRaw, cancellationToken).ConfigureAwait(false);

        long? currentSnapshotId = _snapshotRepository is null
            ? null
            : (await _snapshotRepository.GetCurrentAsync(cancellationToken).ConfigureAwait(false)).SnapshotId.Value;

        return Result<WebReadContextResolution>.Success(new WebReadContextResolution(
            new ReadContext(),
            new KnowledgeContextViewModel(KnowledgeReadContextKind.Current),
            LoadedSnapshotId: currentSnapshotId));
    }

    private static Result<bool> ValidateMutualExclusion(string? txRaw, string? snapRaw, string? relRaw)
    {
        var count = (string.IsNullOrWhiteSpace(txRaw) ? 0 : 1)
            + (string.IsNullOrWhiteSpace(snapRaw) ? 0 : 1)
            + (string.IsNullOrWhiteSpace(relRaw) ? 0 : 1);

        if (count > 1)
        {
            return Result<bool>.Failure(new DomainError(
                ReadContextErrorCodes.InvalidReadContext,
                "Es darf höchstens einer der Parameter 'transactionId', 'snapshotId' oder 'releaseId' gesetzt sein.",
                new Dictionary<string, string>
                {
                    ["transactionId"] = txRaw ?? string.Empty,
                    ["snapshotId"] = snapRaw ?? string.Empty,
                    ["releaseId"] = relRaw ?? string.Empty
                }));
        }

        return Result<bool>.Success(true);
    }

    private async Task<Result<WebReadContextResolution>> ResolveTransactionAsync(
        string transactionIdRaw,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(transactionIdRaw, "D", out var txGuid))
        {
            return Result<WebReadContextResolution>.Failure(new DomainError(
                ReadContextErrorCodes.InvalidReadContext,
                $"Der Wert '{transactionIdRaw}' ist keine gültige Transaktions-ID.",
                new Dictionary<string, string> { ["transactionId"] = transactionIdRaw }));
        }

        var txId = new TransactionId(txGuid);
        var transaction = await _transactionRepository.FindAsync(txId, cancellationToken).ConfigureAwait(false);
        if (transaction is null)
        {
            return Result<WebReadContextResolution>.Failure(new DomainError(
                ReadContextErrorCodes.TransactionNotFound,
                "Die angefragte Transaktion existiert nicht.",
                new Dictionary<string, string> { ["transactionId"] = transactionIdRaw }));
        }

        if (transaction.State != TransactionState.Open)
        {
            return Result<WebReadContextResolution>.Failure(new DomainError(
                ReadContextErrorCodes.TransactionClosed,
                "Die angefragte Transaktion ist bereits geschlossen.",
                new Dictionary<string, string> { ["transactionId"] = transactionIdRaw }));
        }

        var readContext = new ReadContext(TransactionId: txId);
        var contextVm = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Transaction,
            ContextId: txId.Value.ToString("D"),
            DisplayName: string.IsNullOrWhiteSpace(transaction.Purpose)
                ? $"Transaktion {txId.Value:D}"
                : transaction.Purpose,
            BaseSnapshotId: transaction.BaseSnapshotId.Value,
            ChangeVersion: transaction.ChangeVersion);

        return Result<WebReadContextResolution>.Success(new WebReadContextResolution(
            readContext,
            contextVm,
            transaction.ChangeVersion,
            transaction.BaseSnapshotId.Value));
    }

    private static Result<WebReadContextResolution> ResolveSnapshot(string snapshotIdRaw)
    {
        if (!long.TryParse(snapshotIdRaw, NumberStyles.None, CultureInfo.InvariantCulture, out var snapIdLong) || snapIdLong <= 0)
        {
            return Result<WebReadContextResolution>.Failure(new DomainError(
                ReadContextErrorCodes.InvalidReadContext,
                $"Der Wert '{snapshotIdRaw}' ist keine gültige Snapshot-ID.",
                new Dictionary<string, string> { ["snapshotId"] = snapshotIdRaw }));
        }

        var snapId = new SnapshotId(snapIdLong);
        var readContext = new ReadContext(SnapshotId: snapId);
        var contextVm = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Snapshot,
            ContextId: snapId.Value.ToString(CultureInfo.InvariantCulture),
            DisplayName: $"Snapshot {snapId.Value}");
        return Result<WebReadContextResolution>.Success(new WebReadContextResolution(
            readContext,
            contextVm,
            LoadedSnapshotId: snapId.Value));
    }

    private async Task<Result<WebReadContextResolution>> ResolveReleaseAsync(string releaseIdRaw, CancellationToken cancellationToken)
    {
        if (!long.TryParse(releaseIdRaw, NumberStyles.None, CultureInfo.InvariantCulture, out var releaseIdLong) || releaseIdLong <= 0)
        {
            return Result<WebReadContextResolution>.Failure(new DomainError(
                ReadContextErrorCodes.InvalidReadContext,
                $"Der Wert '{releaseIdRaw}' ist keine gültige Release-ID.",
                new Dictionary<string, string> { ["releaseId"] = releaseIdRaw }));
        }

        var releaseId = new ReleaseId(releaseIdLong);
        var release = await _releaseRepository.FindAsync(releaseId, cancellationToken).ConfigureAwait(false);
        if (release is null)
        {
            return Result<WebReadContextResolution>.Failure(new DomainError(
                ReleaseErrorCodes.ReleaseNotFound,
                "Der angefragte Release existiert nicht.",
                new Dictionary<string, string> { [ReleaseErrorCodes.ReleaseIdDetail] = releaseIdRaw }));
        }

        var readContext = new ReadContext(SnapshotId: release.SnapshotId);
        var contextVm = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Release,
            ContextId: release.ReleaseId.Value.ToString(CultureInfo.InvariantCulture),
            DisplayName: release.Name);
        return Result<WebReadContextResolution>.Success(new WebReadContextResolution(
            readContext,
            contextVm,
            LoadedSnapshotId: release.SnapshotId.Value));
    }
}
