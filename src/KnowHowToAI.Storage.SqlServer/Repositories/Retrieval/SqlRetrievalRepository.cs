using Dapper;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Retrieval;

internal sealed class SqlRetrievalRepository : SqlRepository, IRetrievalRepository
{
    internal const string SearchSql = """
        WITH RoleCandidates AS (
            SELECT rr.CandidateRoleId, rr.Priority
            FROM dbo.KnowHowToAI_RoleResolution rr
            INNER JOIN dbo.KnowHowToAI_Role r
                ON r.SnapshotId = rr.SnapshotId
               AND r.RoleId = rr.CandidateRoleId
               AND r.IsDeleted = 0
            WHERE rr.SnapshotId = @snapshotId
              AND rr.RequestedRoleId = @roleId
        ),
        ResolvedContent AS (
            SELECT nc.NodeId, nc.RoleId, nc.ContentRevisionId, nc.ContentMode, nc.ContentMd,
                   ROW_NUMBER() OVER (
                       PARTITION BY nc.NodeId
                       ORDER BY rc.Priority, nc.RoleId
                   ) AS RowNum
            FROM dbo.KnowHowToAI_NodeContent nc
            INNER JOIN RoleCandidates rc
                ON rc.CandidateRoleId = nc.RoleId
            WHERE nc.SnapshotId = @snapshotId
              AND nc.IsDeleted = 0
        ),
        ActiveResolvedContent AS (
            SELECT NodeId, RoleId, ContentRevisionId, ContentMode, ContentMd
            FROM ResolvedContent
            WHERE RowNum = 1
        ),
        MatchedNodes AS (
            SELECT
                n.NodeId,
                n.Title,
                n.Description,
                n.SortOrder,
                arc.RoleId AS ResolvedRoleId,
                arc.ContentRevisionId,
                arc.ContentMode,
                arc.ContentMd,
                CASE
                    WHEN @roleId IS NULL OR arc.RoleId IS NULL THEN 0
                    WHEN arc.RoleId = @roleId THEN 1
                    ELSE 2
                END AS AvailabilityCode,
                CASE
                    WHEN n.Title LIKE @likePattern ESCAPE '\' THEN 1
                    WHEN n.Description LIKE @likePattern ESCAPE '\' THEN 2
                    WHEN arc.ContentMd LIKE @likePattern ESCAPE '\' THEN 3
                    ELSE NULL
                END AS HitRank,
                CASE
                    WHEN n.Title LIKE @likePattern ESCAPE '\' THEN 'Title'
                    WHEN n.Description LIKE @likePattern ESCAPE '\' THEN 'Description'
                    WHEN arc.ContentMd LIKE @likePattern ESCAPE '\' THEN 'Content'
                    ELSE NULL
                END AS HitField
            FROM dbo.KnowHowToAI_Node n
            LEFT JOIN ActiveResolvedContent arc
                ON arc.NodeId = n.NodeId
            WHERE n.SnapshotId = @snapshotId
              AND n.IsDeleted = 0
        )
        SELECT TOP (@limit)
            NodeId,
            Title,
            Description,
            SortOrder,
            ResolvedRoleId,
            ContentRevisionId,
            ContentMode,
            ContentMd,
            AvailabilityCode,
            HitRank,
            HitField
        FROM MatchedNodes
        WHERE HitRank IS NOT NULL
          AND (
              @hasCursor = 0
              OR HitRank > @lastRank
              OR (HitRank = @lastRank AND SortOrder > @lastSortOrder)
              OR (HitRank = @lastRank AND SortOrder = @lastSortOrder AND NodeId > @lastNodeId)
          )
        ORDER BY HitRank ASC, SortOrder ASC, NodeId ASC;
        """;

    private const string ListDependenciesSql = """
        SELECT SnapshotId, TargetNodeId, TargetRoleId, SourceNodeId, SourceRoleId, SourceContentRevisionId
        FROM dbo.KnowHowToAI_ContentDependency
        WHERE SnapshotId = @snapshotId;
        """;

    private const string ListContentsSql = """
        SELECT SnapshotId, NodeId, RoleId, ContentRevisionId, ContentMode, ContentMd, IsDeleted
        FROM dbo.KnowHowToAI_NodeContent
        WHERE SnapshotId = @snapshotId;
        """;

    private const string ListRolesSql = """
        SELECT SnapshotId, RoleId, Name, Description, IsDeleted
        FROM dbo.KnowHowToAI_Role
        WHERE SnapshotId = @snapshotId
        ORDER BY RoleId;
        """;

    private const string ListRoleResolutionsSql = """
        SELECT SnapshotId, RequestedRoleId, CandidateRoleId, Priority
        FROM dbo.KnowHowToAI_RoleResolution
        WHERE SnapshotId = @snapshotId
        ORDER BY RequestedRoleId, Priority;
        """;

    private readonly Func<CancellationToken, Task>? _afterGuardReadForTestAsync;

    public SqlRetrievalRepository(
        SqlConnectionFactory connectionFactory,
        SqlStoragePolicy storagePolicy,
        Func<CancellationToken, Task>? afterGuardReadForTestAsync = null)
        : base(connectionFactory, storagePolicy)
    {
        _afterGuardReadForTestAsync = afterGuardReadForTestAsync;
    }

    /// <inheritdoc />
    public async Task<Result<SearchRepositoryResult>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Text))
            return Result<SearchRepositoryResult>.Success(new SearchRepositoryResult(Array.Empty<SearchHit>()));

        var cursor = SearchCursor.TryDecode(request.Cursor);
        var escapedText = LikeEscaping.Escape(request.Text);
        var parameters = new
        {
            snapshotId = request.SnapshotId.Value,
            roleId = request.RoleId?.Value,
            likePattern = $"%{escapedText}%",
            limit = request.Limit,
            hasCursor = cursor is not null ? 1 : 0,
            lastRank = cursor?.LastRank ?? 0,
            lastSortOrder = cursor?.LastSortOrder ?? 0,
            lastNodeId = cursor?.LastNodeId.Value ?? Guid.Empty
        };

        await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var databaseTransaction = (Microsoft.Data.SqlClient.SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var result = await ExecuteSearchAsync(request, parameters, connection, databaseTransaction, cancellationToken)
                .ConfigureAwait(false);
            return Result<SearchRepositoryResult>.Success(result);
        }
        catch (WorkingSnapshotMutationRejectedException exception)
        {
            await databaseTransaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            return Result<SearchRepositoryResult>.Failure(new DomainError(
                exception.Code,
                exception.Message,
                new Dictionary<string, string>
                {
                    [SearchErrorCodes.TransactionIdDetail] = request.TransactionId!.Value.ToString()
                }));
        }
        catch
        {
            await databaseTransaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<SearchRepositoryResult> ExecuteSearchAsync(
        SearchRequest request,
        object parameters,
        SqlConnection connection,
        Microsoft.Data.SqlClient.SqlTransaction databaseTransaction,
        CancellationToken cancellationToken)
    {
        long? changeVersion = null;
        if (request.TransactionId is { } transactionId)
        {
            var guard = await ReadWorkingSnapshotGuardAsync(connection, databaseTransaction, transactionId, cancellationToken)
                .ConfigureAwait(false);
            ValidateWorkingSnapshotMutationGuard(guard, transactionId);
            changeVersion = guard!.ChangeVersion;

            if (_afterGuardReadForTestAsync is not null)
                await _afterGuardReadForTestAsync(cancellationToken).ConfigureAwait(false);
        }

        (IReadOnlyList<Role> Roles, IReadOnlyList<RoleResolution> Resolutions)? roleData = request.RoleId is not null
            ? await LoadRoleResolutionDataAsync(connection, databaseTransaction, request.SnapshotId.Value, cancellationToken)
                .ConfigureAwait(false)
            : null;

        var rows = (await connection.QueryAsync<SearchHitRow>(
            CreateCommand(SearchSql, parameters, cancellationToken, databaseTransaction)).ConfigureAwait(false)).ToArray();

        if (rows.Length == 0)
        {
            await databaseTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new SearchRepositoryResult(
                Array.Empty<SearchHit>(), changeVersion, roleData?.Roles, roleData?.Resolutions);
        }

        var derivedHitsExist = rows.Any(r => string.Equals(r.ContentMode, SqlPersistedValues.ContentDerived, StringComparison.Ordinal));
        var (allContents, allDependencies) = derivedHitsExist
            ? await LoadDerivedContentsAndDependenciesAsync(connection, databaseTransaction, request.SnapshotId.Value, cancellationToken).ConfigureAwait(false)
            : (null, null);

        await databaseTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        var hits = rows.Select(r => MapRowToSearchHit(r, request, allContents, allDependencies)).ToArray();
        return new SearchRepositoryResult(hits, changeVersion, roleData?.Roles, roleData?.Resolutions);
    }

    private async Task<(IReadOnlyList<NodeContent> Contents, IReadOnlyList<ContentDependency> Dependencies)> LoadDerivedContentsAndDependenciesAsync(
        SqlConnection connection,
        Microsoft.Data.SqlClient.SqlTransaction databaseTransaction,
        long snapshotId,
        CancellationToken cancellationToken)
    {
        var contentRows = await connection.QueryAsync<NodeContentRow>(
            CreateCommand(ListContentsSql, new { snapshotId }, cancellationToken, databaseTransaction)).ConfigureAwait(false);
        var contents = contentRows.Select(SqlRowMapper.ToNodeContent).ToArray();

        var depRows = await connection.QueryAsync<ContentDependencyRow>(
            CreateCommand(ListDependenciesSql, new { snapshotId }, cancellationToken, databaseTransaction)).ConfigureAwait(false);
        var dependencies = depRows.Select(SqlRowMapper.ToContentDependency).ToArray();

        return (contents, dependencies);
    }

    private async Task<(IReadOnlyList<Role> Roles, IReadOnlyList<RoleResolution> Resolutions)> LoadRoleResolutionDataAsync(
        SqlConnection connection,
        Microsoft.Data.SqlClient.SqlTransaction databaseTransaction,
        long snapshotId,
        CancellationToken cancellationToken)
    {
        var roleRows = await connection.QueryAsync<RoleRow>(
            CreateCommand(ListRolesSql, new { snapshotId }, cancellationToken, databaseTransaction)).ConfigureAwait(false);
        var roles = roleRows.Select(SqlRowMapper.ToRole).ToArray();

        var resolutionRows = await connection.QueryAsync<RoleResolutionRow>(
            CreateCommand(ListRoleResolutionsSql, new { snapshotId }, cancellationToken, databaseTransaction)).ConfigureAwait(false);
        var resolutions = resolutionRows.Select(SqlRowMapper.ToRoleResolution).ToArray();

        return (roles, resolutions);
    }

    private static SearchHit MapRowToSearchHit(
        SearchHitRow row,
        SearchRequest request,
        IReadOnlyList<NodeContent>? contents,
        IReadOnlyList<ContentDependency>? dependencies)
    {
        var snippet = row.HitField switch
        {
            "Title" => null,
            "Description" => SnippetExtractor.ExtractSnippet(row.Description, request.Text, request.SnippetMaxChars),
            "Content" => SnippetExtractor.ExtractSnippet(row.ContentMd, request.Text, request.SnippetMaxChars),
            _ => null
        };

        var freshness = DetermineFreshness(row, contents, dependencies, request.SnapshotId);
        var resolvedRoleId = row.ResolvedRoleId is not null ? new RoleId(row.ResolvedRoleId) : (RoleId?)null;

        return new SearchHit(
            new NodeId(row.NodeId),
            row.Title,
            row.Description,
            snippet,
            row.HitField,
            (Availability)row.AvailabilityCode,
            resolvedRoleId,
            freshness,
            row.SortOrder);
    }

    private static Freshness DetermineFreshness(
        SearchHitRow row,
        IReadOnlyList<NodeContent>? contents,
        IReadOnlyList<ContentDependency>? dependencies,
        SnapshotId snapshotId)
    {
        if (row.ResolvedRoleId is null)
            return Freshness.Unknown;

        if (string.Equals(row.ContentMode, SqlPersistedValues.ContentIndependent, StringComparison.Ordinal))
            return Freshness.Current;

        if (string.Equals(row.ContentMode, SqlPersistedValues.ContentDerived, StringComparison.Ordinal)
            && contents is not null
            && dependencies is not null
            && row.ContentRevisionId.HasValue)
        {
            var nodeContent = new NodeContent(
                snapshotId,
                new NodeId(row.NodeId),
                new RoleId(row.ResolvedRoleId),
                new ContentRevisionId(row.ContentRevisionId.Value),
                ContentMode.Derived,
                row.ContentMd ?? string.Empty,
                false);

            return FreshnessEvaluator.Evaluate(nodeContent, contents, dependencies);
        }

        return Freshness.Unknown;
    }

    private sealed class SearchHitRow
    {
        public Guid NodeId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int SortOrder { get; init; }
        public string? ResolvedRoleId { get; init; }
        public Guid? ContentRevisionId { get; init; }
        public string? ContentMode { get; init; }
        public string? ContentMd { get; init; }
        public int AvailabilityCode { get; init; }
        public int HitRank { get; init; }
        public string HitField { get; init; } = string.Empty;
    }
}
