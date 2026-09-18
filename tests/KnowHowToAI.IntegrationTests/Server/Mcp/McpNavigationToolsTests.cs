using System.Text.Json;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Tests.Application.Navigation;
using KnowHowToAI.Server.Mcp.Tools.Navigation;
namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Handler-Vertragstests der Navigation-Tools (get_root, get_node, list_children,
/// list_roles): dünne Delegation an den NavigationService mit protokollkonformer
/// Error-Struktur, ID-Round-Trip und Paging-Grenzen. Keine SQL- oder Server-Infrastruktur.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpNavigationToolsTests
{
    private static readonly SnapshotId CurrentSnapshotId = new(100);
    private static readonly RoleId RoleDeveloper = new("Developer");
    private static readonly RoleId RoleAdmin = new("Admin");
    private static readonly RoleId RoleConsultant = new("Consultant");
    private static readonly RoleId RoleUnknown = new("Nonexistent");

    private static readonly NodeId RootId = new(Guid.Parse("30000000-0000-0000-0000-000000000000"));
    private static readonly NodeId Child1Id = new(Guid.Parse("30000000-0000-0000-0000-000000000001"));
    private static readonly NodeId Child2Id = new(Guid.Parse("30000000-0000-0000-0000-000000000002"));
    private static readonly NodeId Child3Id = new(Guid.Parse("30000000-0000-0000-0000-000000000003"));
    private static readonly NodeId UnknownNodeId = new(Guid.Parse("30000000-0000-0000-0000-000000009999"));

    [Fact]
    public async Task GetRoot_WithResolvedContent_MapsNodeDataWithResolvedRoleAndContent()
    {
        var harness = CreateHarnessWithRootAndChild();
        var tools = CreateTools(harness);

        var envelope = await tools.GetRoot(RoleDeveloper.Value);

        Assert.True(envelope.IsSuccess);
        Assert.Equal(RootId.ToString(), envelope.Data!.NodeId);
        Assert.Equal("Hauptkapitel", envelope.Data.Title);
        Assert.Equal(RoleDeveloper.ToString(), envelope.Data.RequestedRole);
        Assert.Equal(RoleDeveloper.ToString(), envelope.Data.ResolvedRole);
        Assert.False(envelope.Data.FallbackUsed);
        Assert.Equal(nameof(Availability.Explicit), envelope.Data.Availability);
        Assert.Equal(nameof(Freshness.Current), envelope.Data.Freshness);
        Assert.Equal("Inhalt Hauptkapitel.", envelope.Data.Content);
        Assert.NotNull(envelope.Data.ContentRevisionId);
    }

    [Fact]
    public async Task GetRoot_EmptySnapshot_ReturnsSuccessWithoutData()
    {
        var tools = CreateTools(new NavigationTestHarness(CurrentSnapshotId));

        var envelope = await tools.GetRoot(RoleDeveloper.Value);

        Assert.True(envelope.IsSuccess);
        Assert.Equal("Success", envelope.Code);
        Assert.Null(envelope.Data);
        Assert.Null(envelope.Message);
    }

    [Fact]
    public async Task GetNode_UnknownNodeId_ReturnsStableNodeNotFoundEnvelope()
    {
        var tools = CreateTools(CreateHarnessWithRootAndChild());

        var envelope = await tools.GetNode(UnknownNodeId.ToString(), RoleDeveloper.Value);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(NavigationErrorCodes.NodeNotFound, envelope.Code);
        Assert.Equal(UnknownNodeId.ToString(), envelope.Details![NavigationErrorCodes.NodeIdDetail]);
        Assert.Null(envelope.Data);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData("17")]
    public async Task GetNode_MalformedNodeId_IsRejectedAsInvalidNodeId(string rawNodeId)
    {
        var tools = CreateTools(CreateHarnessWithRootAndChild());

        var envelope = await tools.GetNode(rawNodeId, RoleDeveloper.Value);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidNodeId, envelope.Code);
        Assert.Equal(rawNodeId, envelope.Details![NavigationErrorCodes.NodeIdDetail]);
    }

    [Theory]
    [InlineData("get_node")]
    [InlineData("list_children")]
    [InlineData("list_roles")]
    public async Task ReadTools_WithBothSelectors_ReturnsInvalidReadContext(string toolName)
    {
        var tools = CreateTools(CreateHarnessWithRootAndChild());
        const string transactionId = "0d0b1f5a-4e12-4c1e-9f31-5d3e2a8d7b90";
        const string snapshotId = "100";

        var envelope = toolName switch
        {
            "get_node" => (await tools.GetNode(
                RootId.ToString(), RoleDeveloper.Value, transactionId, snapshotId)).Code,
            "list_children" => (await tools.ListChildren(
                RoleDeveloper.Value, RootId.ToString(), transactionId, snapshotId)).Code,
            _ => (await tools.ListRoles(transactionId, snapshotId)).Code
        };

        Assert.Equal(ReadContextErrorCodes.InvalidReadContext, envelope);
    }

    [Fact]
    public async Task ListChildren_MapsMetadataFirstSummariesInDeterministicOrder()
    {
        var harness = CreateHarnessWithRootAndChild();
        var tools = CreateTools(harness);

        var envelope = await tools.ListChildren(RoleDeveloper.Value, parentNodeId: RootId.ToString());

        Assert.True(envelope.IsSuccess);
        Assert.Equal(RootId.ToString(), envelope.Data!.ParentNodeId);
        Assert.Equal(2, envelope.Data.Items.Count);
        Assert.Equal(Child1Id.ToString(), envelope.Data.Items[0].NodeId);
        Assert.Equal(Child2Id.ToString(), envelope.Data.Items[1].NodeId);
        Assert.Equal("Unterabschnitt 1", envelope.Data.Items[0].Title);
        Assert.Equal(1, envelope.Data.Items[0].ChildCount);
        Assert.Equal(0, envelope.Data.Items[1].ChildCount);
        Assert.Equal(0, envelope.Data.Items[0].ContentSizeBytes);
        Assert.Equal(22, envelope.Data.Items[1].ContentSizeBytes);
        Assert.Equal(nameof(Availability.None), envelope.Data.Items[0].Availability);
        Assert.Equal(nameof(Availability.Explicit), envelope.Data.Items[1].Availability);
        Assert.Null(envelope.Data.NextCursor);
    }

    [Fact]
    public async Task ListChildren_MissingLimit_UsesConfiguredDefaultPageSize()
    {
        var tools = CreateTools(CreateHarnessWithThreeChildren());

        var envelope = await tools.ListChildren(RoleDeveloper.Value, parentNodeId: RootId.ToString());

        Assert.True(envelope.IsSuccess);
        Assert.Equal(2, envelope.Data!.Items.Count);
        Assert.NotNull(envelope.Data.NextCursor);
    }

    [Fact]
    public async Task ListChildren_LimitAboveMaximum_IsClampedToMaximumPageSize()
    {
        var tools = CreateTools(CreateHarnessWithThreeChildren());

        var envelope = await tools.ListChildren(RoleDeveloper.Value, parentNodeId: RootId.ToString(), limit: 9999);

        Assert.True(envelope.IsSuccess);
        Assert.Equal(3, envelope.Data!.Items.Count);
        Assert.Null(envelope.Data.NextCursor);
    }

    [Fact]
    public async Task ListChildren_NextCursor_ContinuesWithoutDuplicatesOrGaps()
    {
        var tools = CreateTools(CreateHarnessWithThreeChildren());

        var firstPage = await tools.ListChildren(RoleDeveloper.Value, parentNodeId: RootId.ToString());
        var secondPage = await tools.ListChildren(
            RoleDeveloper.Value, parentNodeId: RootId.ToString(), cursor: firstPage.Data!.NextCursor);

        Assert.True(firstPage.IsSuccess);
        Assert.True(secondPage.IsSuccess);
        var nodeIds = firstPage.Data!.Items.Select(item => item.NodeId)
            .Concat(secondPage.Data!.Items.Select(item => item.NodeId))
            .ToArray();
        Assert.Equal(3, nodeIds.Length);
        Assert.Equal(
            new[] { Child1Id.ToString(), Child2Id.ToString(), Child3Id.ToString() },
            nodeIds);
        Assert.Null(secondPage.Data.NextCursor);
    }

    [Fact]
    public async Task ListChildren_InvalidCursor_ReturnsStableInvalidCursorEnvelope()
    {
        var tools = CreateTools(CreateHarnessWithThreeChildren());

        var envelope = await tools.ListChildren(RoleDeveloper.Value, RootId.ToString(), cursor: "kaputter-cursor");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(NavigationErrorCodes.InvalidCursor, envelope.Code);
        Assert.Equal("kaputter-cursor", envelope.Details![NavigationErrorCodes.CursorDetail]);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task ListChildren_UnknownRole_ReturnsStableRoleErrorEnvelope()
    {
        var tools = CreateTools(CreateHarnessWithThreeChildren());

        var envelope = await tools.ListChildren(RoleUnknown.Value, RootId.ToString());

        Assert.False(envelope.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleNotFound, envelope.Code);
        Assert.Equal(
            RoleUnknown.ToString(),
            envelope.Details![RoleResolutionErrorCodes.RequestedRoleIdDetail]);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task ListRoles_MapsRolesInOrdinalOrderAndPaginates()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        harness.AddRole(new Role(CurrentSnapshotId, RoleConsultant, "Consultant", null, false));
        harness.AddRole(new Role(CurrentSnapshotId, RoleAdmin, "Admin", "Verwaltung", false));
        var tools = CreateTools(harness);

        var firstPage = await tools.ListRoles(limit: 2);
        var secondPage = await tools.ListRoles(limit: 2, cursor: firstPage.Data!.NextCursor);

        Assert.True(firstPage.IsSuccess);
        Assert.Equal(
            new[] { RoleAdmin.ToString(), RoleConsultant.ToString() },
            firstPage.Data!.Items.Select(item => item.RoleId).ToArray());
        Assert.Equal("Admin", firstPage.Data.Items[0].Name);
        Assert.Equal("Verwaltung", firstPage.Data.Items[0].Description);
        Assert.NotNull(firstPage.Data.NextCursor);
        Assert.True(secondPage.IsSuccess);
        Assert.Equal(
            new[] { RoleDeveloper.ToString() },
            secondPage.Data!.Items.Select(item => item.RoleId).ToArray());
        Assert.Null(secondPage.Data.NextCursor);
    }

    [Fact]
    public async Task SuccessAndErrorEnvelopes_SerializeWithStableCamelCaseFieldNames()
    {
        var tools = CreateTools(CreateHarnessWithRootAndChild());
        var successJson = JsonSerializer.Serialize(await tools.GetRoot(RoleDeveloper.Value));
        var errorTools = CreateTools(new NavigationTestHarness(CurrentSnapshotId));
        var errorJson = JsonSerializer.Serialize(await errorTools.GetNode(
            UnknownNodeId.ToString(), RoleDeveloper.Value));

        using var success = JsonDocument.Parse(successJson);
        Assert.Equal("Success", success.RootElement.GetProperty("code").GetString());
        Assert.Equal(RootId.ToString(),
            success.RootElement.GetProperty("data").GetProperty("nodeId").GetString());
        Assert.Equal("Hauptkapitel",
            success.RootElement.GetProperty("data").GetProperty("title").GetString());
        Assert.Equal("Inhalt Hauptkapitel.",
            success.RootElement.GetProperty("data").GetProperty("content").GetString());
        Assert.DoesNotContain("\"message\"", successJson);

        using var error = JsonDocument.Parse(errorJson);
        Assert.Equal(NavigationErrorCodes.NodeNotFound, error.RootElement.GetProperty("code").GetString());
        Assert.Equal(UnknownNodeId.ToString(),
            error.RootElement.GetProperty("details").GetProperty("nodeId").GetString());
        Assert.False(error.RootElement.TryGetProperty("data", out _));
    }

    private static NavigationTestHarness CreateHarnessWithRootAndChild()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Hauptkapitel", null, 0, false));
        harness.AddNode(new Node(CurrentSnapshotId, Child1Id, RootId, "Unterabschnitt 1", null, 1, false));
        harness.AddNode(new Node(CurrentSnapshotId, Child2Id, RootId, "Unterabschnitt 2", null, 2, false));
        harness.AddNode(new Node(CurrentSnapshotId, Child3Id, Child1Id, "Detailpunkt", null, 1, false));
        harness.AddContent(new NodeContent(
            CurrentSnapshotId, RootId, RoleDeveloper,
            new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent,
            "Inhalt Hauptkapitel.", false));
        harness.AddContent(new NodeContent(
            CurrentSnapshotId, Child2Id, RoleDeveloper,
            new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent,
            "Inhalt Unterabschnitt.", false));
        harness.AddContent(new NodeContent(
            CurrentSnapshotId, Child3Id, RoleDeveloper,
            new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent,
            "Inhalt Detailpunkt.", false));
        return harness;
    }

    private static NavigationTestHarness CreateHarnessWithThreeChildren()
    {
        var harness = new NavigationTestHarness(CurrentSnapshotId);
        harness.AddNode(new Node(CurrentSnapshotId, RootId, null, "Hauptkapitel", null, 0, false));
        harness.AddNode(new Node(CurrentSnapshotId, Child1Id, RootId, "Unterabschnitt 1", null, 1, false));
        harness.AddNode(new Node(CurrentSnapshotId, Child2Id, RootId, "Unterabschnitt 2", null, 2, false));
        harness.AddNode(new Node(CurrentSnapshotId, Child3Id, RootId, "Unterabschnitt 3", null, 3, false));
        return harness;
    }

    private static NavigationTools CreateTools(NavigationTestHarness harness)
    {
        const int defaultPageSize = 2;
        const int maximumPageSize = 3;
        var policy = new RetrievalPolicy
        {
            DefaultPageSize = defaultPageSize,
            MaximumPageSize = maximumPageSize,
            SearchPageSize = defaultPageSize,
            SearchMaximumPageSize = maximumPageSize,
            SnippetMaximumCharacters = 100
        };
        return new NavigationTools(harness.CreateService(defaultPageSize, maximumPageSize), policy);
    }
}
