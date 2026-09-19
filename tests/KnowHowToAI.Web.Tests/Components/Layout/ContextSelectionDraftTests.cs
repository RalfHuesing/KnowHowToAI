using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.State;

namespace KnowHowToAI.Web.Tests.Components.Layout;

[Trait("Category", "Unit")]
public sealed class ContextSelectionDraftTests
{
    [Fact]
    public void SnapshotWithoutPositiveId_IsRejected()
    {
        var draft = new ContextSelectionDraft
        {
            SelectedKind = KnowledgeReadContextKind.Snapshot,
            SnapshotIdInput = "0"
        };

        var result = draft.BuildReadContext(ContextSelectionOptionsViewModel.Empty);

        Assert.False(result.IsSuccess);
        Assert.Contains("positive Snapshot-ID", result.Error!.Message);
    }

    [Fact]
    public void SelectedRelease_UsesItsResolvedSnapshotForRoleLoading()
    {
        var draft = new ContextSelectionDraft
        {
            SelectedKind = KnowledgeReadContextKind.Release,
            SelectedReleaseId = "3"
        };
        var options = new ContextSelectionOptionsViewModel(
            [new ContextSelectionReleaseOptionViewModel("3", "v3", 42)],
            [],
            []);

        var result = draft.BuildReadContext(options);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value!.SnapshotId!.Value.Value);
    }

    [Fact]
    public void MandatoryRoleSelection_PreservesExistingReadContextInTargetUrl()
    {
        var draft = new ContextSelectionDraft();

        var targetUrl = draft.BuildTargetUrl(
            new Uri("https://localhost/knowledge/abc?snapshotId=42"),
            ContextSelectorMode.MandatoryRole,
            "Developer");

        Assert.Equal("/knowledge/abc?roleId=Developer&snapshotId=42", targetUrl);
    }
}
