using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Search;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Web.Tests.Features.Search;

[Trait("Category", "Unit")]
public sealed class SearchBreadcrumbLoaderTests
{
    [Fact]
    public async Task LoadAsync_MapsTheCompleteAncestorPathWithoutExposingDomainTypes()
    {
        var snapshotId = new SnapshotId(1);
        var roleId = new RoleId("Developer");
        var harness = new NavigationTestHarness(snapshotId);
        var rootId = new NodeId(Guid.Parse("60000000-0000-0000-0000-000000000006"));
        var childId = new NodeId(Guid.Parse("70000000-0000-0000-0000-000000000007"));
        harness.AddNode(new Node(snapshotId, rootId, null, "Root", null, 0, false));
        harness.AddNode(new Node(snapshotId, childId, rootId, "Kind", null, 0, false));
        var page = new SearchPageViewModel("TODO",
        [
            new SearchHitViewModel(childId.Value, "Kind", null, "TODO", "Content", "Explicit", roleId.Value,
                "Current", 0, ["Kind"])
        ], null);

        var resolved = await new SearchBreadcrumbLoader(harness.CreateService()).LoadAsync(
            page,
            new ReadContext(),
            roleId.Value,
            CancellationToken.None);

        Assert.True(resolved.IsSuccess);
        Assert.Equal(["Root", "Kind"], Assert.Single(resolved.Value!.Items).Breadcrumb);
    }
}
