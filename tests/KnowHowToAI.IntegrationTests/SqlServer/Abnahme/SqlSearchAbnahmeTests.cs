using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.Retrieval;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Abnahme;

/// <summary>
/// M5.12-Abnahme: erzeugt eine repraesentative Testgroesse ausschliesslich über
/// Working-Transaction/Commit-Pfade, weist die Suchsemantik nach und misst für die
/// reale parametrisierte Search-Query Ausführungsplan, logische Reads und Laufzeit.
/// Die Messwerte werden nach temp/search-abnahme-messung.json geschrieben.
/// </summary>
[Trait("Category", "ManualDatabaseIntegration")]
[Collection("ManualDatabaseIntegration")]
public sealed partial class SqlSearchAbnahmeTests
{
    private const int ThemaNodeCount = 400;
    private const int TitleHitStride = 10;
    private const int DescriptionHitStride = 8;
    private const int ContentHitStride = 4;
    private const int FallbackContentStride = 20;
    private const string MeasurementMarker = "M5.12-Abnahme";

    private static readonly RoleId RoleEntwickler = new("Entwickler");

    [Fact]
    public async Task SearchQuery_RepraesentativeDatenmenge_NachweisInPlanUndLaufzeit()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var snapshotId = await SeedRepresentativeDatasetAsync(database);
        var repository = new SqlRetrievalRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 60 });

        var ohneRolle = await PageThroughAllHitsAsync(repository, snapshotId, "Installation", null, 25);
        Assert.Equal(ThemaNodeCount / TitleHitStride, ohneRolle.Count);
        Assert.All(ohneRolle, hit => Assert.Equal("Title", hit.HitField));

        var mitRolle = await PageThroughAllHitsAsync(repository, snapshotId, "Ablauf", RoleEntwickler, 25);
        var erwarteteTreffer = ThemaNodeCount / ContentHitStride;
        Assert.Equal(erwarteteTreffer, mitRolle.Count);
        Assert.All(mitRolle, hit => Assert.Equal(Availability.Explicit, hit.Availability));

        var measurements = await MeasureSearchVariantsAsync(database, snapshotId, repository);
        Assert.All(measurements, measurement =>
        {
            Assert.True(measurement.LogicalReadsTotal > 0, $"Keine Reads erfasst: {measurement.Name}");
            Assert.True(measurement.LogicalReadsTotal < 100_000, $"Unerwartet hohe Reads: {measurement.Name}");
            Assert.True(measurement.WallClockMilliseconds < 2000, $"Unerwartet langsam: {measurement.Name}");
            Assert.Contains("RoleCandidates", measurement.PlanSummary);
        });

        var reportPath = await WriteMeasurementReportAsync(database, measurements);
        Assert.True(File.Exists(reportPath), "Messbericht wurde nicht geschrieben.");
    }

    /// <summary>
    /// Legt die dokumentierte Testgroesse an: 1 Root-Node plus 400 Themen-Nodes,
    /// 3 Rollen mit 2 Resolution Orders, 440 Contents (davon 20 Derived) und
    /// 20 Dependencies - ausschliesslich über begin_transaction, Node-, Content-
    /// und Role-Mutation sowie Commit; committed Snapshots bleiben SQL-unberührt.
    /// </summary>
    private static async Task<SnapshotId> SeedRepresentativeDatasetAsync(SqlTestDatabase database)
    {
        var identifierGenerator = new SequentialIdentifierGenerator();
        var transactionId = new TransactionId(Guid.Parse("51200000-0000-0000-0000-000000000001"));
        await using var session = await WorkingTransactionSession.BeginAsync(
            database, transactionId, identifierGenerator, "M5.12 Search-Abnahme");

        await session.CreateRoleAsync("Entwickler", "Technische Sicht");
        await session.CreateRoleAsync("Endanwender", "Anwendersicht");
        await session.CreateRoleAsync("Berater", "Beratersicht");
        await session.SetResolutionAsync(RoleEntwickler, RoleEntwickler, new RoleId("Endanwender"));
        await session.SetResolutionAsync(new RoleId("Berater"), new RoleId("Berater"), RoleEntwickler);

        var rootNode = await session.CreateNodeAsync(null, "Wurzelthema Abnahme", null, 0);
        for (var index = 1; index <= ThemaNodeCount; index++)
        {
            var node = await session.CreateNodeAsync(
                rootNode.NodeId, BuildTitle(index), BuildDescription(index), index);
            await SeedNodeContentsAsync(session, node.NodeId, index);
        }

        var committed = await session.CommitAsync(database, "Commit der M5.12 Search-Testgroesse");
        return committed.WorkingSnapshotId;
    }

    private static async Task SeedNodeContentsAsync(WorkingTransactionSession session, NodeId nodeId, int index)
    {
        var contentMd = $"Inhalt fuer Thema {index:000} mit fachlichen Hinweisen."
            + (index % ContentHitStride == 0 ? " Ablauf im Inhalt." : string.Empty);
        var content = await session.ReplaceIndependentContentAsync(nodeId, RoleEntwickler, contentMd);

        if (index % FallbackContentStride == 0)
            await session.ReplaceIndependentContentAsync(
                nodeId, new RoleId("Endanwender"), $"Endanwender-Sicht zu Thema {index:000}.");

        if (index % FallbackContentStride == FallbackContentStride / 2)
        {
            await session.ReplaceDerivedContentAsync(
                nodeId,
                new RoleId("Berater"),
                $"Berater-Sicht zu Thema {index:000}.",
                [new ContentDependencySource(nodeId, RoleEntwickler, content.ContentRevisionId)]);
        }
    }

    private static string BuildTitle(int index) =>
        $"Thema {index:000} Grundlagen"
        + (index % TitleHitStride == 0 ? " Installation" : string.Empty);

    private static string BuildDescription(int index) =>
        $"Beschreibung zu Thema {index:000}"
        + (index % DescriptionHitStride == 0 ? " Ablauf Beschreibung" : string.Empty);

    private static int RankOf(string hitField) => hitField switch
    {
        "Title" => 1,
        "Description" => 2,
        _ => 3
    };

    private static async Task<List<SearchHit>> PageThroughAllHitsAsync(
        SqlRetrievalRepository repository,
        SnapshotId snapshotId,
        string text,
        RoleId? roleId,
        int pageSize)
    {
        var allHits = new List<SearchHit>();
        string? cursor = null;
        do
        {
            var page = await repository.SearchAsync(
                new SearchRequest(snapshotId, text, roleId, pageSize, cursor, 100)).ConfigureAwait(false);
            if (page.Count == 0)
                break;

            allHits.AddRange(page);
            var last = page[^1];
            cursor = new SearchCursor(
                snapshotId, null, text, roleId, RankOf(last.HitField), last.SortOrder, last.NodeId).Encode();
        }
        while (cursor is not null);

        return allHits;
    }

    private static async Task<List<SearchQueryMeasurement>> MeasureSearchVariantsAsync(
        SqlTestDatabase database,
        SnapshotId snapshotId,
        SqlRetrievalRepository repository)
    {
        var ersteSeite = await repository.SearchAsync(
            new SearchRequest(snapshotId, "Ablauf", RoleEntwickler, 25, null, 100)).ConfigureAwait(false);
        var letzterTreffer = ersteSeite[^1];

        return new List<SearchQueryMeasurement>
        {
            await MeasureSingleQueryAsync(database, snapshotId, new SearchVariant(
                "OhneRolle", "Installation", null,
                HasCursor: 0, LastRank: 0, LastSortOrder: 0, LastNodeId: Guid.Empty)).ConfigureAwait(false),
            await MeasureSingleQueryAsync(database, snapshotId, new SearchVariant(
                "MitRolleErsteSeite", "Ablauf", RoleEntwickler,
                HasCursor: 0, LastRank: 0, LastSortOrder: 0, LastNodeId: Guid.Empty)).ConfigureAwait(false),
            await MeasureSingleQueryAsync(database, snapshotId, new SearchVariant(
                "MitRolleFolgeseite", "Ablauf", RoleEntwickler,
                HasCursor: 1, LastRank: RankOf(letzterTreffer.HitField), LastSortOrder: letzterTreffer.SortOrder,
                LastNodeId: letzterTreffer.NodeId.Value)).ConfigureAwait(false)
        };
    }

    private sealed record SearchVariant(
        string Name,
        string Text,
        RoleId? RoleId,
        int HasCursor,
        int LastRank,
        int LastSortOrder,
        Guid LastNodeId);

    private static async Task<SearchQueryMeasurement> MeasureSingleQueryAsync(
        SqlTestDatabase database,
        SnapshotId snapshotId,
        SearchVariant variant)
    {
        var statistics = new List<string>();
        await using var connection = (SqlConnection)await database.ConnectionFactory.OpenAsync().ConfigureAwait(false);
        connection.InfoMessage += (_, eventArgs) =>
            statistics.AddRange(eventArgs.Errors.Cast<SqlError>().Select(error => error.Message));

        var statisticsSql = $"""
            -- {MeasurementMarker}: {variant.Name}
            SET STATISTICS IO ON;
            SET STATISTICS TIME ON;
            SET STATISTICS PROFILE ON;
            {SqlRetrievalRepository.SearchSql}
            """;
        await using var command = connection.CreateCommand();
        command.CommandText = statisticsSql;
        command.Parameters.AddWithValue("@snapshotId", snapshotId.Value);
        command.Parameters.AddWithValue("@roleId", (object?)variant.RoleId?.Value ?? DBNull.Value);
        command.Parameters.AddWithValue("@likePattern", $"%{variant.Text}%");
        command.Parameters.AddWithValue("@limit", 50);
        command.Parameters.AddWithValue("@hasCursor", variant.HasCursor);
        command.Parameters.AddWithValue("@lastRank", variant.LastRank);
        command.Parameters.AddWithValue("@lastSortOrder", variant.LastSortOrder);
        command.Parameters.AddWithValue("@lastNodeId", variant.LastNodeId);

        var stopwatch = Stopwatch.StartNew();
        var result = await ExecuteWithProfileAsync(command).ConfigureAwait(false);
        stopwatch.Stop();

        var messageText = string.Join("\n", statistics);
        var logicalReads = ParseLogicalReads(messageText);
        var (cpuMs, elapsedMs) = ParseExecutionTimes(messageText);
        var planSummary = BuildPlanSummary(result.PlanRows);

        return new SearchQueryMeasurement(
            variant.Name,
            result.Rows.Count,
            logicalReads.Total,
            logicalReads.ByTable,
            cpuMs,
            elapsedMs,
            stopwatch.Elapsed.TotalMilliseconds,
            planSummary,
            messageText.Length > 4000 ? messageText[..4000] : messageText);
    }

    private static async Task<(List<SearchPlanRow> Rows, List<QueryPlanRow> PlanRows)> ExecuteWithProfileAsync(
        SqlCommand command)
    {
        var rows = new List<SearchPlanRow>();
        var planRows = new List<QueryPlanRow>();
        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
            rows.Add(new SearchPlanRow
            {
                NodeId = reader.GetGuid(reader.GetOrdinal("NodeId")),
                SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder"))
            });

        if (await reader.NextResultAsync().ConfigureAwait(false))
            while (await reader.ReadAsync().ConfigureAwait(false))
                planRows.Add(ReadPlanRow(reader));

        return (rows, planRows);
    }

    private static QueryPlanRow ReadPlanRow(SqlDataReader reader) => new()
    {
        NodeId = reader.IsDBNull(reader.GetOrdinal("NodeId")) ? 0 : Convert.ToInt64(reader.GetValue(reader.GetOrdinal("NodeId")), CultureInfo.InvariantCulture),
        PhysicalOp = reader.IsDBNull(reader.GetOrdinal("PhysicalOp")) ? null : reader.GetString(reader.GetOrdinal("PhysicalOp")),
        LogicalOp = reader.IsDBNull(reader.GetOrdinal("LogicalOp")) ? null : reader.GetString(reader.GetOrdinal("LogicalOp")),
        Argument = reader.IsDBNull(reader.GetOrdinal("Argument")) ? null : reader.GetString(reader.GetOrdinal("Argument")),
        EstimateRows = reader.IsDBNull(reader.GetOrdinal("EstimateRows")) ? 0 : Convert.ToDouble(reader.GetValue(reader.GetOrdinal("EstimateRows")), CultureInfo.InvariantCulture),
        Type = reader.IsDBNull(reader.GetOrdinal("Type")) ? null : reader.GetString(reader.GetOrdinal("Type")),
        StmtText = reader.IsDBNull(reader.GetOrdinal("StmtText")) ? null : reader.GetString(reader.GetOrdinal("StmtText"))
    };

    private static string BuildPlanSummary(List<QueryPlanRow> planRows)
    {
        var operatorLines = planRows
            .Where(row => row.PhysicalOp is not null)
            .Select(row => $"Node {row.NodeId}: {row.PhysicalOp} ({row.LogicalOp}), EstimateRows={row.EstimateRows:0.##}, Argument={row.Argument}");
        var statementLines = planRows
            .Where(row => row.PhysicalOp is null && row.StmtText is not null)
            .Select(row => $"Statement ({row.Type}): {row.StmtText}");
        return string.Join("\n", statementLines.Concat(operatorLines));
    }

    private static (int Total, Dictionary<string, int> ByTable) ParseLogicalReads(string messageText)
    {
        var byTable = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match match in LogicalReadsRegex().Matches(messageText))
        {
            var tableName = match.Groups[1].Value;
            var reads = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            byTable[tableName] = byTable.GetValueOrDefault(tableName) + reads;
        }

        return (byTable.Values.Sum(), byTable);
    }

    private static (long CpuMs, long ElapsedMs) ParseExecutionTimes(string messageText)
    {
        Match? lastMatch = null;
        foreach (Match match in ExecutionTimesRegex().Matches(messageText))
            lastMatch = match;

        return lastMatch is null
            ? (0, 0)
            : (long.Parse(lastMatch.Groups[1].Value, CultureInfo.InvariantCulture),
                long.Parse(lastMatch.Groups[2].Value, CultureInfo.InvariantCulture));
    }

    private static async Task<string> WriteMeasurementReportAsync(
        SqlTestDatabase database,
        IReadOnlyList<SearchQueryMeasurement> measurements)
    {
        var report = new SearchAbnahmeReport(
            database.DatabaseName,
            await database.GetSqlServerMajorVersionAsync().ConfigureAwait(false),
            ThemaNodeCount,
            measurements,
            null);

        var reportPath = Path.Combine(FindRepositoryRoot(), "temp", "search-abnahme-messung.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        return reportPath;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "KnowHowToAI.slnx")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new InvalidOperationException("Repository-Root mit KnowHowToAI.slnx wurde nicht gefunden.");
    }

    [GeneratedRegex(@"(?:Table|Tabelle): ""([^""]+)""\. (?:Scan count|Anzahl von [ÜU]berpr[üu]fungen): \d+, (?:logical reads|logische Lesevorg[aä]nge): (\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex LogicalReadsRegex();

    [GeneratedRegex(@"(?:SQL Server Execution Times|SQL Server-Ausf[üu]hrungszeiten):\s*CPU[- ](?:time|Zeit) = (\d+) ms,\s*(?:elapsed time|verstrichene Zeit) = (\d+) ms", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex ExecutionTimesRegex();

    private sealed class SearchPlanRow
    {
        public Guid NodeId { get; init; }
        public int SortOrder { get; init; }
    }

    private sealed record SearchQueryMeasurement(
        string Name,
        int RowCount,
        int LogicalReadsTotal,
        Dictionary<string, int> LogicalReadsByTable,
        long CpuMilliseconds,
        long ElapsedMilliseconds,
        double WallClockMilliseconds,
        string PlanSummary,
        string RawStatistics);

    private sealed class QueryPlanRow
    {
        public long NodeId { get; init; }
        public string? PhysicalOp { get; init; }
        public string? LogicalOp { get; init; }
        public string? Argument { get; init; }
        public double EstimateRows { get; init; }
        public string? Type { get; init; }
        public string? StmtText { get; init; }
    }

    private sealed record SearchAbnahmeReport(
        string DatabaseName,
        int SqlServerMajorVersion,
        int ThemaNodeCount,
        IReadOnlyList<SearchQueryMeasurement> Measurements,
        string? Notes);
}
