using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Retrieval.Search;

/// <summary>
/// Transportneutraler Search-Use-Case (search): Textsuche über Titel, Description und Content.
/// V1 bietet eine deterministische parametrisierte Substring-Suche (ADR-V1-006)
/// und verspricht weder semantische noch linguistische Volltextsuche.
/// Ohne <c>RoleId</c> werden ausschließlich die rollenunabhängigen Felder Title und
/// Description durchsucht. Mit <c>RoleId</c> werden die angefragte aktive Rolle und ihre
/// vollständige Resolution Order über <see cref="RoleResolver.ValidateOrder"/> geprüft;
/// Fehler werden mit denselben stabilen Fehlercodes wie die Rollenauflösung gemeldet und
/// niemals als leeres Ergebnis behandelt.
/// </summary>
public sealed class SearchService
{
    private readonly SearchRepositories _repos;
    private readonly RetrievalPolicy _retrievalPolicy;

    public SearchService(
        SearchRepositories repositories,
        RetrievalPolicy retrievalPolicy)
    {
        ArgumentNullException.ThrowIfNull(repositories);
        ArgumentNullException.ThrowIfNull(retrievalPolicy);
        _repos = repositories;
        _retrievalPolicy = retrievalPolicy;
    }

    /// <summary>
    /// Sucht nach Nodes, die <paramref name="query"/> im Titel, in der Description
    /// oder in aktivem auflösbarem Content enthalten.
    /// </summary>
    public async Task<Result<SearchResultPage>> SearchAsync(
        SearchQuery query,
        ReadContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(query.Text))
        {
            return Result<SearchResultPage>.Success(
                new SearchResultPage(query.Text ?? string.Empty, Array.Empty<SearchHit>(), null));
        }

        var contextResult = await ResolveContextAsync(context, cancellationToken).ConfigureAwait(false);
        if (!contextResult.IsSuccess)
            return Result<SearchResultPage>.Failure(contextResult.Error!);

        var resolvedContext = contextResult.Value!;
        var cursorError = ValidateCursor(query.Cursor, resolvedContext, query);
        if (cursorError is not null)
            return Result<SearchResultPage>.Failure(cursorError);

        var effectiveLimit = ResolveEffectiveLimit(query.Limit);
        var filteredResult = await LoadFilteredHitsAsync(query, resolvedContext, effectiveLimit, cancellationToken)
            .ConfigureAwait(false);
        if (!filteredResult.IsSuccess)
            return Result<SearchResultPage>.Failure(filteredResult.Error!);

        var filtered = filteredResult.Value!;
        var effectiveContext = filtered.ChangeVersion != resolvedContext.ChangeVersion
            ? resolvedContext with { ChangeVersion = filtered.ChangeVersion }
            : resolvedContext;

        var nextCursor = filtered.HasNext && filtered.Items.Count > 0
            ? CreateNextCursor(filtered.Items[^1], effectiveContext, query)
            : null;

        return Result<SearchResultPage>.Success(
            new SearchResultPage(query.Text, filtered.Items, nextCursor));
    }

    private async Task<Result<FilteredSearchHits>> LoadFilteredHitsAsync(
        SearchQuery query,
        ResolvedReadContext resolvedContext,
        int effectiveLimit,
        CancellationToken cancellationToken)
    {
        var items = new List<SearchHit>(effectiveLimit + 1);
        var repositoryCursor = query.Cursor;
        var batchSize = effectiveLimit + 1;
        long? effectiveChangeVersion = resolvedContext.ChangeVersion;

        while (items.Count <= effectiveLimit)
        {
            var request = new SearchRequest(
                resolvedContext.SnapshotId,
                query.Text,
                query.RoleId,
                batchSize,
                repositoryCursor,
                _retrievalPolicy.SnippetMaximumCharacters,
                resolvedContext.TransactionId,
                query.Filter);

            var searchResult = await _repos.Retrieval.SearchAsync(request, cancellationToken).ConfigureAwait(false);
            if (!searchResult.IsSuccess)
                return Result<FilteredSearchHits>.Failure(searchResult.Error!);

            var results = searchResult.Value!;
            effectiveChangeVersion = results.ChangeVersion ?? effectiveChangeVersion;

            var validationError = ValidateBatch(query, resolvedContext, results, effectiveChangeVersion);
            if (validationError is not null)
                return Result<FilteredSearchHits>.Failure(validationError);

            AddMatchingHits(items, results, query.Filter, effectiveLimit);

            if (items.Count > effectiveLimit || results.Count < batchSize)
                return Result<FilteredSearchHits>.Success(new FilteredSearchHits(
                    items.Take(effectiveLimit).ToArray(),
                    items.Count > effectiveLimit,
                    effectiveChangeVersion));

            repositoryCursor = CreateNextCursor(results[^1], resolvedContext with { ChangeVersion = effectiveChangeVersion }, query);
        }

        return Result<FilteredSearchHits>.Success(new FilteredSearchHits(
            items.Take(effectiveLimit).ToArray(),
            items.Count > effectiveLimit,
            effectiveChangeVersion));
    }

    private static DomainError? ValidateBatch(
        SearchQuery query,
        ResolvedReadContext resolvedContext,
        SearchRepositoryResult results,
        long? effectiveChangeVersion) =>
        ValidateRequestedRoleResolution(query, resolvedContext, results)
        ?? ValidateLockedCursorChangeVersion(query.Cursor, resolvedContext.Source, effectiveChangeVersion);

    private static void AddMatchingHits(
        ICollection<SearchHit> target,
        IReadOnlyList<SearchHit> hits,
        SearchFilter? filter,
        int effectiveLimit)
    {
        foreach (var hit in hits)
        {
            if (filter is null || filter.IsEmpty || filter.Matches(hit))
                target.Add(hit);

            if (target.Count > effectiveLimit)
                return;
        }
    }

    private static DomainError? ValidateRequestedRoleResolution(
        SearchQuery query,
        ResolvedReadContext resolvedContext,
        SearchRepositoryResult results)
    {
        if (query.RoleId is not { } requestedRole)
            return null;

        var validation = RoleResolver.ValidateOrder(
            resolvedContext.SnapshotId,
            requestedRole,
            results.Roles ?? Array.Empty<Role>(),
            results.Resolutions ?? Array.Empty<RoleResolution>());
        return validation.IsSuccess ? null : validation.Error;
    }

    private int ResolveEffectiveLimit(int? limit) =>
        limit is { } positiveLimit && positiveLimit > 0
            ? Math.Min(positiveLimit, _retrievalPolicy.SearchMaximumPageSize)
            : _retrievalPolicy.SearchPageSize;

    private static DomainError? ValidateLockedCursorChangeVersion(
        string? cursor,
        ReadContextSource source,
        long? effectiveChangeVersion)
    {
        if (cursor is null || source != ReadContextSource.Transaction || effectiveChangeVersion is null)
            return null;

        var parsedCursor = SearchCursor.TryDecode(cursor);
        return parsedCursor is not null && parsedCursor.ChangeVersion != effectiveChangeVersion
            ? new DomainError(
                SearchErrorCodes.CursorExpired,
                "Der Cursor ist nach einer zwischenzeitlichen Mutation der Transaktion abgelaufen.",
                new Dictionary<string, string> { [SearchErrorCodes.CursorDetail] = cursor })
            : null;
    }

    private Task<Result<ResolvedReadContext>> ResolveContextAsync(
        ReadContext context,
        CancellationToken cancellationToken) =>
        ReadContextReader.ResolveAsync(context, _repos.Snapshots, _repos.Transactions, cancellationToken);

    private static DomainError? ValidateCursor(string? cursor, ResolvedReadContext resolvedContext, SearchQuery query)
    {
        if (cursor is null)
            return null;

        var parsedCursor = SearchCursor.TryDecode(cursor);
        if (parsedCursor is null)
        {
            return new DomainError(
                SearchErrorCodes.InvalidCursor,
                "Der Cursor ist ungültig oder abgelaufen.",
                new Dictionary<string, string> { [SearchErrorCodes.CursorDetail] = cursor });
        }

        if (parsedCursor.SnapshotId != resolvedContext.SnapshotId)
        {
            if (resolvedContext.Source == ReadContextSource.Current)
            {
                return new DomainError(
                    SearchErrorCodes.CursorExpired,
                    "Der Cursor ist nach einer zwischenzeitlichen Aktualisierung des aktuellen Snapshots abgelaufen.",
                    new Dictionary<string, string> { [SearchErrorCodes.CursorDetail] = cursor });
            }

            return new DomainError(
                SearchErrorCodes.InvalidCursor,
                "Der Cursor gehört nicht zu diesem Snapshot.",
                new Dictionary<string, string> { [SearchErrorCodes.CursorDetail] = cursor });
        }

        if (!string.Equals(parsedCursor.QueryText, query.Text, StringComparison.Ordinal)
            || parsedCursor.RoleId != query.RoleId
            || !string.Equals(parsedCursor.FilterFingerprint, query.Filter?.Fingerprint, StringComparison.Ordinal))
        {
            return new DomainError(
                SearchErrorCodes.InvalidCursor,
                "Der Cursor gehört nicht zu dieser Suchanfrage.",
                new Dictionary<string, string> { [SearchErrorCodes.CursorDetail] = cursor });
        }

        if (resolvedContext.ChangeVersion.HasValue || parsedCursor.ChangeVersion.HasValue)
        {
            if (parsedCursor.ChangeVersion != resolvedContext.ChangeVersion)
            {
                return new DomainError(
                    SearchErrorCodes.CursorExpired,
                    "Der Cursor ist nach einer zwischenzeitlichen Mutation der Transaktion abgelaufen.",
                    new Dictionary<string, string> { [SearchErrorCodes.CursorDetail] = cursor });
            }
        }

        return null;
    }

    private static string CreateNextCursor(SearchHit lastHit, ResolvedReadContext resolvedContext, SearchQuery query)
    {
        var lastRank = GetHitRank(lastHit.HitField);
        return new SearchCursor(
            resolvedContext.SnapshotId,
            resolvedContext.ChangeVersion,
            query.Text,
            query.RoleId,
            lastRank,
            lastHit.SortOrder,
            lastHit.NodeId,
            query.Filter?.Fingerprint).Encode();
    }

    private static int GetHitRank(string hitField) => hitField switch
    {
        "Title" => 1,
        "Description" => 2,
        "Content" => 3,
        _ => 3
    };

    private sealed record FilteredSearchHits(
        IReadOnlyList<SearchHit> Items,
        bool HasNext,
        long? ChangeVersion);
}
