using Bunit;
using KnowHowToAI.Core.Application.Mutations.Audiences;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Audiences;
using KnowHowToAI.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Audiences;

[Trait("Category", "Unit")]
public sealed class AudienceEditorTests : BunitContext
{
    private static readonly TransactionId TransactionId = new(Guid.Parse("d6b6c44b-1f9c-4ef1-a8b8-bf3c1d8e2f44"));

    [Fact]
    public void CurrentContext_RendersAudiencesReadOnly()
    {
        var navigation = CreateNavigationService(out _);
        AddEditorServices(navigation, new WorkingAudienceMutationState(new SnapshotId(2), [], [], [], []));

        var cut = Render<AudienceEditor>(parameters => parameters
            .Add(component => component.ReadContext, new ReadContext())
            .Add(component => component.ChangeVersion, null));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Zielgruppen können nur in einer offenen Working Transaction", cut.Markup);
            Assert.DoesNotContain("audience-create-form", cut.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void WorkingTransaction_LoadsAllAudiencePages()
    {
        var harness = new NavigationTestHarness(new SnapshotId(1));
        var transaction = CreateTransaction();
        harness.SetTransaction(transaction);
        harness.ClearAudiences(transaction.WorkingSnapshotId);
        harness.AddAudience(new Audience(transaction.WorkingSnapshotId, new AudienceId("R1"), "Audience 1", null, false));
        harness.AddAudience(new Audience(transaction.WorkingSnapshotId, new AudienceId("R2"), "Audience 2", null, false));
        harness.AddAudience(new Audience(transaction.WorkingSnapshotId, new AudienceId("R3"), "Audience 3", null, false));
        var navigation = harness.CreateService(defaultPageSize: 1, maximumPageSize: 1);
        AddEditorServices(navigation, new WorkingAudienceMutationState(transaction.WorkingSnapshotId, [], [], [], []));

        var cut = Render<AudienceEditor>(parameters => parameters
            .Add(component => component.ReadContext, new ReadContext(TransactionId: TransactionId))
            .Add(component => component.ChangeVersion, 0));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Zielgruppen (3)", cut.Markup);
            Assert.NotEmpty(cut.FindAll("[data-testid='audience-item-R1']"));
            Assert.NotEmpty(cut.FindAll("[data-testid='audience-item-R3']"));
        });
    }

    [Fact]
    public void WorkingTransaction_CreateAudience_ProjectsMutationAndNotifiesPage()
    {
        var navigation = CreateNavigationService(out var transaction);
        var repository = CreateAudienceRepository(transaction, new Audience(transaction.WorkingSnapshotId, new AudienceId("Developer"), "Developer", null, false));
        AddEditorServices(navigation, repository.State);
        Services.AddSingleton(new AudienceMutationService(repository));
        long? changedVersion = null;

        var cut = Render<AudienceEditor>(parameters => parameters
            .Add(component => component.ReadContext, new ReadContext(TransactionId: TransactionId))
            .Add(component => component.ChangeVersion, 0)
            .Add(component => component.MutationSucceeded, EventCallback.Factory.Create<long>(this, version => changedVersion = version)));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='audience-create-name']")));

        cut.Find("[data-testid='audience-create-name']").Change("Consultant");
        cut.Find("[data-testid='audience-create-submit']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Consultant", cut.Markup));
        Assert.Equal(1, repository.ChangeVersion);
        Assert.Equal(1, changedVersion);
        Assert.Contains(repository.State.Audiences, audience => audience.AudienceId == new AudienceId("Consultant"));
    }

    [Fact]
    public void WorkingTransaction_RenameAudience_ProjectsMutation()
    {
        var navigation = CreateNavigationService(out var transaction);
        var developer = new Audience(transaction.WorkingSnapshotId, new AudienceId("Developer"), "Developer", null, false);
        var repository = CreateAudienceRepository(transaction, developer);
        AddEditorServices(navigation, repository.State);
        Services.AddSingleton(new AudienceMutationService(repository));

        var cut = RenderEditor();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='audience-edit-Developer']")));
        cut.Find("[data-testid='audience-edit-Developer']").Click();
        cut.Find("[data-testid='audience-edit-name-Developer']").Change("Renamed");
        cut.Find("[data-testid='audience-save-Developer']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Renamed", cut.Markup));
        Assert.Equal("Renamed", repository.State.Audiences.Single(audience => audience.AudienceId == new AudienceId("Developer")).Name);
    }

    [Fact]
    public void WorkingTransaction_DeleteAudience_ProjectsMutation()
    {
        var navigation = CreateNavigationService(out var transaction);
        var developer = new Audience(transaction.WorkingSnapshotId, new AudienceId("Developer"), "Developer", null, false);
        var repository = CreateAudienceRepository(transaction, developer);
        AddEditorServices(navigation, repository.State);
        Services.AddSingleton(new AudienceMutationService(repository));

        var cut = RenderEditor();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='audience-delete-Developer']")));
        cut.Find("[data-testid='audience-delete-Developer']").Click();
        cut.Find("[data-testid='audience-delete-confirm']").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid='audience-item-Developer']")));
        Assert.True(repository.State.Audiences.Single(audience => audience.AudienceId == new AudienceId("Developer")).IsDeleted);
    }

    [Fact]
    public async Task WorkingTransaction_StaleWrite_RendersStableServerErrorAndKeepsInput()
    {
        var navigation = CreateNavigationService(out var transaction);
        var repository = CreateAudienceRepository(transaction, new Audience(transaction.WorkingSnapshotId, new AudienceId("Developer"), "Developer", null, false));
        var service = new AudienceMutationService(repository);
        _ = await service.CreateAudienceAsync(TransactionId, "AlreadyChanged", null, 0);
        AddEditorServices(navigation, repository.State);
        Services.AddSingleton(service);

        var cut = RenderEditor();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='audience-create-name']")));
        cut.Find("[data-testid='audience-create-name']").Change("StaleAudience");
        cut.Find("[data-testid='audience-create-submit']").Click();

        cut.WaitForAssertion(() => Assert.Contains("[ChangeVersionConflict]", cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain(repository.State.Audiences, audience => audience.AudienceId == new AudienceId("StaleAudience"));
        Assert.Contains("StaleAudience", cut.Markup, StringComparison.Ordinal);
    }

    private IRenderedComponent<AudienceEditor> RenderEditor() =>
        Render<AudienceEditor>(parameters => parameters
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
        "Zielgruppen-Test",
        "Test",
        "Web",
        null);

    private static InMemoryAudienceMutationRepository CreateAudienceRepository(
        KnowledgeTransaction transaction,
        Audience audience) =>
        new(new WorkingAudienceMutationState(transaction.WorkingSnapshotId, [audience], [], [], []));

    private void AddEditorServices(NavigationService navigation, WorkingAudienceMutationState state)
    {
        Services.AddSingleton(navigation);
        Services.AddSingleton(new AudienceMutationService(new InMemoryAudienceMutationRepository(state)));
    }
}
