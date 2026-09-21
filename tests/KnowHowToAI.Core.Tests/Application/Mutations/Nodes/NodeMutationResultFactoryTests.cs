using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.Mutations.Nodes;

[Trait("Category", "Unit")]
public sealed class NodeMutationResultFactoryTests
{
    [Fact]
    public void Create_ReportsChangedNodeAndAffectedIds()
    {
        var snapshotId = new SnapshotId(1);
        var nodeId = new NodeId(Guid.Parse("30000000-0000-0000-0000-000000000001"));
        var previousNode = new Node(snapshotId, nodeId, null, "Alt", null, 0, false);
        var currentNode = previousNode with { Title = "Neu" };
        var previousState = new WorkingNodeMutationState(snapshotId, [previousNode], [], [], [nodeId]);
        var currentState = new WorkingNodeMutationState(snapshotId, [currentNode], [], [], [nodeId]);
        var execution = new WorkingNodeMutationExecution<HierarchyMutationResult>(
            new HierarchyMutationResult(currentNode, [currentNode]),
            snapshotId,
            3,
            previousState,
            currentState);

        var result = new NodeMutationResultFactory(TestPolicies.DefaultValidation).Create(execution.Value, execution);

        Assert.True(result.IsSuccess);
        Assert.Equal("Neu", result.Value!.Node.Title);
        Assert.Contains(nodeId, result.Value.AffectedNodeIds);
        Assert.Equal(3, result.Value.ChangeVersion);
    }
}
