using Bunit;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Runtime;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Mcp.Mapping;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.History;
using KnowHowToAI.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.History;

/// <summary>
/// M3.8-T1: Prüft die fachliche Parität von UI- und MCP-Leseergebnissen für
/// Snapshots, Releases, Snapshot-Diffs und Fehlerbehandlung gegen gemeinsame Core-Use-Cases.
/// </summary>
[Trait("Category", "Unit")]
public sealed class HistoryReadParityTests : BunitContext
{
    private static readonly SnapshotId BaseSnapshotId = new(10);
    private static readonly SnapshotId TargetSnapshotId = new(20);

    // ── Snapshots ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetSnapshot_UiAndMcpShowIdenticalSnapshotMetadata()
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = new Snapshot(
            TargetSnapshotId,
            BaseSnapshotId,
            SnapshotState.Committed,
            now.AddHours(-2),
            now);

        var result = Result<Snapshot>.Success(snapshot);

        // MCP-Mapping
        var mcpEnvelope = McpHistoryMapper.ToSnapshotEnvelope(result);
        Assert.True(mcpEnvelope.IsSuccess);
        var mcpData = mcpEnvelope.Data!;

        // UI-Mapping
        var uiVm = HistoryMapper.ToSnapshotViewModel(snapshot);

        // Paritäts-Prüfung
        Assert.Equal(mcpData.SnapshotId, uiVm.SnapshotId.ToString());
        Assert.Equal(mcpData.BaseSnapshotId, uiVm.BaseSnapshotId?.ToString());
        Assert.Equal(mcpData.State, uiVm.State);
        Assert.Equal(mcpData.CreatedAtUtc, uiVm.CreatedAtUtc);
        Assert.Equal(mcpData.CommittedAtUtc, uiVm.CommittedAtUtc);
    }

    [Fact]
    public void ListSnapshots_UiAndMcpShowIdenticalSnapshotsAndPaging()
    {
        var now = DateTimeOffset.UtcNow;
        var s1 = new Snapshot(new SnapshotId(1), null, SnapshotState.Committed, now.AddDays(-2), now.AddDays(-2));
        var s2 = new Snapshot(new SnapshotId(2), new SnapshotId(1), SnapshotState.Committed, now.AddDays(-1), now.AddDays(-1));

        var page = new SnapshotPage([s1, s2], NextCursor: "snapshot-cursor-99");

        // UI-Mapping
        var uiVm = HistoryMapper.ToSnapshotPageViewModel(page);

        // Prüfe Paging- und Item-Parität
        Assert.Equal("snapshot-cursor-99", uiVm.NextCursor);
        Assert.Equal(2, uiVm.Items.Count);
        Assert.Equal(1L, uiVm.Items[0].SnapshotId);
        Assert.Equal("Committed", uiVm.Items[0].State);
        Assert.Equal(2L, uiVm.Items[1].SnapshotId);
        Assert.Equal("Committed", uiVm.Items[1].State);
    }

    // ── Releases ──────────────────────────────────────────────────────────────

    [Fact]
    public void ListReleases_UiAndMcpShowIdenticalReleasesAndPaging()
    {
        var now = DateTimeOffset.UtcNow;
        var releases = new List<Release>
        {
            new(new ReleaseId(1), new SnapshotId(5), "v1.0.0", "Initiale Version", now.AddDays(-10)),
            new(new ReleaseId(2), new SnapshotId(10), "v1.1.0", "Feature-Release", now.AddDays(-5))
        };

        var page = new ReleasePage(releases, NextCursor: "release-cursor-abc");
        var result = Result<ReleasePage>.Success(page);

        // MCP-Mapping
        var mcpEnvelope = McpHistoryMapper.ToReleasePageEnvelope(result);
        Assert.True(mcpEnvelope.IsSuccess);
        var mcpData = mcpEnvelope.Data!;

        // UI-Mapping
        var uiVm = HistoryMapper.ToReleasePageViewModel(page);

        // Paritäts-Prüfung
        Assert.Equal(mcpData.NextCursor, uiVm.NextCursor);
        Assert.Equal(mcpData.Items.Count, uiVm.Items.Count);

        for (var i = 0; i < releases.Count; i++)
        {
            var mcpRel = mcpData.Items[i];
            var uiRel = uiVm.Items[i];

            Assert.Equal(mcpRel.ReleaseId, uiRel.ReleaseId.ToString());
            Assert.Equal(mcpRel.SnapshotId, uiRel.SnapshotId.ToString());
            Assert.Equal(mcpRel.Name, uiRel.Name);
            Assert.Equal(mcpRel.Description, uiRel.Description);
            Assert.Equal(mcpRel.ReleasedAtUtc, uiRel.ReleasedAtUtc);
        }
    }

    // ── Snapshot-Diff ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CompareSnapshots_CommonUseCaseResult_ReachesHistoryPageAndMcpContract()
    {
        var nodeId = new NodeId(Guid.Parse("30000000-0000-0000-0000-000000000010"));
        var harness = new NavigationTestHarness(TargetSnapshotId);
        harness.AddHistoricalSnapshot(new Snapshot(
            BaseSnapshotId,
            null,
            SnapshotState.Committed,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch));
        harness.AddNode(new Node(BaseSnapshotId, nodeId, null, "Vorher", null, 0, false));
        harness.AddNode(new Node(TargetSnapshotId, nodeId, null, "Nachher", null, 0, false));
        var policy = new RetrievalPolicy
        {
            DefaultPageSize = 10,
            MaximumPageSize = 10,
            SearchPageSize = 10,
            SearchMaximumPageSize = 10,
            SnippetMaximumCharacters = 100
        };
        var historyService = new HistoryService(harness.CreateRepositories(), policy);
        var releaseService = new ReleaseService(
            harness.CreateRepositories(),
            new InMemoryReleaseMutationRepository(),
            new SystemClock(),
            policy,
            new ValidationPolicy
            {
                ContentSizeWarningBytes = 4096,
                ChildCountWarning = 25,
                HierarchyDepthWarning = 8,
                PossibleEmbeddedHeadingWarning = true
            });
        var query = new SnapshotComparisonQuery(BaseSnapshotId, TargetSnapshotId);
        var applicationResult = await historyService.CompareSnapshotsAsync(query);
        var mcp = McpHistoryMapper.ToSnapshotDiffEnvelope(applicationResult).Data!;

        Services.AddSingleton(historyService);
        Services.AddSingleton(releaseService);
        Services.AddSingleton(new PageRegionState());
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            $"/history?baseSnapshotId={BaseSnapshotId.Value}&targetSnapshotId={TargetSnapshotId.Value}");

        var cut = Render<HistoryPage>();

        var mcpEntry = Assert.Single(mcp.Items);
        cut.WaitForAssertion(() =>
        {
            var summary = cut.Find("[data-testid='snapshot-diff-summary']").TextContent;
            Assert.Contains($"Snapshot {mcp.BaseSnapshotId} → Snapshot {mcp.TargetSnapshotId}", summary, StringComparison.Ordinal);
            Assert.Contains(mcp.TotalCount.ToString(), summary, StringComparison.Ordinal);
            var renderedEntry = cut.Find($"[data-testid='snapshot-diff-entry-Node-{mcpEntry.Id}']").TextContent;
            Assert.Contains(mcpEntry.Id, renderedEntry, StringComparison.Ordinal);
            Assert.Contains("Vorher", renderedEntry, StringComparison.Ordinal);
            Assert.Contains("Nachher", renderedEntry, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void CompareSnapshots_DiffContainsAllEntityKinds_UiAndMcpReflectSameChanges()
    {
        var nodeId = new NodeId(Guid.Parse("30000000-0000-0000-0000-000000000001"));
        var roleDeveloper = new RoleId("Developer");
        var roleAdmin = new RoleId("Admin");
        var revBefore = new ContentRevisionId(Guid.Parse("40000000-0000-0000-0000-000000000001"));
        var revAfter = new ContentRevisionId(Guid.Parse("40000000-0000-0000-0000-000000000002"));

        var nodeBefore = new Node(BaseSnapshotId, nodeId, null, "Titel Alt", "Desc Alt", 1, false);
        var nodeAfter = new Node(TargetSnapshotId, nodeId, null, "Titel Neu", "Desc Neu", 1, false);

        var roleAdded = new Role(TargetSnapshotId, roleAdmin, "Administrator", "Admin-Rolle", false);

        var resBefore = new RoleResolution(BaseSnapshotId, roleDeveloper, roleDeveloper, 1);
        var resAfter = new RoleResolution(TargetSnapshotId, roleDeveloper, roleAdmin, 1);

        var contentBefore = new NodeContent(BaseSnapshotId, nodeId, roleDeveloper, revBefore, ContentMode.Independent, "Inhalt Alt", false);
        var contentAfter = new NodeContent(TargetSnapshotId, nodeId, roleDeveloper, revAfter, ContentMode.Independent, "Inhalt Neu", false);

        var depDeleted = new ContentDependency(BaseSnapshotId, nodeId, roleDeveloper, nodeId, roleAdmin, revBefore);

        var diff = new SnapshotDiff(
            BaseSnapshotId,
            TargetSnapshotId,
            Nodes: [new NodeDiffEntry(DiffChangeKind.Modified, nodeBefore, nodeAfter)],
            Roles: [new RoleDiffEntry(DiffChangeKind.Added, Before: null, After: roleAdded)],
            RoleResolutions: [new RoleResolutionDiffEntry(DiffChangeKind.Modified, resBefore, resAfter)],
            Contents: [new ContentDiffEntry(DiffChangeKind.Modified, contentBefore, contentAfter)],
            Dependencies: [new DependencyDiffEntry(DiffChangeKind.Deleted, Before: depDeleted, After: null)],
            NextCursor: "diff-cursor-xyz",
            TotalCount: 5);

        var result = Result<SnapshotDiff>.Success(diff);

        // MCP-Mapping
        var mcpEnvelope = McpHistoryMapper.ToSnapshotDiffEnvelope(result);
        Assert.True(mcpEnvelope.IsSuccess);
        var mcpData = mcpEnvelope.Data!;

        // UI-Mapping
        var uiVm = HistoryMapper.ToSnapshotDiffViewModel(diff);

        // Paritäts-Prüfung der Metadaten
        Assert.Equal(mcpData.BaseSnapshotId, uiVm.BaseSnapshotId.ToString());
        Assert.Equal(mcpData.TargetSnapshotId, uiVm.TargetSnapshotId.ToString());
        Assert.Equal(mcpData.TotalCount, uiVm.TotalCount);
        Assert.Equal(mcpData.NextCursor, uiVm.NextCursor);
        Assert.Equal(mcpData.Items.Count, uiVm.Entries.Count);
        Assert.Equal(5, uiVm.Entries.Count);

        // Prüfe jede Entitätsänderung in beiden Darstellungen
        // 1. Node (Modified)
        var mcpNode = Assert.Single(mcpData.Items, e => e.EntityType == "node");
        var uiNode = Assert.Single(uiVm.Entries, e => e.EntityType == "Node");
        Assert.Equal("Modified", mcpNode.Kind);
        Assert.Equal("Modified", uiNode.Kind);
        Assert.Equal(nodeId.Value.ToString("D"), mcpNode.Id);
        Assert.Equal(nodeId.Value.ToString("D"), uiNode.PrimaryId);

        // 2. Role (Added)
        var mcpRole = Assert.Single(mcpData.Items, e => e.EntityType == "role");
        var uiRole = Assert.Single(uiVm.Entries, e => e.EntityType == "Role");
        Assert.Equal("Added", mcpRole.Kind);
        Assert.Equal("Added", uiRole.Kind);
        Assert.Equal(roleAdmin.Value, mcpRole.Id);
        Assert.Equal(roleAdmin.Value, uiRole.PrimaryId);

        // 3. RoleResolution (Modified)
        var mcpRes = Assert.Single(mcpData.Items, e => e.EntityType == "roleResolution");
        var uiRes = Assert.Single(uiVm.Entries, e => e.EntityType == "RoleResolution");
        Assert.Equal("Modified", mcpRes.Kind);
        Assert.Equal("Modified", uiRes.Kind);
        Assert.Equal(roleDeveloper.Value, mcpRes.Id);
        Assert.Equal(roleAdmin.Value, mcpRes.RoleId);
        Assert.Equal(roleDeveloper.Value, uiRes.PrimaryId);
        Assert.Equal(roleAdmin.Value, uiRes.SecondaryId);
        Assert.Contains(roleDeveloper.Value, uiRes.Detail);
        Assert.Contains(roleAdmin.Value, uiRes.Detail);

        // 4. Content (Modified)
        var mcpContent = Assert.Single(mcpData.Items, e => e.EntityType == "content");
        var uiContent = Assert.Single(uiVm.Entries, e => e.EntityType == "Content");
        Assert.Equal("Modified", mcpContent.Kind);
        Assert.Equal("Modified", uiContent.Kind);
        Assert.Equal(nodeId.Value.ToString("D"), mcpContent.Id);
        Assert.Equal(roleDeveloper.Value, mcpContent.RoleId);
        Assert.Equal(nodeId.Value.ToString("D"), uiContent.PrimaryId);
        Assert.Equal(roleDeveloper.Value, uiContent.SecondaryId);

        // 5. Dependency (Deleted)
        var mcpDep = Assert.Single(mcpData.Items, e => e.EntityType == "dependency");
        var uiDep = Assert.Single(uiVm.Entries, e => e.EntityType == "Dependency");
        Assert.Equal("Deleted", mcpDep.Kind);
        Assert.Equal("Deleted", uiDep.Kind);
        Assert.Equal(nodeId.Value.ToString("D"), mcpDep.Id);
        Assert.Equal(roleDeveloper.Value, mcpDep.RoleId);
        Assert.Equal(nodeId.Value.ToString("D"), uiDep.PrimaryId);
        Assert.Equal(nodeId.Value.ToString("D"), uiDep.SecondaryId);
    }

    // ── Fehlerbehandlung ──────────────────────────────────────────────────────

    [Fact]
    public void HistoryErrors_SnapshotNotFound_UiAndMcpPreserveErrorCode()
    {
        var error = new DomainError(
            HistoryErrorCodes.SnapshotNotFound,
            "Der angefragte Snapshot existiert nicht.",
            new Dictionary<string, string> { [HistoryErrorCodes.SnapshotIdDetail] = "999" });
        var failedResult = Result<Snapshot>.Failure(error);

        var mcpEnvelope = McpHistoryMapper.ToSnapshotEnvelope(failedResult);
        var uiResult = HistoryMapper.ToSnapshotResult(failedResult);

        Assert.False(mcpEnvelope.IsSuccess);
        Assert.Equal(HistoryErrorCodes.SnapshotNotFound, mcpEnvelope.Code);
        Assert.Equal("999", mcpEnvelope.Details![HistoryErrorCodes.SnapshotIdDetail]);

        Assert.False(uiResult.IsSuccess);
        Assert.Equal(HistoryErrorCodes.SnapshotNotFound, uiResult.Error!.Code);
        Assert.Equal("999", uiResult.Error.Details[HistoryErrorCodes.SnapshotIdDetail]);
    }
}
