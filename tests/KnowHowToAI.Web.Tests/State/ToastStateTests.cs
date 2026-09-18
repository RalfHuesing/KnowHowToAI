using KnowHowToAI.Server.Web.State;

namespace KnowHowToAI.Web.Tests.State;

[Trait("Category", "Unit")]
public sealed class ToastStateTests
{
    [Fact]
    public void ShowAddsAnEntryAndNotifiesExactlyOnce()
    {
        var state = new ToastState();
        var changeCount = 0;
        state.Changed += () => changeCount++;

        state.Show("Die Änderung wurde gespeichert.");

        var entry = Assert.Single(state.Entries);
        Assert.Equal("Die Änderung wurde gespeichert.", entry.Message);
        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(1, changeCount);
    }

    [Fact]
    public void DismissRemovesOnlyTheMatchingEntryAndNotifiesExactlyOnce()
    {
        var state = new ToastState();
        state.Show("Die Änderung wurde gespeichert.");
        state.Show("Der Snapshot wurde verglichen.");
        var dismissedId = state.Entries[0].Id;
        var changeCount = 0;
        state.Changed += () => changeCount++;

        state.Dismiss(dismissedId);

        var remaining = Assert.Single(state.Entries);
        Assert.Equal("Der Snapshot wurde verglichen.", remaining.Message);
        Assert.Equal(1, changeCount);
    }

    [Fact]
    public void DismissOfAnUnknownEntryChangesNothing()
    {
        var state = new ToastState();
        state.Show("Die Änderung wurde gespeichert.");
        var changeCount = 0;
        state.Changed += () => changeCount++;

        state.Dismiss(Guid.NewGuid());

        Assert.Single(state.Entries);
        Assert.Equal(0, changeCount);
    }
}
