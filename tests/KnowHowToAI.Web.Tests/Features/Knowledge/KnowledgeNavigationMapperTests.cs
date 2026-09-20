using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Server.Web.Features.Knowledge;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class KnowledgeNavigationMapperTests
{
    [Fact]
    public void ToNodeDetailsViewModel_WithNull_ReturnsNull()
    {
        var result = KnowledgeNavigationMapper.ToNodeDetailsViewModel(null);
        Assert.Null(result);

        var emptyResult = KnowledgeNavigationMapper.ToNodeDetailsViewModel(new NodeWithContent(
            Node: null,
            RequestedAudienceId: new AudienceId("developer"),
            ResolvedAudienceId: null,
            Availability: Availability.None,
            FallbackUsed: false,
            Content: null,
            Freshness: Freshness.Current));
        Assert.Null(emptyResult);
    }

    [Fact]
    public void ToNodeDetailsViewModel_WithValidNode_MapsAllPropertiesAndChangeVersion()
    {
        var nodeId = new NodeId(Guid.NewGuid());
        var parentNodeId = new NodeId(Guid.NewGuid());
        var requestedRoleId = new AudienceId("developer");
        var resolvedRoleId = new AudienceId("architect");
        var contentRevisionId = new ContentRevisionId(Guid.NewGuid());
        var snapshotId = new SnapshotId(10);

        var node = new Node(
            snapshotId,
            nodeId,
            parentNodeId,
            "Architektur-Übersicht",
            "Wichtigste Systembausteine",
            1,
            IsDeleted: false);

        var content = new NodeContent(
            snapshotId,
            nodeId,
            resolvedRoleId,
            contentRevisionId,
            ContentMode.Independent,
            "# Systemarchitektur\nInhalt...",
            IsDeleted: false);

        var nodeWithContent = new NodeWithContent(
            node,
            requestedRoleId,
            resolvedRoleId,
            Availability.Explicit,
            FallbackUsed: false,
            content,
            Freshness.Current);

        var vm = KnowledgeNavigationMapper.ToNodeDetailsViewModel(nodeWithContent, changeVersion: 42L);

        Assert.NotNull(vm);
        Assert.Equal(nodeId.Value, vm.NodeId);
        Assert.Equal(parentNodeId.Value, vm.ParentNodeId);
        Assert.Equal("Architektur-Übersicht", vm.Title);
        Assert.Equal("Wichtigste Systembausteine", vm.Description);
        Assert.Equal(1, vm.SortOrder);
        Assert.Equal(requestedRoleId.Value, vm.RequestedRoleId);
        Assert.Equal(resolvedRoleId.Value, vm.ResolvedRoleId);
        Assert.False(vm.FallbackUsed);
        Assert.Equal("Explicit", vm.Availability);
        Assert.Equal("Current", vm.Freshness);
        Assert.Equal(contentRevisionId.Value, vm.ContentRevisionId);
        Assert.Equal("Independent", vm.ContentMode);
        Assert.Equal("# Systemarchitektur\nInhalt...", vm.ContentMd);
        Assert.Equal(42L, vm.ChangeVersion);
    }

    [Fact]
    public void ToChildrenPageViewModel_MapsSummariesAndPreservesCursor()
    {
        var parentId = new NodeId(Guid.NewGuid());
        var childId = new NodeId(Guid.NewGuid());
        var resolvedRoleId = new AudienceId("architect");

        var summary = new ChildNodeSummary(
            childId,
            "Unterknoten",
            "Beschreibung",
            2,
            ChildCount: 3,
            ContentSizeBytes: 1024,
            Availability.Fallback,
            resolvedRoleId,
            Freshness.Stale);

        var page = new ChildrenPage(
            parentId,
            new[] { summary },
            NextCursor: "opaque-cursor-123");

        var vm = KnowledgeNavigationMapper.ToChildrenPageViewModel(page);

        Assert.Equal(parentId.Value, vm.ParentNodeId);
        Assert.Equal("opaque-cursor-123", vm.NextCursor);
        var item = Assert.Single(vm.Items);
        Assert.Equal(childId.Value, item.NodeId);
        Assert.Equal("Unterknoten", item.Title);
        Assert.Equal("Beschreibung", item.Description);
        Assert.Equal(2, item.SortOrder);
        Assert.Equal(3, item.ChildCount);
        Assert.Equal(1024, item.ContentSizeBytes);
        Assert.Equal("Fallback", item.Availability);
        Assert.Equal(resolvedRoleId.Value, item.ResolvedRoleId);
        Assert.Equal("Stale", item.Freshness);
    }

    [Fact]
    public void ResultMappers_PreserveErrorsAndWarningsCompletely()
    {
        var error = new DomainError("NodeNotFound", "Knoten nicht gefunden.", new Dictionary<string, string> { ["nodeId"] = "test" });
        var warning = new DomainWarning("StaleContent", "Inhalt ist veraltet.");

        var failedNodeResult = Result<NodeWithContent>.Failure(error, new[] { warning });
        var vmErrorResult = KnowledgeNavigationMapper.ToNodeDetailsResult(failedNodeResult);

        Assert.False(vmErrorResult.IsSuccess);
        Assert.Equal("NodeNotFound", vmErrorResult.Error!.Code);
        Assert.Equal("Knoten nicht gefunden.", vmErrorResult.Error.Message);
        Assert.Equal("test", vmErrorResult.Error.Details["nodeId"]);
        var w = Assert.Single(vmErrorResult.Warnings);
        Assert.Equal("StaleContent", w.Code);

        var failedPageResult = Result<ChildrenPage>.Failure(error, new[] { warning });
        var vmPageErrorResult = KnowledgeNavigationMapper.ToChildrenPageResult(failedPageResult);

        Assert.False(vmPageErrorResult.IsSuccess);
        Assert.Equal("NodeNotFound", vmPageErrorResult.Error!.Code);
        Assert.Single(vmPageErrorResult.Warnings);
    }
}
