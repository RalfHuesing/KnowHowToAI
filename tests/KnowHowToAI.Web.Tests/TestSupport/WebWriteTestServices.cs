using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Workflow;
using KnowHowToAI.TestSupport;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Web.Tests.TestSupport;

internal static class WebWriteTestServices
{
    public static WebWriteCoordinator CreateCoordinator(
        NavigationTestHarness harness,
        WorkspaceState workspace,
        NavigationManager navigationManager)
    {
        var repositories = harness.CreateRepositories();
        var transactions = new TransactionService(
            repositories.Transactions,
            new InMemoryWorkingSnapshotValidationDataRepository(),
            new FixedIdentifierGenerator { FixedTransactionId = new(Guid.Parse("33333333-3333-3333-3333-333333333333")) },
            TestPolicies.DefaultValidation);

        return new WebWriteCoordinator(
            transactions,
            repositories.Snapshots,
            new TestCurrentUserService(),
            workspace,
            navigationManager);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public CurrentUser GetCurrentUser() => new("web-tests", "Web-Tests");
    }
}
