using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Dashboard;

[Trait("Category", "Unit")]
public sealed class DashboardPageTests : Bunit.BunitContext
{
    [Fact]
    public void RendersSemanticHeadingAndTechnicalShellStatus()
    {
        Services.AddSingleton(CreateNavigationService([CreateRole("Developer")]));

        var cut = Render<DashboardPage>();

        cut.Find("h1").MarkupMatches("<h1>KnowHowToAI</h1>");
        Assert.Contains("Shell bereit.", cut.Find("[data-testid=shell-status]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void DisplaysEmptyStatusWhenTheReadOnlyRoleQueryReturnsNoRoles()
    {
        Services.AddSingleton(CreateNavigationService([]));

        var cut = Render<DashboardPage>();

        Assert.Contains("keine Rollen", cut.Find("[data-testid=shell-status]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ChangesOnlyTheLocalInteractionStatusAfterTheButtonIsClicked()
    {
        Services.AddSingleton(CreateNavigationService([CreateRole("Developer")]));
        var cut = Render<DashboardPage>();

        cut.Find("button").Click();

        Assert.Contains("Interaktivität ist verfügbar.", cut.Find("[data-testid=interaction-status]").TextContent, StringComparison.Ordinal);
    }

    private static NavigationService CreateNavigationService(IReadOnlyList<Role> roles)
    {
        var snapshotId = new SnapshotId(1);
        var repositories = new SnapshotReadRepositories(
            new SnapshotRepository(snapshotId),
            new TransactionRepository(),
            new HierarchyRepository(),
            new ContentRepository(),
            new RoleRepository(roles),
            new DependencyRepository());

        return new NavigationService(repositories, new RetrievalPolicy
        {
            DefaultPageSize = 1,
            MaximumPageSize = 1,
            SearchPageSize = 1,
            SearchMaximumPageSize = 1,
            SnippetMaximumCharacters = 50
        });
    }

    private static Role CreateRole(string roleId) => new(new SnapshotId(1), new RoleId(roleId), roleId, null, false);

    private sealed class SnapshotRepository(SnapshotId snapshotId) : ISnapshotRepository
    {
        public Task<Snapshot?> FindAsync(SnapshotId value, CancellationToken cancellationToken = default) => Task.FromResult<Snapshot?>(null);
        public Task<Snapshot> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new Snapshot(snapshotId, null, SnapshotState.Committed, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch));
    }

    private sealed class TransactionRepository : ITransactionRepository
    {
        public Task<KnowledgeTransaction> BeginAsync(BeginTransactionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) => Task.FromResult<KnowledgeTransaction?>(null);
        public Task<CommitTransactionResult> CommitAsync(CommitTransactionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class HierarchyRepository : IHierarchyRepository
    {
        public Task<IReadOnlyList<Node>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Node>>([]);
    }

    private sealed class ContentRepository : IContentRepository
    {
        public Task<IReadOnlyList<NodeContent>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<NodeContent>>([]);
    }

    private sealed class RoleRepository(IReadOnlyList<Role> roles) : IRoleRepository
    {
        public Task<IReadOnlyList<Role>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) => Task.FromResult(roles);
        public Task<IReadOnlyList<RoleResolution>> ListResolutionsBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RoleResolution>>([]);
    }

    private sealed class DependencyRepository : IDependencyRepository
    {
        public Task<IReadOnlyList<ContentDependency>> ListBySnapshotAsync(SnapshotId snapshotId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContentDependency>>([]);
    }
}
