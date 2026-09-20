using Dapper;
using System.Text.Json;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Retrieval;

internal sealed class SqlRetrievalRepository : SqlRepository, IRetrievalRepository
{
    internal const string SearchSql = """
        WITH AudienceCandidates AS (
            SELECT rr.CandidateAudienceId, rr.Priority
            FROM dbo.KnowHowToAI_AudienceResolution rr
            INNER JOIN dbo.KnowHowToAI_Audience r
                ON r.SnapshotId = rr.SnapshotId
               AND r.AudienceId = rr.CandidateAudienceId
               AND r.IsDeleted = 0
            WHERE rr.SnapshotId = @snapshotId
              AND rr.RequestedAudienceId = @audienceId
        ),
        ResolvedContent AS (
            SELECT nc.NodeId, nc.AudienceId, nc.ContentRevisionId, nc.ContentMode, nc.ContentMd,
                   ROW_NUMBER() OVER (
                       PARTITION BY nc.NodeId
                       ORDER BY rc.Priority, nc.AudienceId
                   ) AS RowNum
            FROM dbo.KnowHowToAI_NodeContent nc
            INNER JOIN AudienceCandidates rc
                ON rc.CandidateAudienceId = nc.AudienceId
            WHERE nc.SnapshotId = @snapshotId
              AND nc.IsDeleted = 0
        ),
        ActiveResolvedContent AS (
            SELECT NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd
            FROM ResolvedContent
            WHERE RowNum = 1
        ),
        StaleContents AS (
            SELECT nc.NodeId, nc.AudienceId
            FROM dbo.KnowHowToAI_NodeContent nc
            WHERE nc.SnapshotId = @snapshotId
              AND nc.IsDeleted = 0
              AND nc.ContentMode = 'Derived'
              AND NOT EXISTS (
                  SELECT 1
                  FROM dbo.KnowHowToAI_ContentDependency cd
                  WHERE cd.SnapshotId = nc.SnapshotId
                    AND cd.TargetNodeId = nc.NodeId
                    AND cd.TargetAudienceId = nc.AudienceId)
            UNION ALL
            SELECT cd.TargetNodeId, cd.TargetAudienceId
            FROM dbo.KnowHowToAI_ContentDependency cd
            INNER JOIN dbo.KnowHowToAI_NodeContent target
                ON target.SnapshotId = cd.SnapshotId
               AND target.NodeId = cd.TargetNodeId
               AND target.AudienceId = cd.TargetAudienceId
               AND target.IsDeleted = 0
               AND target.ContentMode = 'Derived'
            LEFT JOIN dbo.KnowHowToAI_NodeContent source
                ON source.SnapshotId = cd.SnapshotId
               AND source.NodeId = cd.SourceNodeId
               AND source.AudienceId = cd.SourceAudienceId
               AND source.IsDeleted = 0
            WHERE cd.SnapshotId = @snapshotId
              AND (source.NodeId IS NULL OR source.ContentRevisionId <> cd.SourceContentRevisionId)
            UNION ALL
            SELECT cd.TargetNodeId, cd.TargetAudienceId
            FROM dbo.KnowHowToAI_ContentDependency cd
            INNER JOIN dbo.KnowHowToAI_NodeContent target
                ON target.SnapshotId = cd.SnapshotId
               AND target.NodeId = cd.TargetNodeId
               AND target.AudienceId = cd.TargetAudienceId
               AND target.IsDeleted = 0
               AND target.ContentMode = 'Derived'
            INNER JOIN StaleContents stale
                ON stale.NodeId = cd.SourceNodeId
               AND stale.AudienceId = cd.SourceAudienceId
            WHERE cd.SnapshotId = @snapshotId
        ),
        MatchedNodes AS (
            SELECT
                n.NodeId,
                n.Title,
                n.Description,
                n.SortOrder,
                arc.AudienceId AS ResolvedAudienceId,
                arc.ContentRevisionId,
                arc.ContentMode,
                arc.ContentMd,
                CASE
                    WHEN @audienceId IS NULL OR arc.AudienceId IS NULL THEN 0
                    WHEN arc.AudienceId = @audienceId THEN 1
                    ELSE 2
                END AS AvailabilityCode,
                CASE
                    WHEN arc.AudienceId IS NULL THEN 0
                    WHEN arc.ContentMode = 'Independent' THEN 1
                    WHEN arc.ContentMode = 'Derived'
                         AND EXISTS (SELECT 1 FROM StaleContents stale WHERE stale.NodeId = arc.NodeId AND stale.AudienceId = arc.AudienceId) THEN 2
                    WHEN arc.ContentMode = 'Derived' THEN 1
                    ELSE 0
                END AS FreshnessCode,
                CASE
                    WHEN arc.ContentMode = 'Derived'
                         AND EXISTS (SELECT 1 FROM StaleContents stale WHERE stale.NodeId = arc.NodeId AND stale.AudienceId = arc.AudienceId)
                        THEN 'StaleDerivedContent'
                    ELSE NULL
                END AS FindingCode,
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
            ResolvedAudienceId,
            ContentRevisionId,
            ContentMode,
            ContentMd,
            AvailabilityCode,
            FreshnessCode,
            FindingCode,
            HitRank,
            HitField
        FROM MatchedNodes
        WHERE HitRank IS NOT NULL
          AND (@hasResolvedAudienceFilter = 0 OR ResolvedAudienceId IN (SELECT [value] FROM OPENJSON(@resolvedAudienceFilter)))
          AND (@hasAvailabilityFilter = 0 OR AvailabilityCode IN (SELECT CONVERT(int, [value]) FROM OPENJSON(@availabilityFilter)))
          AND (@hasFreshnessFilter = 0 OR FreshnessCode IN (SELECT CONVERT(int, [value]) FROM OPENJSON(@freshnessFilter)))
          AND (@hasFindingFilter = 0 OR FindingCode IN (SELECT [value] FROM OPENJSON(@findingFilter)))
          AND (
              @hasCursor = 0
              OR HitRank > @lastRank
              OR (HitRank = @lastRank AND SortOrder > @lastSortOrder)
              OR (HitRank = @lastRank AND SortOrder = @lastSortOrder AND NodeId > @lastNodeId)
          )
        ORDER BY HitRank ASC, SortOrder ASC, NodeId ASC
        OPTION (MAXRECURSION 32767);
        """;

    internal const string ListDependenciesSql = """
        SELECT SnapshotId, TargetNodeId, TargetAudienceId, SourceNodeId, SourceAudienceId, SourceContentRevisionId
        FROM dbo.KnowHowToAI_ContentDependency
        WHERE SnapshotId = @snapshotId;
        """;

    internal const string ListContentsSql = """
        SELECT SnapshotId, NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd, IsDeleted
        FROM dbo.KnowHowToAI_NodeContent
        WHERE SnapshotId = @snapshotId;
        """;

    internal const string ListAudiencesSql = """
        SELECT SnapshotId, AudienceId, Name, Description, IsDeleted
        FROM dbo.KnowHowToAI_Audience
        WHERE SnapshotId = @snapshotId
        ORDER BY AudienceId;
        """;

    internal const string ListAudienceResolutionsSql = """
        SELECT SnapshotId, RequestedAudienceId, CandidateAudienceId, Priority
        FROM dbo.KnowHowToAI_AudienceResolution
        WHERE SnapshotId = @snapshotId
        ORDER BY RequestedAudienceId, Priority;
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
        var filterParameters = CreateFilterParameters(request.Filter);
        var parameters = new
        {
            snapshotId = request.SnapshotId.Value,
            audienceId = request.AudienceId?.Value,
            likePattern = $"%{escapedText}%",
            limit = request.Limit,
            hasCursor = cursor is not null ? 1 : 0,
            lastRank = cursor?.LastRank ?? 0,
            lastSortOrder = cursor?.LastSortOrder ?? 0,
            lastNodeId = cursor?.LastNodeId.Value ?? Guid.Empty,
            hasResolvedAudienceFilter = filterParameters.HasResolvedAudienceFilter,
            resolvedAudienceFilter = filterParameters.ResolvedAudienceFilter,
            hasAvailabilityFilter = filterParameters.HasAvailabilityFilter,
            availabilityFilter = filterParameters.AvailabilityFilter,
            hasFreshnessFilter = filterParameters.HasFreshnessFilter,
            freshnessFilter = filterParameters.FreshnessFilter,
            hasFindingFilter = filterParameters.HasFindingFilter,
            findingFilter = filterParameters.FindingFilter
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

        (IReadOnlyList<Audience> Audiences, IReadOnlyList<AudienceResolution> Resolutions)? audienceData = request.AudienceId is not null
            ? await LoadAudienceResolutionDataAsync(connection, databaseTransaction, request.SnapshotId.Value, cancellationToken)
                .ConfigureAwait(false)
            : null;

        var rows = (await connection.QueryAsync<SearchHitRow>(
            CreateCommand(SearchSql, parameters, cancellationToken, databaseTransaction)).ConfigureAwait(false)).ToArray();

        if (rows.Length == 0)
        {
            await databaseTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new SearchRepositoryResult(
                Array.Empty<SearchHit>(), changeVersion, audienceData?.Audiences, audienceData?.Resolutions);
        }

        await databaseTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        var hits = rows.Select(row => MapRowToSearchHit(row, request)).ToArray();
        return new SearchRepositoryResult(hits, changeVersion, audienceData?.Audiences, audienceData?.Resolutions);
    }

    private static SearchFilterParameters CreateFilterParameters(SearchFilter? filter) => new(
        HasValues(filter?.ResolvedAudienceIds),
        Serialize(filter?.ResolvedAudienceIds?.Select(audience => audience.Value)),
        HasValues(filter?.Availabilities),
        Serialize(filter?.Availabilities?.Select(value => (int)value)),
        HasValues(filter?.Freshnesses),
        Serialize(filter?.Freshnesses?.Select(value => (int)value)),
        HasValues(filter?.FindingCodes),
        Serialize(filter?.FindingCodes));

    private static int HasValues<T>(IReadOnlyList<T>? values) => values is { Count: > 0 } ? 1 : 0;

    private static string Serialize<T>(IEnumerable<T>? values) => JsonSerializer.Serialize(values ?? []);

    private async Task<(IReadOnlyList<Audience> Audiences, IReadOnlyList<AudienceResolution> Resolutions)> LoadAudienceResolutionDataAsync(
        SqlConnection connection,
        Microsoft.Data.SqlClient.SqlTransaction databaseTransaction,
        long snapshotId,
        CancellationToken cancellationToken)
    {
        var audienceRows = await connection.QueryAsync<AudienceRow>(
            CreateCommand(ListAudiencesSql, new { snapshotId }, cancellationToken, databaseTransaction)).ConfigureAwait(false);
        var audiences = audienceRows.Select(SqlRowMapper.ToAudience).ToArray();

        var resolutionRows = await connection.QueryAsync<AudienceResolutionRow>(
            CreateCommand(ListAudienceResolutionsSql, new { snapshotId }, cancellationToken, databaseTransaction)).ConfigureAwait(false);
        var resolutions = resolutionRows.Select(SqlRowMapper.ToAudienceResolution).ToArray();

        return (audiences, resolutions);
    }

    private static SearchHit MapRowToSearchHit(
        SearchHitRow row,
        SearchRequest request)
    {
        var snippet = row.HitField switch
        {
            "Title" => null,
            "Description" => SnippetExtractor.ExtractSnippet(row.Description, request.Text, request.SnippetMaxChars),
            "Content" => SnippetExtractor.ExtractSnippet(row.ContentMd, request.Text, request.SnippetMaxChars),
            _ => null
        };

        var freshness = (Freshness)row.FreshnessCode;
        var resolvedAudienceId = row.ResolvedAudienceId is not null ? new AudienceId(row.ResolvedAudienceId) : (AudienceId?)null;

        var findings = row.FindingCode is null ? Array.Empty<string>() : [row.FindingCode];

        return new SearchHit(
            new NodeId(row.NodeId),
            row.Title,
            row.Description,
            snippet,
            row.HitField,
            (Availability)row.AvailabilityCode,
            resolvedAudienceId,
            freshness,
            row.SortOrder,
            findings);
    }

    private sealed class SearchHitRow
    {
        public Guid NodeId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int SortOrder { get; init; }
        public string? ResolvedAudienceId { get; init; }
        public Guid? ContentRevisionId { get; init; }
        public string? ContentMode { get; init; }
        public string? ContentMd { get; init; }
        public int AvailabilityCode { get; init; }
        public int FreshnessCode { get; init; }
        public string? FindingCode { get; init; }
        public int HitRank { get; init; }
        public string HitField { get; init; } = string.Empty;
    }

    private sealed record SearchFilterParameters(
        int HasResolvedAudienceFilter,
        string ResolvedAudienceFilter,
        int HasAvailabilityFilter,
        string AvailabilityFilter,
        int HasFreshnessFilter,
        string FreshnessFilter,
        int HasFindingFilter,
        string FindingFilter);
}
