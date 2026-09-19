using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.State;

namespace KnowHowToAI.Web.Tests.State;

[Trait("Category", "Unit")]
public sealed class WorkspaceStateTests
{
    [Fact]
    public void InitialState_IsCurrentWithoutNodeOrRole()
    {
        var state = new WorkspaceState();

        Assert.Null(state.CurrentNodeId);
        Assert.Null(state.CurrentRoleId);
        Assert.Null(state.CurrentChangeVersion);
        Assert.Equal(KnowledgeReadContextKind.Current, state.CurrentContext.ReadContext);
        Assert.Null(state.CurrentReadContext.SnapshotId);
        Assert.Null(state.CurrentReadContext.TransactionId);
    }

    [Fact]
    public void SetNode_UpdatesPropertyAndFiresChangedOnlyWhenDifferent()
    {
        var state = new WorkspaceState();
        var changeCount = 0;
        state.Changed += () => changeCount++;

        var nodeId = Guid.NewGuid();
        state.SetNode(nodeId);

        Assert.Equal(nodeId, state.CurrentNodeId);
        Assert.Equal(1, changeCount);

        // Gleicher Wert darf kein erneutes Event auslösen
        state.SetNode(nodeId);
        Assert.Equal(1, changeCount);

        state.SetNode(null);
        Assert.Null(state.CurrentNodeId);
        Assert.Equal(2, changeCount);
    }

    [Fact]
    public void SetRole_UpdatesPropertyAndFiresChangedOnlyWhenDifferent()
    {
        var state = new WorkspaceState();
        var changeCount = 0;
        state.Changed += () => changeCount++;

        var roleId = "architect";
        state.SetRole(roleId);

        Assert.Equal(roleId, state.CurrentRoleId);
        Assert.Equal(1, changeCount);

        state.SetRole(roleId);
        Assert.Equal(1, changeCount);
    }

    [Fact]
    public void SetChangeVersion_UpdatesPropertyAndFiresChangedOnlyWhenDifferent()
    {
        var state = new WorkspaceState();
        var changeCount = 0;
        state.Changed += () => changeCount++;

        state.SetChangeVersion(123L);
        Assert.Equal(123L, state.CurrentChangeVersion);
        Assert.Equal(1, changeCount);

        state.SetChangeVersion(123L);
        Assert.Equal(1, changeCount);
    }

    [Fact]
    public void SetContext_UpdatesBothContextsAndFiresChanged()
    {
        var state = new WorkspaceState();
        var changeCount = 0;
        state.Changed += () => changeCount++;

        var snapId = new SnapshotId(42);
        var readContext = new ReadContext(SnapshotId: snapId);
        var contextVm = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Snapshot,
            ContextId: "42",
            DisplayName: "Snapshot 42");

        state.SetContext(contextVm, readContext);

        Assert.Equal(contextVm, state.CurrentContext);
        Assert.Equal(readContext, state.CurrentReadContext);
        Assert.Equal(1, changeCount);

        state.SetContext(contextVm, readContext);
        Assert.Equal(1, changeCount);
    }

    [Fact]
    public void Reset_ClearsAllValuesAndRestoresDefaults()
    {
        var state = new WorkspaceState();
        state.SetNode(Guid.NewGuid());
        state.SetRole("architect");
        state.SetChangeVersion(10L);
        state.SetContext(
            new KnowledgeContextViewModel(KnowledgeReadContextKind.Transaction, ContextId: "tx-1"),
            new ReadContext(TransactionId: new TransactionId(Guid.NewGuid())));

        var changeCount = 0;
        state.Changed += () => changeCount++;

        state.Reset();

        Assert.Null(state.CurrentNodeId);
        Assert.Null(state.CurrentRoleId);
        Assert.Null(state.CurrentChangeVersion);
        Assert.Equal(KnowledgeReadContextKind.Current, state.CurrentContext.ReadContext);
        Assert.Null(state.CurrentReadContext.TransactionId);
        Assert.Equal(1, changeCount);
    }

    [Fact]
    public void SetDirty_UpdatesPropertyAndFiresChangedOnlyWhenDifferent()
    {
        var state = new WorkspaceState();
        var changeCount = 0;
        state.Changed += () => changeCount++;

        state.SetDirty(true);
        Assert.True(state.IsDirty);
        Assert.Equal(1, changeCount);

        // Gleicher Wert darf kein neues Event auslösen
        state.SetDirty(true);
        Assert.Equal(1, changeCount);

        state.SetDirty(false);
        Assert.False(state.IsDirty);
        Assert.Equal(2, changeCount);
    }

    [Fact]
    public void Properties_ReflectTransactionAndBaseSnapshot()
    {
        var state = new WorkspaceState();
        var txId = new TransactionId(Guid.NewGuid());

        Assert.False(state.HasActiveTransaction);
        Assert.Null(state.ActiveTransactionId);
        Assert.Null(state.CurrentContext.BaseSnapshotId);

        var contextVm = new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Transaction,
            ContextId: txId.Value.ToString("D"),
            BaseSnapshotId: 42L);

        state.SetContext(contextVm, new ReadContext(TransactionId: txId));

        Assert.True(state.HasActiveTransaction);
        Assert.Equal(txId, state.ActiveTransactionId);
        Assert.Equal(42L, state.CurrentContext.BaseSnapshotId);
    }
}
