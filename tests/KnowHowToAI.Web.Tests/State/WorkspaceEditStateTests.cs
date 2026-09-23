using KnowHowToAI.Server.Web.State;

namespace KnowHowToAI.Web.Tests.State;

[Trait("Category", "Unit")]
public sealed class WorkspaceEditStateTests
{
    [Fact]
    public void DirtySourcesRemainDirtyUntilEveryEditorHasSaved()
    {
        var state = new WorkspaceEditState();

        Assert.True(state.SetDirty(true, "content:node-a"));
        Assert.False(state.SetDirty(true, "metadata:node-a"));
        Assert.True(state.IsDirty);

        Assert.False(state.SetDirty(false, "content:node-a"));
        Assert.True(state.IsDirty);
        Assert.True(state.SetDirty(false, "metadata:node-a"));
        Assert.False(state.IsDirty);
    }
}
