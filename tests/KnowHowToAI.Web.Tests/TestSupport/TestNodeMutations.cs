using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Web.Tests.TestSupport;

public static class TestNodeMutations
{
    public static NodeMutationApplicationService CreateService(
        InMemoryNodeMutationRepository repository,
        NodeId? fixedNodeId = null,
        ValidationPolicy? policy = null)
    {
        var idGen = fixedNodeId.HasValue
            ? new FixedIdentifierGenerator { FixedNodeId = fixedNodeId.Value }
            : (IIdentifierGenerator)new FixedIdentifierGenerator();

        return new NodeMutationApplicationService(
            repository,
            new NodeMutationService(idGen),
            policy ?? TestPolicies.DefaultValidation);
    }
}
