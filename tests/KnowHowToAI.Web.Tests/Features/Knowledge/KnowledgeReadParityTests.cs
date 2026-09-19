using System.Text;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Retrieval.Export;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Navigation;
using KnowHowToAI.Server.Mcp.Mapping;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

/// <summary>
/// M3.8-T1: Prüft die fachliche Parität von UI- und MCP-Leseergebnissen für
/// Navigation, Rollenauflösung, Markdown-Export und Kontextgrenzen gegen gemeinsame Core-Use-Cases.
/// </summary>
[Trait("Category", "Unit")]
public sealed class KnowledgeReadParityTests
{
    private static readonly SnapshotId TestSnapshotId = new(100);
    private static readonly RoleId RoleDeveloper = new("Developer");
    private static readonly RoleId RoleDefault = new("Default");
    private static readonly NodeId RootId = new(Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly NodeId ChildId = new(Guid.Parse("10000000-0000-0000-0000-000000000002"));

    private sealed class FakeReleaseRepository : IReleaseRepository
    {
        private readonly Dictionary<ReleaseId, Release> _releases = new();

        public void Add(Release release) => _releases[release.ReleaseId] = release;

        public Task<Release?> FindAsync(ReleaseId releaseId, CancellationToken cancellationToken = default)
        {
            _releases.TryGetValue(releaseId, out var release);
            return Task.FromResult(release);
        }
    }

    // ── Navigation & Node Details ─────────────────────────────────────────────

    [Fact]
    public void GetNode_WithResolvedContent_UiAndMcpShowIdenticalState()
    {
        var revisionId = new ContentRevisionId(Guid.NewGuid());
        var node = new Node(
            TestSnapshotId,
            RootId,
            ParentNodeId: null,
            Title: "Architektur-Übersicht",
            Description: "Beschreibung der Architektur",
            SortOrder: 1,
            IsDeleted: false);

        var content = new NodeContent(
            TestSnapshotId,
            RootId,
            RoleDeveloper,
            revisionId,
            ContentMode.Independent,
            "# Systemarchitektur\nInhalt...",
            IsDeleted: false);

        var nodeWithContent = new NodeWithContent(
            node,
            RoleDeveloper,
            RoleDeveloper,
            Availability.Explicit,
            FallbackUsed: false,
            content,
            Freshness.Current);

        var result = Result<NodeWithContent>.Success(nodeWithContent);

        // MCP-Mapping
        var mcpEnvelope = McpNavigationMapper.ToEnvelope(result);
        Assert.True(mcpEnvelope.IsSuccess);
        var mcpData = mcpEnvelope.Data!;

        // UI-Mapping
        var uiVm = KnowledgeNavigationMapper.ToNodeDetailsViewModel(nodeWithContent)!;
        Assert.NotNull(uiVm);

        // Paritäts-Prüfung fachlicher Zustand
        Assert.Equal(mcpData.NodeId, uiVm.NodeId.ToString("D"));
        Assert.Equal(mcpData.Title, uiVm.Title);
        Assert.Equal(mcpData.Description, uiVm.Description);
        Assert.Equal(mcpData.SortOrder, uiVm.SortOrder);
        Assert.Equal(mcpData.RequestedRole, uiVm.RequestedRoleId);
        Assert.Equal(mcpData.ResolvedRole, uiVm.ResolvedRoleId);
        Assert.Equal(mcpData.FallbackUsed, uiVm.FallbackUsed);
        Assert.Equal(mcpData.Availability, uiVm.Availability);
        Assert.Equal(mcpData.Freshness, uiVm.Freshness);
        Assert.Equal(mcpData.ContentRevisionId, uiVm.ContentRevisionId?.ToString("D"));
        Assert.Equal(mcpData.Content, uiVm.ContentMd);
    }

    [Fact]
    public void GetNode_EmptyRoot_UiAndMcpShowConsistentEmptyState()
    {
        var emptyRoot = new NodeWithContent(
            Node: null,
            RequestedRoleId: RoleDeveloper,
            ResolvedRoleId: null,
            Availability: Availability.None,
            FallbackUsed: false,
            Content: null,
            Freshness: Freshness.Current);

        var result = Result<NodeWithContent>.Success(emptyRoot);

        var mcpEnvelope = McpNavigationMapper.ToEnvelope(result);
        var uiVm = KnowledgeNavigationMapper.ToNodeDetailsViewModel(emptyRoot);

        // MCP liefert Success ohne Data; UI liefert null-ViewModel
        Assert.True(mcpEnvelope.IsSuccess);
        Assert.Null(mcpEnvelope.Data);
        Assert.Null(uiVm);
    }

    // ── Rollenauflösung (Fallback, Stale, Missing) ─────────────────────────────

    [Fact]
    public void GetNode_WithRoleFallback_UiAndMcpReflectFallbackState()
    {
        var revisionId = new ContentRevisionId(Guid.NewGuid());
        var node = new Node(TestSnapshotId, RootId, null, "Root", "Desc", 0, false);
        var fallbackContent = new NodeContent(
            TestSnapshotId,
            RootId,
            RoleDefault,
            revisionId,
            ContentMode.Independent,
            "Standardinhalt",
            false);

        var nodeWithContent = new NodeWithContent(
            node,
            RequestedRoleId: RoleDeveloper,
            ResolvedRoleId: RoleDefault,
            Availability: Availability.Fallback,
            FallbackUsed: true,
            Content: fallbackContent,
            Freshness: Freshness.Current);

        var result = Result<NodeWithContent>.Success(nodeWithContent);

        var mcpEnvelope = McpNavigationMapper.ToEnvelope(result);
        var uiVm = KnowledgeNavigationMapper.ToNodeDetailsViewModel(nodeWithContent)!;

        // Beide Transportsysteme müssen den Fallback konsistent ausweisen
        Assert.Equal(mcpData(mcpEnvelope).RequestedRole, uiVm.RequestedRoleId);
        Assert.Equal("Developer", uiVm.RequestedRoleId);
        Assert.Equal(mcpData(mcpEnvelope).ResolvedRole, uiVm.ResolvedRoleId);
        Assert.Equal("Default", uiVm.ResolvedRoleId);
        Assert.True(mcpData(mcpEnvelope).FallbackUsed);
        Assert.True(uiVm.FallbackUsed);
        Assert.Equal("Fallback", mcpData(mcpEnvelope).Availability);
        Assert.Equal("Fallback", uiVm.Availability);
        Assert.Equal("Standardinhalt", mcpData(mcpEnvelope).Content);
        Assert.Equal("Standardinhalt", uiVm.ContentMd);
    }

    [Fact]
    public void GetNode_WithStaleContentAndWarnings_UiAndMcpPreserveFreshnessAndWarnings()
    {
        var revisionId = new ContentRevisionId(Guid.NewGuid());
        var node = new Node(TestSnapshotId, RootId, null, "Root", "Desc", 0, false);
        var derivedContent = new NodeContent(
            TestSnapshotId,
            RootId,
            RoleDeveloper,
            revisionId,
            ContentMode.Derived,
            "Abgeleiteter Inhalt",
            false);

        var nodeWithContent = new NodeWithContent(
            node,
            RoleDeveloper,
            RoleDeveloper,
            Availability.Explicit,
            FallbackUsed: false,
            derivedContent,
            Freshness.Stale);

        var warning = new DomainWarning("StaleContent", "Der abgeleitete Inhalt basiert auf einer veralteten Source-Revision.");
        var result = Result<NodeWithContent>.Success(nodeWithContent, [warning]);

        var mcpEnvelope = McpNavigationMapper.ToEnvelope(result);
        var uiResult = KnowledgeNavigationMapper.ToNodeDetailsResult(result);

        // Fachlicher Zustand
        Assert.Equal("Stale", mcpEnvelope.Data!.Freshness);
        Assert.Equal("Stale", uiResult.Value!.Freshness);

        // Warnungen müssen in beiden Modellen vollständig vorhanden sein
        Assert.NotNull(mcpEnvelope.Warnings);
        var mcpWarning = Assert.Single(mcpEnvelope.Warnings);
        Assert.Equal("StaleContent", mcpWarning.Code);
        Assert.Equal(warning.Message, mcpWarning.Message);

        var uiWarning = Assert.Single(uiResult.Warnings);
        Assert.Equal("StaleContent", uiWarning.Code);
        Assert.Equal(warning.Message, uiWarning.Message);
    }

    [Fact]
    public void GetNode_WithNoContent_UiAndMcpReflectNoneAvailability()
    {
        var node = new Node(TestSnapshotId, RootId, null, "Root", "Desc", 0, false);
        var nodeWithContent = new NodeWithContent(
            node,
            RequestedRoleId: RoleDeveloper,
            ResolvedRoleId: null,
            Availability: Availability.None,
            FallbackUsed: false,
            Content: null,
            Freshness: Freshness.Current);

        var result = Result<NodeWithContent>.Success(nodeWithContent);

        var mcpEnvelope = McpNavigationMapper.ToEnvelope(result);
        var uiVm = KnowledgeNavigationMapper.ToNodeDetailsViewModel(nodeWithContent)!;

        Assert.Equal("None", mcpEnvelope.Data!.Availability);
        Assert.Equal("None", uiVm.Availability);
        Assert.Null(mcpEnvelope.Data.ResolvedRole);
        Assert.Null(uiVm.ResolvedRoleId);
        Assert.Null(mcpEnvelope.Data.Content);
        Assert.Null(uiVm.ContentMd);
        Assert.Null(mcpEnvelope.Data.ContentRevisionId);
        Assert.Null(uiVm.ContentRevisionId);
    }

    // ── Children Navigation ───────────────────────────────────────────────────

    [Fact]
    public void ListChildren_UiAndMcpShowIdenticalChildrenAndPaging()
    {
        var children = new List<ChildNodeSummary>
        {
            new(
                new NodeId(Guid.NewGuid()),
                "Kind 1",
                "Erstes Kind",
                SortOrder: 1,
                ChildCount: 2,
                ContentSizeBytes: 512,
                Availability.Explicit,
                RoleDeveloper,
                Freshness.Current),
            new(
                new NodeId(Guid.NewGuid()),
                "Kind 2",
                "Zweites Kind",
                SortOrder: 2,
                ChildCount: 0,
                ContentSizeBytes: 256,
                Availability.Fallback,
                RoleDefault,
                Freshness.Stale)
        };

        var page = new ChildrenPage(RootId, children, NextCursor: "cursor-token-42");
        var result = Result<ChildrenPage>.Success(page);

        var mcpEnvelope = McpNavigationMapper.ToEnvelope(result);
        var uiVm = KnowledgeNavigationMapper.ToChildrenPageViewModel(page);

        Assert.True(mcpEnvelope.IsSuccess);
        Assert.Equal(mcpEnvelope.Data!.ParentNodeId, uiVm.ParentNodeId?.ToString("D"));
        Assert.Equal(mcpEnvelope.Data.NextCursor, uiVm.NextCursor);
        Assert.Equal(mcpEnvelope.Data.Items.Count, uiVm.Items.Count);

        for (var i = 0; i < children.Count; i++)
        {
            var mcpChild = mcpEnvelope.Data.Items[i];
            var uiChild = uiVm.Items[i];

            Assert.Equal(mcpChild.NodeId, uiChild.NodeId.ToString("D"));
            Assert.Equal(mcpChild.Title, uiChild.Title);
            Assert.Equal(mcpChild.Description, uiChild.Description);
            Assert.Equal(mcpChild.SortOrder, uiChild.SortOrder);
            Assert.Equal(mcpChild.ChildCount, uiChild.ChildCount);
            Assert.Equal(mcpChild.ContentSizeBytes, uiChild.ContentSizeBytes);
            Assert.Equal(mcpChild.Availability, uiChild.Availability);
            Assert.Equal(mcpChild.ResolvedRole, uiChild.ResolvedRoleId);
            Assert.Equal(mcpChild.Freshness, uiChild.Freshness);
        }
    }

    // ── Markdown-Export ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExportTree_UiAndMcpDeliverIdenticalMarkdownContent()
    {
        var harness = new NavigationTestHarness(TestSnapshotId);
        harness.AddNode(new Node(TestSnapshotId, RootId, null, "Export-Root", "Root-Beschreibung", 0, false));
        harness.AddNode(new Node(TestSnapshotId, ChildId, RootId, "Export-Kind", "Kind-Beschreibung", 1, false));

        var rootRev = new ContentRevisionId(Guid.NewGuid());
        harness.AddContent(new NodeContent(
            TestSnapshotId, RootId, RoleDeveloper, rootRev, ContentMode.Independent, "Root Markdown Text", false));

        var childRev = new ContentRevisionId(Guid.NewGuid());
        harness.AddContent(new NodeContent(
            TestSnapshotId, ChildId, RoleDeveloper, childRev, ContentMode.Independent, "Kind Markdown Text", false));

        var exportService = harness.CreateExportService();
        var exportResult = await exportService.ExportTreeAsync(RootId, new ReadContext(), RoleDeveloper);
        Assert.True(exportResult.IsSuccess);

        // MCP-Mapping
        var mcpEnvelope = McpRetrievalMapper.ToExportEnvelope(exportResult);
        Assert.True(mcpEnvelope.IsSuccess);

        // UI-Download liefert dieselben UTF-8-Bytes wie der MCP-Text
        var uiBytes = Encoding.UTF8.GetBytes(exportResult.Value!);
        var mcpBytes = Encoding.UTF8.GetBytes(mcpEnvelope.Data!.Markdown);

        Assert.Equal(exportResult.Value, mcpEnvelope.Data.Markdown);
        Assert.Equal(uiBytes, mcpBytes);
    }

    // ── Kontextgrenzen & Fehlerbehandlung ─────────────────────────────────────

    [Fact]
    public async Task ContextBoundaries_MutualExclusionAndValidationErrors_UiAndMcpEnforceSameErrorCodes()
    {
        // 1. Gegenseitiger Ausschluss von transactionId und snapshotId
        var txRaw = Guid.NewGuid().ToString("D");
        var snapRaw = "42";

        var mcpContextResult = McpReadContextMapper.ToApplicationContext(
            new McpReadContextRequest(TransactionId: txRaw, SnapshotId: snapRaw));
        Assert.False(mcpContextResult.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, mcpContextResult.Error!.Code);

        var webResolver = new WebReadContextResolver(new FakeReleaseRepository(), new InMemoryTransactionRepository(new InMemoryKnowledgeStore()));
        var webContextResult = await webResolver.ResolveAsync(txRaw, snapRaw, releaseIdRaw: null);
        Assert.False(webContextResult.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, webContextResult.Error!.Code);

        // 2. Ungültige UUID für Transaktions-ID
        var invalidTx = "nicht-eine-guid";
        var mcpTxResult = McpReadContextMapper.ToApplicationContext(
            new McpReadContextRequest(TransactionId: invalidTx));
        Assert.False(mcpTxResult.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, mcpTxResult.Error!.Code);

        var webTxResult = await webResolver.ResolveAsync(invalidTx, snapshotIdRaw: null, releaseIdRaw: null);
        Assert.False(webTxResult.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, webTxResult.Error!.Code);

        // 3. Ungültige Zahl für Snapshot-ID
        var invalidSnap = "keine-zahl";
        var mcpSnapResult = McpReadContextMapper.ToApplicationContext(
            new McpReadContextRequest(SnapshotId: invalidSnap));
        Assert.False(mcpSnapResult.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, mcpSnapResult.Error!.Code);

        var webSnapResult = await webResolver.ResolveAsync(transactionIdRaw: null, invalidSnap, releaseIdRaw: null);
        Assert.False(webSnapResult.IsSuccess);
        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, webSnapResult.Error!.Code);

        // 4. Nicht gefundener Node liefert in beiden Systemen NodeNotFound
        var notFoundError = new DomainError(NavigationErrorCodes.NodeNotFound, "Node existiert nicht.");
        var failedResult = Result<NodeWithContent>.Failure(notFoundError);

        var mcpNotFound = McpNavigationMapper.ToEnvelope(failedResult);
        var uiNotFound = KnowledgeNavigationMapper.ToNodeDetailsResult(failedResult);

        Assert.False(mcpNotFound.IsSuccess);
        Assert.Equal(NavigationErrorCodes.NodeNotFound, mcpNotFound.Code);
        Assert.False(uiNotFound.IsSuccess);
        Assert.Equal(NavigationErrorCodes.NodeNotFound, uiNotFound.Error!.Code);
    }

    private static McpNodeData mcpData(McpToolEnvelope<McpNodeData> envelope) => envelope.Data!;
}
