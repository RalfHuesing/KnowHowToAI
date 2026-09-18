using System.Diagnostics;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.TestSupport;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.IntegrationTests.SqlServer.Abnahme;

/// <summary>
/// M5.16-Abnahme: blaettern einen Diff über die volle M5.12-Datenmenge vollständig
/// seitenweise durch und messen pro Cursor-Seite gelesene Zeilen, logische Reads und
/// Laufzeit. Belegt, dass <see cref="HistoryService"/> je Seite beide Snapshots
/// vollständig lädt und den Diff neu berechnet. Messwerte nach
/// temp/diff-abnahme-messung.json.
/// </summary>
public sealed partial class SqlDiffReleaseAbnahmeTests
{
    private const int DiffPageSize = 50;
    private const int PagingThemaNodeCount = 400;
    private const int PagingContentStride = 20;
    private const int PagingModifiedNodeCount = 150;
    private const int PagingModifiedContentCount = 100;
    private const int PagingAddedNodeCount = 40;
    private const int PagingDeletedNodeCount = 20;
    private const int PagingDeletedFirstIndex = 381;
    private const string PagingMeasurementMarker = "M5.16-Abnahme";

    private static readonly TransactionId PagingBasisTransactionId =
        new(Guid.Parse("51216000-0000-0000-0000-000000000001"));
    private static readonly TransactionId PagingAenderungsTransactionId =
        new(Guid.Parse("51216000-0000-0000-0000-000000000002"));

    [Fact]
    public async Task DiffPaging_UeberVolleM512Datenmenge_KostenProSeiteNachgewiesen()
    {
        await using var database = await SqlTestDatabase.ConnectFreshAsync();
        await SqlTestDatabase.CreateMigrator(database).MigrateAsync();

        var identifierGenerator = new SequentialIdentifierGenerator();
        var (baseSnapshotId, themaNodes) = await SeedBaseDiffDatasetAsync(database, identifierGenerator);
        var targetSnapshotId = await CommitDiffChangeSnapshotAsync(database, identifierGenerator, themaNodes);

        var historyService = CreateHistoryService(database);
        var erwarteteZeilen = await LadeSnapshotZeilenAsync(database, baseSnapshotId, targetSnapshotId);

        var ersteMessungen = await DurchblaetternUndMessenAsync(
            historyService, baseSnapshotId, targetSnapshotId, database);
        var zweiteMessungen = await DurchblaetternUndMessenAsync(
            historyService, baseSnapshotId, targetSnapshotId, database);

        AssertDiffSeitenMessungen(ersteMessungen, erwarteteZeilen);
        Assert.Equal(
            ersteMessungen
                .Select(seite => (seite.Seitennummer, seite.ItemCount, seite.TotalCount))
                .ToList(),
            zweiteMessungen
                .Select(seite => (seite.Seitennummer, seite.ItemCount, seite.TotalCount))
                .ToList());

        var istPlan = await ErfasseDiffIstPlanAsync(database, baseSnapshotId, targetSnapshotId);
        Assert.False(string.IsNullOrWhiteSpace(istPlan), "Kein Ist-Plan erfasst.");
        var reportPath = await SchreibeDiffMessberichtAsync(
            database, baseSnapshotId, targetSnapshotId, ersteMessungen, istPlan);
        Assert.True(File.Exists(reportPath), "Diff-Messbericht wurde nicht geschrieben.");
    }

    /// <summary>
    /// Basissnapshot in M5.12-Groesse: 1 Root plus 400 Themen-Nodes, 3 Rollen mit
    /// 2 Resolution Orders, 440 Contents (400 Entwickler, 20 Endanwender, 20 Berater
    /// Derived) und 20 Dependencies - ausschliesslich über Mutation-Pfade. Der
    /// IdentifierGenerator bleibt über beide Transaktionen geteilt, damit NodeIds
    /// fortlaufend und global einmalig bleiben.
    /// </summary>
    private static async Task<(SnapshotId SnapshotId, List<NodeId> ThemaNodes)> SeedBaseDiffDatasetAsync(
        SqlTestDatabase database, SequentialIdentifierGenerator identifierGenerator)
    {
        await using var session = await WorkingTransactionSession.BeginAsync(
            database, PagingBasisTransactionId, identifierGenerator, "M5.16 Diff-Basissnapshot");

        await session.CreateRoleAsync("Entwickler", null);
        await session.CreateRoleAsync("Endanwender", null);
        await session.CreateRoleAsync("Berater", null);
        await session.SetResolutionAsync(RoleEntwickler, RoleEntwickler, new RoleId("Endanwender"));
        await session.SetResolutionAsync(new RoleId("Berater"), new RoleId("Berater"), RoleEntwickler);

        var wurzel = await session.CreateNodeAsync(null, "Wurzel Diff-Paging", null, 0);
        var themaNodes = new List<NodeId>();
        for (var index = 1; index <= PagingThemaNodeCount; index++)
            themaNodes.Add((await SeedPagingThemaNodeAsync(session, wurzel.NodeId, index)).NodeId);

        var committed = await session.CommitAsync(database, "Commit M5.16 Diff-Basissnapshot");
        return (committed.WorkingSnapshotId, themaNodes);
    }

    private static async Task<Node> SeedPagingZusatzNodeAsync(
        WorkingTransactionSession session, NodeId parentNodeId, int index)
    {
        var node = await session.CreateNodeAsync(
            parentNodeId, $"Zusatzthema {index:000}", null, PagingThemaNodeCount + index);
        await session.ReplaceIndependentContentAsync(
            node.NodeId, RoleEntwickler, $"Entwickler-Inhalt zu Zusatzthema {index:000}.");
        return node;
    }

    private static async Task<Node> SeedPagingThemaNodeAsync(
        WorkingTransactionSession session, NodeId wurzelNodeId, int index)
    {
        var node = await session.CreateNodeAsync(
            wurzelNodeId, $"Thema {index:000}", $"Beschreibung zu Thema {index:000}", index);
        var content = await session.ReplaceIndependentContentAsync(
            node.NodeId, RoleEntwickler, $"Entwickler-Inhalt zu Thema {index:000}.");

        if (index % PagingContentStride == 0)
            await session.ReplaceIndependentContentAsync(
                node.NodeId, new RoleId("Endanwender"), $"Endanwender-Inhalt zu Thema {index:000}.");

        if (index % PagingContentStride == PagingContentStride / 2)
            await session.ReplaceDerivedContentAsync(
                node.NodeId, new RoleId("Berater"), $"Berater-Inhalt zu Thema {index:000}.",
                [new ContentDependencySource(node.NodeId, RoleEntwickler, content.ContentRevisionId)]);

        return node;
    }

    /// <summary>
    /// Aenderungs-Snapshot mit mehreren hundert Diff-Eintraegen: 150 Modified Nodes,
    /// 100 Modified Contents, 40 Added Nodes samt Content, 20 Deleted Subtrees
    /// (22 Deleted Contents, 1 Deleted Dependency).
    /// </summary>
    private static async Task<SnapshotId> CommitDiffChangeSnapshotAsync(
        SqlTestDatabase database, SequentialIdentifierGenerator identifierGenerator, List<NodeId> themaNodes)
    {
        await using var session = await WorkingTransactionSession.BeginAsync(
            database, PagingAenderungsTransactionId, identifierGenerator, "M5.16 Diff-Aenderungen");

        for (var index = 0; index < PagingModifiedNodeCount; index++)
            await session.UpdateNodeAsync(themaNodes[index], $"Thema {index + 1:000} aktualisiert", null);
        for (var index = 0; index < PagingModifiedContentCount; index++)
            await session.ReplaceIndependentContentAsync(
                themaNodes[index], RoleEntwickler, $"Entwickler-Inhalt zu Thema {index + 1:000} (geaendert).");
        for (var index = 1; index <= PagingAddedNodeCount; index++)
            await SeedPagingZusatzNodeAsync(session, themaNodes[0], index);
        for (var index = 0; index < PagingDeletedNodeCount; index++)
            await session.DeleteNodeSubtreeAsync(
                themaNodes[PagingDeletedFirstIndex - 1 + index]);

        var committed = await session.CommitAsync(database, "Commit M5.16 Diff-Aenderungen");
        return committed.WorkingSnapshotId;
    }

    private static int ErwarteteDiffEintraege() =>
        PagingModifiedNodeCount + PagingAddedNodeCount + PagingDeletedNodeCount
        + PagingModifiedContentCount + PagingAddedNodeCount
        + PagingDeletedNodeCount + GeloeschteZusatzContents() + GeloeschteDependencies();

    private static int GeloeschteZusatzContents() =>
        Enumerable.Range(PagingDeletedFirstIndex, PagingDeletedNodeCount)
            .Count(index => IstEndanwenderIndex(index) || IstBeraterIndex(index));

    private static int GeloeschteDependencies() =>
        Enumerable.Range(PagingDeletedFirstIndex, PagingDeletedNodeCount)
            .Count(IstBeraterIndex);

    private static int ErwarteteSeitenzahl() =>
        (ErwarteteDiffEintraege() + DiffPageSize - 1) / DiffPageSize;

    private static bool IstEndanwenderIndex(int index) => index % PagingContentStride == 0;

    private static bool IstBeraterIndex(int index) =>
        index % PagingContentStride == PagingContentStride / 2;

    private static async Task<SnapshotZeilen> LadeSnapshotZeilenAsync(
        SqlTestDatabase database, SnapshotId baseSnapshotId, SnapshotId targetSnapshotId)
    {
        var policy = new SqlStoragePolicy { CommandTimeoutSeconds = 60 };
        var hierarchy = new SqlHierarchyRepository(database.ConnectionFactory, policy);
        var roles = new SqlRoleRepository(database.ConnectionFactory, policy);
        var contents = new SqlContentRepository(database.ConnectionFactory, policy);
        var dependencies = new SqlDependencyRepository(database.ConnectionFactory, policy);

        var basis = await ZaehleSnapshotZeilenAsync(
            hierarchy, roles, contents, dependencies, baseSnapshotId);
        var ziel = await ZaehleSnapshotZeilenAsync(
            hierarchy, roles, contents, dependencies, targetSnapshotId);

        Assert.Equal(PagingThemaNodeCount + 1, basis.Nodes);
        Assert.Equal(PagingThemaNodeCount + 2 * (PagingThemaNodeCount / PagingContentStride), basis.Contents);
        Assert.Equal(PagingThemaNodeCount / PagingContentStride, basis.Dependencies);
        Assert.Equal(basis.Nodes + PagingAddedNodeCount, ziel.Nodes);
        Assert.Equal(basis.Contents + PagingAddedNodeCount, ziel.Contents);
        Assert.Equal(basis.Dependencies - GeloeschteDependencies(), ziel.Dependencies);
        return new SnapshotZeilen(basis, ziel);
    }

    private static async Task<SnapshotZeilenStatistik> ZaehleSnapshotZeilenAsync(
        SqlHierarchyRepository hierarchy,
        SqlRoleRepository roles,
        SqlContentRepository contents,
        SqlDependencyRepository dependencies,
        SnapshotId snapshotId)
    {
        return new SnapshotZeilenStatistik(
            (await hierarchy.ListBySnapshotAsync(snapshotId)).Count,
            (await roles.ListBySnapshotAsync(snapshotId)).Count,
            (await roles.ListResolutionsBySnapshotAsync(snapshotId)).Count,
            (await contents.ListBySnapshotAsync(snapshotId)).Count,
            (await dependencies.ListBySnapshotAsync(snapshotId)).Count);
    }

    private static async Task<List<DiffSeitenMessung>> DurchblaetternUndMessenAsync(
        HistoryService historyService,
        SnapshotId baseSnapshotId,
        SnapshotId targetSnapshotId,
        SqlTestDatabase database)
    {
        var messungen = new List<DiffSeitenMessung>();
        string? cursor = null;
        while (messungen.Count <= ErwarteteDiffEintraege())
        {
            var stopwatch = Stopwatch.StartNew();
            var diff = await RequireDiffAsync(historyService.CompareSnapshotsAsync(
                baseSnapshotId, targetSnapshotId, DiffPageSize, cursor));
            stopwatch.Stop();

            var sqlMessung = await MesseSnapshotLadungenAsync(database, baseSnapshotId, targetSnapshotId);
            messungen.Add(new DiffSeitenMessung(
                messungen.Count + 1,
                diff.Nodes.Count + diff.Roles.Count + diff.RoleResolutions.Count
                    + diff.Contents.Count + diff.Dependencies.Count,
                diff.TotalCount,
                diff.NextCursor,
                stopwatch.Elapsed.TotalMilliseconds,
                sqlMessung));

            cursor = diff.NextCursor;
            if (cursor is null)
                break;
        }

        return messungen;
    }

    private static void AssertDiffSeitenMessungen(
        IReadOnlyList<DiffSeitenMessung> messungen, SnapshotZeilen erwarteteZeilen)
    {
        Assert.Equal(ErwarteteDiffEintraege(), messungen[^1].TotalCount);
        Assert.Equal(ErwarteteSeitenzahl(), messungen.Count);
        Assert.Null(messungen[^1].NextCursor);
        Assert.Equal(messungen[^1].TotalCount, messungen.Sum(seite => seite.ItemCount));
        Assert.All(messungen.SkipLast(1), seite => Assert.Equal(DiffPageSize, seite.ItemCount));
        Assert.All(messungen, seite => AssertDiffSeitenLadung(seite, erwarteteZeilen));
    }

    private static void AssertDiffSeitenLadung(DiffSeitenMessung seite, SnapshotZeilen erwarteteZeilen)
    {
        Assert.True(seite.SqlMessung.LogicalReadsTotal > 0, $"Keine Reads auf Seite {seite.Seitennummer}.");
        Assert.True(
            seite.SqlMessung.LogicalReadsTotal < 100_000,
            $"Unerwartet hohe Reads auf Seite {seite.Seitennummer}.");
        Assert.True(
            seite.ServiceWallClockMilliseconds < 2000,
            $"Seite {seite.Seitennummer} unerwartet langsam.");

        Assert.Equal(5, seite.SqlMessung.BasisZeilenProStatement.Length);
        Assert.Equal(5, seite.SqlMessung.ZielZeilenProStatement.Length);
        Assert.Equal(
            erwarteteZeilen.Basis.ZeilenProStatement,
            seite.SqlMessung.BasisZeilenProStatement);
        Assert.Equal(erwarteteZeilen.Ziel.ZeilenProStatement, seite.SqlMessung.ZielZeilenProStatement);
    }

    private static async Task<SqlLadungsMessung> MesseSnapshotLadungenAsync(
        SqlTestDatabase database, SnapshotId baseSnapshotId, SnapshotId targetSnapshotId)
    {
        var statistics = new List<string>();
        await using var connection = (SqlConnection)await database.ConnectionFactory.OpenAsync();
        connection.InfoMessage += (_, eventArgs) =>
            statistics.AddRange(eventArgs.Errors.Cast<SqlError>().Select(error => error.Message));

        var stopwatch = Stopwatch.StartNew();
        var basisZeilen = await MesseLadungsBatchAsync(connection, baseSnapshotId);
        var zielZeilen = await MesseLadungsBatchAsync(connection, targetSnapshotId);
        stopwatch.Stop();

        var messageText = string.Join("\n", statistics);
        var reads = SqlStatisticsMessages.ParseLogicalReads(messageText);
        var (cpuMs, elapsedMs) = SqlStatisticsMessages.ParseExecutionTimes(messageText);
        return new SqlLadungsMessung(
            basisZeilen, zielZeilen, reads.Total, reads.ByTable, cpuMs, elapsedMs,
            stopwatch.Elapsed.TotalMilliseconds);
    }

    private static async Task<int[]> MesseLadungsBatchAsync(SqlConnection connection, SnapshotId snapshotId)
    {
        var statisticsSql = $"""
            -- {PagingMeasurementMarker}: vollstaendige Snapshot-Ladung
            SET STATISTICS IO ON;
            SET STATISTICS TIME ON;
            {SqlHierarchyRepository.ListSql}
            {SqlRoleRepository.ListRolesSql}
            {SqlRoleRepository.ListResolutionsSql}
            {SqlContentRepository.ListSql}
            {SqlDependencyRepository.ListSql}
            """;
        await using var command = connection.CreateCommand();
        command.CommandText = statisticsSql;
        command.Parameters.AddWithValue("@snapshotId", snapshotId.Value);

        var zeilenProStatement = new List<int>();
        await using var reader = await command.ExecuteReaderAsync();
        do
        {
            var geleseneZeilen = 0;
            while (await reader.ReadAsync())
                geleseneZeilen++;
            zeilenProStatement.Add(geleseneZeilen);
        }
        while (await reader.NextResultAsync());

        return [.. zeilenProStatement];
    }

    private static async Task<string> ErfasseDiffIstPlanAsync(
        SqlTestDatabase database, SnapshotId baseSnapshotId, SnapshotId targetSnapshotId)
    {
        await using var connection = (SqlConnection)await database.ConnectionFactory.OpenAsync();
        var planZeilen = new List<string>();
        await LesePlanZeilenAsync(connection, baseSnapshotId, planZeilen);
        await LesePlanZeilenAsync(connection, targetSnapshotId, planZeilen);
        return string.Join("\n", planZeilen);
    }

    private static async Task LesePlanZeilenAsync(
        SqlConnection connection, SnapshotId snapshotId, List<string> planZeilen)
    {
        var statisticsSql = $"""
            -- {PagingMeasurementMarker}: Ist-Plan der Snapshot-Ladung
            SET STATISTICS PROFILE ON;
            {SqlHierarchyRepository.ListSql}
            {SqlRoleRepository.ListRolesSql}
            {SqlRoleRepository.ListResolutionsSql}
            {SqlContentRepository.ListSql}
            {SqlDependencyRepository.ListSql}
            """;
        await using var command = connection.CreateCommand();
        command.CommandText = statisticsSql;
        command.Parameters.AddWithValue("@snapshotId", snapshotId.Value);

        await using var reader = await command.ExecuteReaderAsync();
        do
        {
            var columns = reader.GetColumnSchema().Select(column => column.ColumnName).ToHashSet();
            if (!columns.Contains("PhysicalOp"))
                continue;

            while (await reader.ReadAsync())
            {
                if (reader.IsDBNull(reader.GetOrdinal("PhysicalOp")))
                    continue;

                var argument = reader.IsDBNull(reader.GetOrdinal("Argument"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("Argument"));
                planZeilen.Add(
                    $"{reader.GetString(reader.GetOrdinal("PhysicalOp"))} "
                    + $"({reader.GetString(reader.GetOrdinal("LogicalOp"))}) {argument}");
            }
        }
        while (await reader.NextResultAsync());
    }

    private static async Task<string> SchreibeDiffMessberichtAsync(
        SqlTestDatabase database,
        SnapshotId baseSnapshotId,
        SnapshotId targetSnapshotId,
        IReadOnlyList<DiffSeitenMessung> messungen,
        string istPlan)
    {
        var report = new DiffPagingAbnahmeReport(
            database.DatabaseName,
            await database.GetSqlServerMajorVersionAsync(),
            DiffPageSize,
            messungen[^1].TotalCount,
            messungen.Count,
            messungen,
            istPlan,
            "Vollstaendige Snapshot-Ladung je Cursor-Seite konstant, unabhaengig vom Offset.");

        return TestMeasurementReports.WriteJson("diff-abnahme-messung.json", report);
    }

    private sealed record SnapshotZeilenStatistik(
        int Nodes,
        int Roles,
        int Resolutions,
        int Contents,
        int Dependencies)
    {
        public int[] ZeilenProStatement => [Nodes, Roles, Resolutions, Contents, Dependencies];
    }

    private sealed record SnapshotZeilen(SnapshotZeilenStatistik Basis, SnapshotZeilenStatistik Ziel);

    private sealed record SqlLadungsMessung(
        int[] BasisZeilenProStatement,
        int[] ZielZeilenProStatement,
        int LogicalReadsTotal,
        Dictionary<string, int> LogicalReadsByTable,
        long CpuMilliseconds,
        long ElapsedMilliseconds,
        double BatchWallClockMilliseconds);

    private sealed record DiffSeitenMessung(
        int Seitennummer,
        int ItemCount,
        int TotalCount,
        string? NextCursor,
        double ServiceWallClockMilliseconds,
        SqlLadungsMessung SqlMessung);

    private sealed record DiffPagingAbnahmeReport(
        string DatabaseName,
        int SqlServerMajorVersion,
        int PageSize,
        int DiffEintraegeGesamt,
        int Seitenzahl,
        IReadOnlyList<DiffSeitenMessung> Seiten,
        string IstPlanSummary,
        string? Notes);
}
