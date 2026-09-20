using Bunit;
using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Roles;
using KnowHowToAI.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Roles;

[Trait("Category", "Unit")]
public sealed class RoleEditorTests : BunitContext
{
    private static readonly TransactionId TransactionId = new(Guid.Parse("d6b6c44b-1f9c-4ef1-a8b8-bf3c1d8e2f44"));

    [Fact]
    public void CurrentContext_RendersRolesReadOnly()
    {
        var navigation = CreateNavigationService(out _);
        AddEditorServices(navigation, new WorkingRoleMutationState(new SnapshotId(2), [], [], [], []));

        var cut = Render<RoleEditor>(parameters => parameters
            .Add(component => component.ReadContext, new ReadContext())
            .Add(component => component.ChangeVersion, null));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Rollen können nur in einer offenen Working Transaction", cut.Markup);
            Assert.DoesNotContain("role-create-form", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void WorkingTransaction_LoadsAllRolePages()
    {
        var harness = new NavigationTestHarness(new SnapshotId(1));
        var transaction = CreateTransaction();
        harness.SetTransaction(transaction);
        harness.ClearRoles(transaction.WorkingSnapshotId);
        harness.AddRole(new Role(transaction.WorkingSnapshotId, new RoleId("R1"), "Role 1", null, false));
        harness.AddRole(new Role(transaction.WorkingSnapshotId, new RoleId("R2"), "Role 2", null, false));
        harness.AddRole(new Role(transaction.WorkingSnapshotId, new RoleId("R3"), "Role 3", null, false));
        var navigation = harness.CreateService(defaultPageSize: 1, maximumPageSize: 1);
        AddEditorServices(navigation, new WorkingRoleMutationState(transaction.WorkingSnapshotId, [], [], [], []));

        var cut = Render<RoleEditor>(parameters => parameters
            .Add(component => component.ReadContext, new ReadContext(TransactionId: TransactionId))
            .Add(component => component.ChangeVersion, 0));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Rollen (3)", cut.Markup);
            Assert.NotEmpty(cut.FindAll("[data-testid='role-item-R1']"));
            Assert.NotEmpty(cut.FindAll("[data-testid='role-item-R3']"));
        });
    }

    [Fact]
    public void WorkingTransaction_CreateRole_ProjectsMutationAndNotifiesPage()
    {
        var navigation = CreateNavigationService(out var transaction);
        var repository = CreateRoleRepository(transaction, new Role(transaction.WorkingSnapshotId, new RoleId("Developer"), "Developer", null, false));
        AddEditorServices(navigation, repository.State);
        Services.AddSingleton(new RoleMutationService(repository));
        long? changedVersion = null;

        var cut = Render<RoleEditor>(parameters => parameters
            .Add(component => component.ReadContext, new ReadContext(TransactionId: TransactionId))
            .Add(component => component.ChangeVersion, 0)
            .Add(component => component.MutationSucceeded, EventCallback.Factory.Create<long>(this, version => changedVersion = version)));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='role-create-name']")));

        cut.Find("[data-testid='role-create-name']").Change("Consultant");
        cut.Find("[data-testid='role-create-submit']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Consultant", cut.Markup));
        Assert.Equal(1, repository.ChangeVersion);
        Assert.Equal(1, changedVersion);
        Assert.Contains(repository.State.Roles, role => role.RoleId == new RoleId("Consultant"));
    }

    [Fact]
    public void WorkingTransaction_RenameRole_ProjectsMutation()
    {
        var navigation = CreateNavigationService(out var transaction);
        var developer = new Role(transaction.WorkingSnapshotId, new RoleId("Developer"), "Developer", null, false);
        var repository = CreateRoleRepository(transaction, developer);
        AddEditorServices(navigation, repository.State);
        Services.AddSingleton(new RoleMutationService(repository));

        var cut = RenderEditor();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='role-edit-Developer']")));
        cut.Find("[data-testid='role-edit-Developer']").Click();
        cut.Find("[data-testid='role-edit-name-Developer']").Change("Renamed");
        cut.Find("[data-testid='role-save-Developer']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Renamed", cut.Markup));
        Assert.Equal("Renamed", repository.State.Roles.Single(role => role.RoleId == new RoleId("Developer")).Name);
    }

    [Fact]
    public void WorkingTransaction_DeleteRole_ProjectsMutation()
    {
        var navigation = CreateNavigationService(out var transaction);
        var developer = new Role(transaction.WorkingSnapshotId, new RoleId("Developer"), "Developer", null, false);
        var repository = CreateRoleRepository(transaction, developer);
        AddEditorServices(navigation, repository.State);
        Services.AddSingleton(new RoleMutationService(repository));

        var cut = RenderEditor();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='role-delete-Developer']")));
        cut.Find("[data-testid='role-delete-Developer']").Click();
        cut.Find("[data-testid='role-delete-confirm']").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid='role-item-Developer']")));
        Assert.True(repository.State.Roles.Single(role => role.RoleId == new RoleId("Developer")).IsDeleted);
    }

    [Fact]
    public async Task WorkingTransaction_StaleWrite_RendersStableServerErrorAndKeepsInput()
    {
        var navigation = CreateNavigationService(out var transaction);
        var repository = CreateRoleRepository(transaction, new Role(transaction.WorkingSnapshotId, new RoleId("Developer"), "Developer", null, false));
        var service = new RoleMutationService(repository);
        _ = await service.CreateRoleAsync(TransactionId, "AlreadyChanged", null, 0);
        AddEditorServices(navigation, repository.State);
        Services.AddSingleton(service);

        var cut = RenderEditor();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='role-create-name']")));
        cut.Find("[data-testid='role-create-name']").Change("StaleRole");
        cut.Find("[data-testid='role-create-submit']").Click();

        cut.WaitForAssertion(() => Assert.Contains("[ChangeVersionConflict]", cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain(repository.State.Roles, role => role.RoleId == new RoleId("StaleRole"));
        Assert.Contains("StaleRole", cut.Markup, StringComparison.Ordinal);
    }

    private IRenderedComponent<RoleEditor> RenderEditor() =>
        Render<RoleEditor>(parameters => parameters
            .Add(component => component.ReadContext, new ReadContext(TransactionId: TransactionId))
            .Add(component => component.ChangeVersion, 0));

    private NavigationService CreateNavigationService(out KnowledgeTransaction transaction)
    {
        var harness = new NavigationTestHarness(new SnapshotId(1));
        transaction = CreateTransaction();
        harness.SetTransaction(transaction);
        return harness.CreateService(defaultPageSize: 100, maximumPageSize: 100);
    }

    private static KnowledgeTransaction CreateTransaction() => new(
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

    private static InMemoryRoleMutationRepository CreateRoleRepository(
        KnowledgeTransaction transaction,
        Role role) =>
        new(new WorkingRoleMutationState(transaction.WorkingSnapshotId, [role], [], [], []));

    private void AddEditorServices(NavigationService navigation, WorkingRoleMutationState state)
    {
        Services.AddSingleton(navigation);
        Services.AddSingleton(new RoleMutationService(new InMemoryRoleMutationRepository(state)));
    }
}
