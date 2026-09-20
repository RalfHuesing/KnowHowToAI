using Bunit;
using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Roles;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Roles;

[Trait("Category", "Unit")]
public sealed class RolesPageTests : BunitContext
{
    private static readonly TransactionId TransactionId = new(Guid.Parse("d6b6c44b-1f9c-4ef1-a8b8-bf3c1d8e2f44"));

    [Fact]
    public void CurrentContext_RendersRolesReadOnly()
    {
        var navigation = CreateNavigationService(out _);
        AddPageServices(navigation, new WebReadContextResolution(
            new ReadContext(),
            new KnowledgeContextViewModel(KnowledgeReadContextKind.Current),
            null));

        var cut = Render<RolesPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Rollen können nur in einer offenen Working Transaction", cut.Markup);
            Assert.DoesNotContain("role-create-form", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void WorkingTransaction_CreateRole_UpdatesStateAndMarksContextDirty()
    {
        var navigation = CreateNavigationService(out var transaction);
        var repository = new InMemoryRoleMutationRepository(new WorkingRoleMutationState(
            transaction.WorkingSnapshotId,
            [new Role(transaction.WorkingSnapshotId, new RoleId("Developer"), "Developer", null, false)],
            [], [], []));
        AddPageServices(navigation, new WebReadContextResolution(
            new ReadContext(TransactionId: TransactionId),
            new KnowledgeContextViewModel(
                KnowledgeReadContextKind.Transaction,
                ContextId: TransactionId.Value.ToString("D"),
                ChangeVersion: 0),
            0));
        Services.AddSingleton(new RoleMutationService(repository));

        var cut = Render<RolesPage>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='role-create-name']")));
        cut.Find("[data-testid='role-create-name']").Change("Consultant");
        cut.Find("[data-testid='role-create-description']").Change("Beratung");
        cut.Find("[data-testid='role-create-submit']").Click();

        Assert.Contains(repository.State.Roles, role => role.RoleId == new RoleId("Consultant"));
        Assert.Equal(1, repository.ChangeVersion);
        Assert.True(Services.GetRequiredService<WorkspaceState>().IsDirty);
        Assert.Contains("role-create-form", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WorkingTransaction_StaleWrite_RendersStableServerError()
    {
        var navigation = CreateNavigationService(out var transaction);
        var repository = new InMemoryRoleMutationRepository(new WorkingRoleMutationState(
            transaction.WorkingSnapshotId,
            [new Role(transaction.WorkingSnapshotId, new RoleId("Developer"), "Developer", null, false)],
            [], [], []));
        var service = new RoleMutationService(repository);
        _ = await service.CreateRoleAsync(TransactionId, "AlreadyChanged", null, 0);
        AddPageServices(navigation, new WebReadContextResolution(
            new ReadContext(TransactionId: TransactionId),
            new KnowledgeContextViewModel(KnowledgeReadContextKind.Transaction, ChangeVersion: 0),
            0));
        Services.AddSingleton(service);

        var cut = Render<RolesPage>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='role-create-name']")));
        cut.Find("[data-testid='role-create-name']").Change("StaleRole");
        cut.Find("[data-testid='role-create-submit']").Click();

        cut.WaitForAssertion(() => Assert.Contains("[ChangeVersionConflict]", cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain(repository.State.Roles, role => role.RoleId == new RoleId("StaleRole"));
        Assert.False(Services.GetRequiredService<WorkspaceState>().IsDirty);
    }

    private NavigationService CreateNavigationService(out KnowledgeTransaction transaction)
    {
        var harness = new NavigationTestHarness(new SnapshotId(1));
        transaction = new KnowledgeTransaction(
            TransactionId,
            new SnapshotId(1),
            new SnapshotId(2),
            TransactionState.Open,
            ChangeVersion: 0,
            DateTimeOffset.UtcNow,
            null,
            "Rollen-Test",
            "Test",
            "Web",
            null);
        harness.SetTransaction(transaction);
        return harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
    }

    private void AddPageServices(NavigationService navigation, WebReadContextResolution resolution)
    {
        var resolver = new StubContextResolver(resolution);
        Services.AddSingleton(new PageRegionState());
        Services.AddSingleton(new WorkspaceState());
        Services.AddSingleton(navigation);
        Services.AddSingleton<IWebReadContextResolver>(resolver);
        Services.AddSingleton(new RoleMutationService(new InMemoryRoleMutationRepository(
            new WorkingRoleMutationState(new SnapshotId(2), [], [], [], []))));
    }

    private sealed class StubContextResolver(WebReadContextResolution resolution) : IWebReadContextResolver
    {
        public Task<Result<WebReadContextResolution>> ResolveAsync(
            string? transactionIdRaw,
            string? snapshotIdRaw,
            string? releaseIdRaw,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<WebReadContextResolution>.Success(resolution));
    }
}
