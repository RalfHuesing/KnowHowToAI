using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Gemeinsamer, transportneutraler Snapshot-Lader für Navigation und Export.
/// Liefert für einen ReadContext entweder die konsistente atomare Working-Sicht
/// (IWorkingSnapshotReadRepository.ReadOpenWorkingAsync) oder ReadContextReader.ResolveAsync
/// plus die fünf Listen-Reads – einheitlich mit ActiveReadFilter-gefilterten Active-Daten.
/// Fehlercodes, Prüfungsreihenfolge, Freshness- und Paging-Ergebnisse bleiben unverändert;
/// der InvalidReadContext-Check liefert ReadContextResolver.
/// </summary>
public static class SnapshotReadDataLoader
{
    public static async Task<Result<SnapshotReadData>> LoadAsync(
        ReadContext context,
        SnapshotReadRepositories repositories,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(repositories);

        if (context.TransactionId is { } workingTransactionId
            && context.SnapshotId is null
            && repositories.WorkingSnapshots is not null)
        {
            return await LoadWorkingAsync(
                context.IncludeDeleted,
                repositories.WorkingSnapshots,
                workingTransactionId,
                cancellationToken).ConfigureAwait(false);
        }

        var contextResult = await ReadContextReader.ResolveAsync(
            context,
            repositories.Snapshots,
            repositories.Transactions,
            cancellationToken).ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Result<SnapshotReadData>.Failure(contextResult.Error!);

        return await LoadCommittedAsync(repositories, contextResult.Value!, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<Result<SnapshotReadData>> LoadWorkingAsync(
        bool includeDeleted,
        IWorkingSnapshotReadRepository workingSnapshots,
        TransactionId workingTransactionId,
        CancellationToken cancellationToken)
    {
        var workingResult = await workingSnapshots
            .ReadOpenWorkingAsync(workingTransactionId, cancellationToken)
            .ConfigureAwait(false);
        if (!workingResult.IsSuccess)
            return Result<SnapshotReadData>.Failure(workingResult.Error!);

        var workingData = workingResult.Value!;
        var resolvedContext = new ResolvedReadContext(
            workingData.Transaction.WorkingSnapshotId,
            ReadContextSource.Transaction,
            workingTransactionId,
            includeDeleted,
            workingData.ChangeVersion);

        return Result<SnapshotReadData>.Success(new SnapshotReadData(
            resolvedContext,
            workingData.Transaction.WorkingSnapshotId,
            ActiveReadFilter.Apply(workingData.Nodes, resolvedContext),
            workingData.Roles,
            workingData.RoleResolutions,
            ActiveReadFilter.Apply(workingData.Contents, resolvedContext),
            workingData.Dependencies,
            workingData.ChangeVersion));
    }

    private static async Task<Result<SnapshotReadData>> LoadCommittedAsync(
        SnapshotReadRepositories repositories,
        ResolvedReadContext resolvedContext,
        CancellationToken cancellationToken)
    {
        var snapshotId = resolvedContext.SnapshotId;
        var nodes = await repositories.Hierarchy.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var roles = await repositories.Roles.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var resolutions = await repositories.Roles
            .ListResolutionsBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var contents = await repositories.Contents.ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        var dependencies = await repositories.Dependencies
            .ListBySnapshotAsync(snapshotId, cancellationToken).ConfigureAwait(false);

        return Result<SnapshotReadData>.Success(new SnapshotReadData(
            resolvedContext,
            snapshotId,
            ActiveReadFilter.Apply(nodes, resolvedContext),
            roles,
            resolutions,
            ActiveReadFilter.Apply(contents, resolvedContext),
            dependencies,
            resolvedContext.ChangeVersion));
    }
}
