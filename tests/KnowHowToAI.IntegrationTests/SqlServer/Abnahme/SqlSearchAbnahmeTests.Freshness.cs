using System.Diagnostics;
using System.Text.Json;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Mapping;
using KnowHowToAI.Storage.SqlServer.Repositories.Retrieval;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Abnahme;

/// <summary>
/// M5.15-Abnahme: misst die kompletten Kosten von SqlRetrievalRepository.SearchAsync
/// bei Derived-Content-Treffern einschließlich des Ladens aller Contents und
/// Dependencies für die transitive Freshness-Bewertung. Die Messwerte werden nach
/// temp/search-abnahme-freshness-messung.json geschrieben.
/// </summary>
public sealed partial class SqlSearchAbnahmeTests
{
    private const string FreshnessMessungMarker = "M5.15-Abnahme";
    private const string DerivedSuchtext = "Berater-Sicht";

    [Fact]
    public async Task SearchMitDerivedTreffern_KompletteFreshnessLadeKosten_NachweisInReadsUndLaufzeit()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();
        var snapshotId = await SeedRepresentativeDatasetAsync(database);
        var repository = new SqlRetrievalRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 60 });

        var erwarteteDerivedTreffer = ThemaNodeCount / FallbackContentStride;
        var repositoryStopwatch = Stopwatch.StartNew();
        var ergebnis = (await repository.SearchAsync(
            new SearchRequest(snapshotId, DerivedSuchtext, new RoleId("Berater"), 25, null, 100))
            .ConfigureAwait(false)).Value!;
        repositoryStopwatch.Stop();

        Assert.Equal(erwarteteDerivedTreffer, ergebnis.Count);
        Assert.All(ergebnis, hit =>
        {
            Assert.Equal("Content", hit.HitField);
            Assert.Equal(Availability.Explicit, hit.Availability);
            Assert.Equal(Freshness.Current, hit.Freshness);
        });

        var measurement = await MeasureCompleteDerivedSearchAsync(database, snapshotId).ConfigureAwait(false);
        measurement = measurement with
        {
            RepositoryWallClockMilliseconds = repositoryStopwatch.Elapsed.TotalMilliseconds
        };

        Assert.True(measurement.LogicalReadsTotal > 0, "Keine Reads erfasst.");
        Assert.True(measurement.LogicalReadsTotal < 100_000, $"Unerwartet hohe Reads: {measurement.LogicalReadsTotal}");
        Assert.True(measurement.WallClockMilliseconds < 2000, $"Unerwartet langsam: {measurement.WallClockMilliseconds}");
        Assert.Equal(erwarteteDerivedTreffer, measurement.DerivedHitCount);
        Assert.Equal(ErwarteteGesamtContents(), measurement.LoadedContentCount);
        Assert.Equal(erwarteteDerivedTreffer, measurement.LoadedDependencyCount);
        Assert.Equal(ErwarteteFreshnessTeilmenge(), measurement.FreshnessSubsetRowCount);
        Assert.True(
            measurement.FreshnessSubsetRowCount < measurement.LoadedContentCount + measurement.LoadedDependencyCount,
            "Freshness-Teilmenge ist nicht kleiner als die vollstaendige Ladung.");

        var reportPath = await WriteFreshnessMeasurementReportAsync(database, measurement).ConfigureAwait(false);
        Assert.True(File.Exists(reportPath), "Freshness-Messbericht wurde nicht geschrieben.");
    }

    /// <summary>
    /// Anzahl Contents im kommittierten Abnahme-Snapshot: 400 Entwickler (Independent),
    /// 20 Endanwender (Independent), 20 Berater (Derived) = 440.
    /// </summary>
    private static int ErwarteteGesamtContents() =>
        ThemaNodeCount + 2 * (ThemaNodeCount / FallbackContentStride);

    /// <summary>
    /// Fuer die transitive Freshness der Trefferseite noetige Zeilen: alle
    /// Dependencies der Derived-Treffer plus ihre unmittelbaren Source-Contents.
    /// </summary>
    private static int ErwarteteFreshnessTeilmenge() => 2 * (ThemaNodeCount / FallbackContentStride);

    private static async Task<FreshnessSearchMeasurement> MeasureCompleteDerivedSearchAsync(
        SqlTestDatabase database,
        SnapshotId snapshotId)
    {
        var statistics = new List<string>();
        await using var connection = (SqlConnection)await database.ConnectionFactory.OpenAsync().ConfigureAwait(false);
        connection.InfoMessage += (_, eventArgs) =>
            statistics.AddRange(eventArgs.Errors.Cast<SqlError>().Select(error => error.Message));

        var statisticsSql = $"""
            -- {FreshnessMessungMarker}: kompletter SearchAsync-Pfad mit Freshness-Ladung
            SET STATISTICS IO ON;
            SET STATISTICS TIME ON;
            SET STATISTICS PROFILE ON;
            {SqlRetrievalRepository.ListRolesSql}
            {SqlRetrievalRepository.ListRoleResolutionsSql}
            {SqlRetrievalRepository.SearchSql}
            {SqlRetrievalRepository.ListContentsSql}
            {SqlRetrievalRepository.ListDependenciesSql}
            """;
        await using var command = connection.CreateCommand();
        command.CommandText = statisticsSql;
        command.Parameters.AddWithValue("@snapshotId", snapshotId.Value);
        command.Parameters.AddWithValue("@roleId", "Berater");
        command.Parameters.AddWithValue("@likePattern", $"%{DerivedSuchtext}%");
        command.Parameters.AddWithValue("@limit", 25);
        command.Parameters.AddWithValue("@hasCursor", 0);
        command.Parameters.AddWithValue("@lastRank", 0);
        command.Parameters.AddWithValue("@lastSortOrder", 0);
        command.Parameters.AddWithValue("@lastNodeId", Guid.Empty);

        var stopwatch = Stopwatch.StartNew();
        var executed = await ExecuteCompletePathAsync(command).ConfigureAwait(false);
        stopwatch.Stop();

        var messageText = string.Join("\n", statistics);
        var logicalReads = ParseLogicalReads(messageText);
        var (cpuMs, elapsedMs) = ParseExecutionTimes(messageText);

        return new FreshnessSearchMeasurement(
            "MitDerivedTrefferBeraterKomplett",
            executed.SearchRows.Count,
            executed.SearchRows.Count(row => row.ContentMode == SqlPersistedValues.ContentDerived),
            executed.Contents.Count,
            executed.Dependencies.Count,
            CountFreshnessSubsetRows(executed),
            logicalReads.Total,
            logicalReads.ByTable,
            cpuMs,
            elapsedMs,
            stopwatch.Elapsed.TotalMilliseconds,
            0,
            BuildPlanSummary(executed.PlanRows),
            messageText.Length > 4000 ? messageText[..4000] : messageText);
    }

    private static async Task<CompletePathRows> ExecuteCompletePathAsync(SqlCommand command)
    {
        var rows = new CompletePathRows();
        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        do
        {
            var columns = reader.GetColumnSchema().Select(column => column.ColumnName).ToHashSet();
            if (columns.Contains("PhysicalOp"))
                rows.PlanRows.AddRange(ReadPlanResultSet(reader));
            else if (columns.Contains("HitField"))
                await ReadSearchRowsAsync(reader, rows).ConfigureAwait(false);
            else if (columns.Contains("ContentMd"))
                ReadContentRows(reader, rows);
            else if (columns.Contains("SourceContentRevisionId"))
                ReadDependencyRows(reader, rows);
            else if (columns.Contains("RequestedRoleId"))
                rows.Resolutions = ReadCountedRows(reader);
            else if (columns.Contains("RoleId"))
                rows.Roles = ReadCountedRows(reader);
        }
        while (await reader.NextResultAsync().ConfigureAwait(false));
        return rows;
    }

    private static async Task ReadSearchRowsAsync(SqlDataReader reader, CompletePathRows rows)
    {
        while (await reader.ReadAsync().ConfigureAwait(false))
            rows.SearchRows.Add(new DerivedSearchRow(
                reader.GetGuid(reader.GetOrdinal("NodeId")),
                reader.IsDBNull(reader.GetOrdinal("ResolvedRoleId"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ResolvedRoleId")),
                reader.IsDBNull(reader.GetOrdinal("ContentRevisionId"))
                    ? null
                    : reader.GetGuid(reader.GetOrdinal("ContentRevisionId")),
                reader.IsDBNull(reader.GetOrdinal("ContentMode"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ContentMode"))));
    }

    private static void ReadContentRows(SqlDataReader reader, CompletePathRows rows)
    {
        while (reader.Read())
            rows.Contents.Add(new LoadedContentRow(
                reader.GetGuid(reader.GetOrdinal("NodeId")),
                reader.GetString(reader.GetOrdinal("RoleId")),
                reader.GetGuid(reader.GetOrdinal("ContentRevisionId")),
                reader.GetString(reader.GetOrdinal("ContentMode")),
                reader.GetBoolean(reader.GetOrdinal("IsDeleted"))));
    }

    private static void ReadDependencyRows(SqlDataReader reader, CompletePathRows rows)
    {
        while (reader.Read())
            rows.Dependencies.Add(new LoadedDependencyRow(
                reader.GetGuid(reader.GetOrdinal("TargetNodeId")),
                reader.GetString(reader.GetOrdinal("TargetRoleId")),
                reader.GetGuid(reader.GetOrdinal("SourceNodeId")),
                reader.GetString(reader.GetOrdinal("SourceRoleId"))));
    }

    private static int ReadCountedRows(SqlDataReader reader)
    {
        var count = 0;
        while (reader.Read())
            count++;
        return count;
    }

    private static IEnumerable<QueryPlanRow> ReadPlanResultSet(SqlDataReader reader)
    {
        while (reader.Read())
            yield return ReadPlanRow(reader);
    }

    /// <summary>
    /// Zeilen der Freshness-Teilmenge: Dependencies der Derived-Treffer plus ihre
    /// Source-Contents. Source-Contents sind hier Independent, eine Ebene genuegt;
    /// eine echte Teilmenge muesste transitiv abschliessen.
    /// </summary>
    private static int CountFreshnessSubsetRows(CompletePathRows rows)
    {
        var derivedHits = rows.SearchRows
            .Where(row => row.ContentMode == SqlPersistedValues.ContentDerived)
            .ToList();
        var neededDependencies = rows.Dependencies
            .Where(dependency => derivedHits.Any(hit =>
                hit.NodeId == dependency.TargetNodeId
                && hit.ResolvedRoleId == dependency.TargetRoleId))
            .ToList();
        var neededSourceContents = rows.Contents
            .Where(content => neededDependencies.Any(dependency =>
                dependency.SourceNodeId == content.NodeId
                && dependency.SourceRoleId == content.RoleId))
            .ToList();
        return neededDependencies.Count + neededSourceContents.Count;
    }

    private static async Task<string> WriteFreshnessMeasurementReportAsync(
        SqlTestDatabase database,
        FreshnessSearchMeasurement measurement)
    {
        var report = new FreshnessSearchAbnahmeReport(
            database.DatabaseName,
            await database.GetSqlServerMajorVersionAsync().ConfigureAwait(false),
            ThemaNodeCount,
            measurement,
            "Kompletter SearchAsync-Pfad bei Derived-Treffern: Rollen- und Resolution-Laden, "
            + "SearchSql, volles Laden aller Contents und Dependencies (Freshness-Bewertung).");

        var reportPath = Path.Combine(FindRepositoryRoot(), "temp", "search-abnahme-freshness-messung.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        return reportPath;
    }

    private sealed record DerivedSearchRow(
        Guid NodeId,
        string? ResolvedRoleId,
        Guid? ContentRevisionId,
        string? ContentMode);

    private sealed record LoadedContentRow(
        Guid NodeId,
        string RoleId,
        Guid ContentRevisionId,
        string ContentMode,
        bool IsDeleted);

    private sealed record LoadedDependencyRow(
        Guid TargetNodeId,
        string TargetRoleId,
        Guid SourceNodeId,
        string SourceRoleId);

    private sealed class CompletePathRows
    {
        public List<DerivedSearchRow> SearchRows { get; } = [];
        public List<LoadedContentRow> Contents { get; } = [];
        public List<LoadedDependencyRow> Dependencies { get; } = [];
        public List<QueryPlanRow> PlanRows { get; } = [];
        public int Roles { get; set; }
        public int Resolutions { get; set; }
    }

    private sealed record FreshnessSearchMeasurement(
        string Name,
        int SearchRowCount,
        int DerivedHitCount,
        int LoadedContentCount,
        int LoadedDependencyCount,
        int FreshnessSubsetRowCount,
        int LogicalReadsTotal,
        Dictionary<string, int> LogicalReadsByTable,
        long CpuMilliseconds,
        long ElapsedMilliseconds,
        double WallClockMilliseconds,
        double RepositoryWallClockMilliseconds,
        string PlanSummary,
        string RawStatistics);

    private sealed record FreshnessSearchAbnahmeReport(
        string DatabaseName,
        int SqlServerMajorVersion,
        int ThemaNodeCount,
        FreshnessSearchMeasurement Measurement,
        string? Notes);
}
