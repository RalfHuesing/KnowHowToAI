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
    private sealed record TransactionSelection(
        TransactionId? TransactionId,
        long? ChangeVersion,
        bool StartedTransaction,
        DomainError? Error);

    private const string Client = "Web UI";
    private const string Purpose = "Wissenspflege";

    private readonly TransactionService _transactionService;
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly WorkspaceState _workspaceState;
    private readonly NavigationManager _navigationManager;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

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

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            if (_workspaceState.ActiveTransactionId is { } activeTransactionId)
            {
                return await RunMutationAsync(
                    activeTransactionId,
                    _workspaceState.CurrentChangeVersion,
                    false,
                    mutation,
                    getChangeVersion,
                    cancellationToken);
            }

            if (!CanBeginFromCurrent())
            {
                return Failure<T>(new DomainError(
                    ReadContextErrorCodes.InvalidReadContext,
                    "Ein erster Web-Write kann nur vom Current Snapshot aus begonnen werden."));
            }

            var selection = await SelectTransactionAsync(loadedCurrentSnapshotId, cancellationToken);
            if (selection.Error is { } error)
                return new WebWriteCoordinatorResult<T>(selection.TransactionId, Result<T>.Failure(error), selection.StartedTransaction);

            return await RunMutationAsync(
                selection.TransactionId!.Value,
                selection.ChangeVersion,
                selection.StartedTransaction,
                mutation,
                getChangeVersion,
                cancellationToken);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private bool CanBeginFromCurrent() =>
        !_workspaceState.CurrentReadContext.SnapshotId.HasValue
        && _workspaceState.CurrentContext.ReadContext == KnowledgeReadContextKind.Current;

    private async Task<TransactionSelection> SelectTransactionAsync(
        long? loadedCurrentSnapshotId,
        CancellationToken cancellationToken)
    {
        if (_workspaceState.ActiveTransactionId is { } transactionId)
            return new TransactionSelection(transactionId, _workspaceState.CurrentChangeVersion, false, null);

        var currentSnapshot = await _snapshotRepository.GetCurrentAsync(cancellationToken);
        if (loadedCurrentSnapshotId is null || currentSnapshot.SnapshotId.Value != loadedCurrentSnapshotId.Value)
        {
            return new TransactionSelection(
                null,
                null,
                false,
                CreateCurrentSnapshotConflict(loadedCurrentSnapshotId, currentSnapshot.SnapshotId.Value));
        }

        var beginResult = await _transactionService.BeginAsync(
            new BeginTransactionOptions(Purpose, _currentUserService.GetCurrentUserName(), Client),
            cancellationToken);

        var transaction = beginResult.Value!;
        ApplyTransactionToWorkspace(transaction);
        SelectDraftInUrl(transaction.TransactionId);

        var conflict = transaction.BaseSnapshotId.Value != loadedCurrentSnapshotId.Value
            ? CreateCurrentSnapshotConflict(loadedCurrentSnapshotId, transaction.BaseSnapshotId.Value)
            : null;
        return new TransactionSelection(transaction.TransactionId, transaction.ChangeVersion, true, conflict);
    }

    private async Task<WebWriteCoordinatorResult<T>> RunMutationAsync<T>(
        TransactionId transactionId,
        long? expectedChangeVersion,
        bool startedTransaction,
        Func<TransactionId, long?, CancellationToken, Task<Result<T>>> mutation,
        Func<T, long?> getChangeVersion,
        CancellationToken cancellationToken)
    {
        var result = await mutation(transactionId, expectedChangeVersion, cancellationToken);
        if (result.IsSuccess && result.Value is { } value && getChangeVersion(value) is { } changeVersion)
        {
            _workspaceState.SetChangeVersion(changeVersion);
            _workspaceState.SetContext(
                _workspaceState.CurrentContext with { ChangeVersion = changeVersion },
                _workspaceState.CurrentReadContext);
        }

        return new WebWriteCoordinatorResult<T>(transactionId, result, startedTransaction);
    }

    private static WebWriteCoordinatorResult<T> Failure<T>(DomainError error) =>
        new(null, Result<T>.Failure(error), false);

    private void ApplyTransactionToWorkspace(KnowledgeTransaction transaction)
    {
        var context = _workspaceState.CurrentContext with
        {
            ReadContext = KnowledgeReadContextKind.Transaction,
            ContextId = transaction.TransactionId.Value.ToString("D"),
            DisplayName = Purpose,
            BaseSnapshotId = transaction.BaseSnapshotId.Value,
            ChangeVersion = transaction.ChangeVersion,
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
