using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace KnowHowToAI.Server.Web.Workflow;

/// <summary>Ergebnis eines Web-Writes; eine begonnene Transaction bleibt auch bei Mutationsfehlern adressierbar.</summary>
public sealed record WebWriteCoordinatorResult<T>(
    TransactionId? TransactionId,
    Result<T> Mutation,
    bool StartedTransaction);

/// <summary>
/// Gemeinsamer Einstieg für persistente Web-Writes. Der Coordinator delegiert
/// Transaktionsanlage und Mutation an die vorhandenen Application-Use-Cases.
/// </summary>
public sealed class WebWriteCoordinator
{
    private const string Client = "Web UI";
    private const string Purpose = "Wissenspflege";

    private readonly TransactionService _transactionService;
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly WorkspaceState _workspaceState;
    private readonly NavigationManager _navigationManager;
    private readonly SemaphoreSlim _beginGate = new(1, 1);

    public WebWriteCoordinator(
        TransactionService transactionService,
        ISnapshotRepository snapshotRepository,
        ICurrentUserService currentUserService,
        WorkspaceState workspaceState,
        NavigationManager navigationManager)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        _snapshotRepository = snapshotRepository ?? throw new ArgumentNullException(nameof(snapshotRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _workspaceState = workspaceState ?? throw new ArgumentNullException(nameof(workspaceState));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
    }

    public async Task<WebWriteCoordinatorResult<T>> WriteAsync<T>(
        long? loadedCurrentSnapshotId,
        Func<TransactionId, long?, CancellationToken, Task<Result<T>>> mutation,
        Func<T, long?> getChangeVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        ArgumentNullException.ThrowIfNull(getChangeVersion);

        if (_workspaceState.ActiveTransactionId is { } activeTransactionId)
            return await RunMutationAsync(activeTransactionId, _workspaceState.CurrentChangeVersion, false).ConfigureAwait(false);

        if (_workspaceState.CurrentReadContext.SnapshotId.HasValue
            || _workspaceState.CurrentContext.ReadContext != KnowledgeReadContextKind.Current)
        {
            return new WebWriteCoordinatorResult<T>(
                null,
                Result<T>.Failure(new DomainError(
                    ReadContextErrorCodes.InvalidReadContext,
                    "Ein erster Web-Write kann nur vom Current Snapshot aus begonnen werden.")),
                false);
        }

        TransactionId selectedTransactionId;
        long? expectedChangeVersion;
        var startedTransaction = false;
        await _beginGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_workspaceState.ActiveTransactionId is { } transactionId)
            {
                selectedTransactionId = transactionId;
                expectedChangeVersion = _workspaceState.CurrentChangeVersion;
            }
            else
            {
                var currentSnapshot = await _snapshotRepository.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
                if (loadedCurrentSnapshotId is null || currentSnapshot.SnapshotId.Value != loadedCurrentSnapshotId.Value)
                {
                    return new WebWriteCoordinatorResult<T>(
                        null,
                        Result<T>.Failure(CreateCurrentSnapshotConflict(loadedCurrentSnapshotId, currentSnapshot.SnapshotId.Value)),
                        false);
                }

                var beginResult = await _transactionService.BeginAsync(
                    new BeginTransactionOptions(
                        Purpose,
                        _currentUserService.GetCurrentUserName(),
                        Client),
                    cancellationToken).ConfigureAwait(false);

                var transaction = beginResult.Value!;
                selectedTransactionId = transaction.TransactionId;
                expectedChangeVersion = transaction.ChangeVersion;
                startedTransaction = true;
                ApplyTransactionToWorkspace(transaction);
                SelectDraftInUrl(transaction.TransactionId);

                if (transaction.BaseSnapshotId.Value != loadedCurrentSnapshotId.Value)
                {
                    return new WebWriteCoordinatorResult<T>(
                        selectedTransactionId,
                        Result<T>.Failure(CreateCurrentSnapshotConflict(loadedCurrentSnapshotId, transaction.BaseSnapshotId.Value)),
                        true);
                }
            }
        }
        finally
        {
            _beginGate.Release();
        }

        return await RunMutationAsync(selectedTransactionId, expectedChangeVersion, startedTransaction).ConfigureAwait(false);

        async Task<WebWriteCoordinatorResult<T>> RunMutationAsync(
            TransactionId transactionId,
            long? expectedChangeVersion,
            bool startedTransaction)
        {
            var result = await mutation(transactionId, expectedChangeVersion, cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                if (result.Value is { } value && getChangeVersion(value) is { } changeVersion)
                {
                    _workspaceState.SetChangeVersion(changeVersion);
                    _workspaceState.SetContext(
                        _workspaceState.CurrentContext with { ChangeVersion = changeVersion },
                        _workspaceState.CurrentReadContext);
                }
            }

            return new WebWriteCoordinatorResult<T>(transactionId, result, startedTransaction);
        }
    }

    private void ApplyTransactionToWorkspace(KnowledgeTransaction transaction)
    {
        var context = _workspaceState.CurrentContext with
        {
            ReadContext = KnowledgeReadContextKind.Transaction,
            ContextId = transaction.TransactionId.Value.ToString("D"),
            DisplayName = Purpose,
            BaseSnapshotId = transaction.BaseSnapshotId.Value,
            ChangeVersion = transaction.ChangeVersion,
            IsDirty = _workspaceState.IsDirty
        };
        _workspaceState.SetContext(context, new ReadContext(TransactionId: transaction.TransactionId));
        _workspaceState.SetChangeVersion(transaction.ChangeVersion);
        _workspaceState.SetLoadedSnapshotId(transaction.BaseSnapshotId.Value);
    }

    private void SelectDraftInUrl(TransactionId transactionId)
    {
        var currentUri = _navigationManager.ToAbsoluteUri(_navigationManager.Uri);
        var query = QueryHelpers.ParseQuery(currentUri.Query)
            .ToDictionary(pair => pair.Key, pair => (string?)pair.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);
        query.Remove("snapshotId");
        query.Remove("releaseId");
        query["transactionId"] = transactionId.Value.ToString("D");
        var target = QueryHelpers.AddQueryString(currentUri.AbsolutePath, query);
        if (!string.Equals(currentUri.PathAndQuery, target, StringComparison.OrdinalIgnoreCase))
            _navigationManager.NavigateTo(target, replace: true);
    }

    private static DomainError CreateCurrentSnapshotConflict(long? expectedSnapshotId, long actualSnapshotId) =>
        new(
            TransactionValidationErrorCodes.SnapshotConflict,
            "Der Current Snapshot wurde seit dem Laden geändert. Laden Sie den aktuellen Stand neu, bevor Sie schreiben.",
            new Dictionary<string, string>
            {
                [TransactionValidationErrorCodes.BaseSnapshotIdDetail] = expectedSnapshotId?.ToString() ?? "",
                [TransactionValidationErrorCodes.CurrentSnapshotIdDetail] = actualSnapshotId.ToString()
            });
}
