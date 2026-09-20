using Bunit;
using KnowHowToAI.Core.Application.Mutations.Audiences;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
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
    public async Task WorkingTransaction_MutationEvent_UpdatesWorkspaceContext()
    {
        var navigation = CreateNavigationService(out _);
        AddPageServices(navigation, new WebReadContextResolution(
            new ReadContext(TransactionId: TransactionId),
            new KnowledgeContextViewModel(
                KnowledgeReadContextKind.Transaction,
                ContextId: TransactionId.Value.ToString("D"),
                ChangeVersion: 0),
            0));

        var cut = Render<RolesPage>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindComponents<RoleEditor>()));
        var editor = cut.FindComponent<RoleEditor>();

        await cut.InvokeAsync(() => editor.Instance.MutationSucceeded.InvokeAsync(1));

        var workspace = Services.GetRequiredService<WorkspaceState>();
        Assert.Equal(1, workspace.CurrentChangeVersion);
        Assert.True(workspace.IsDirty);
        Assert.Equal(1, Services.GetRequiredService<PageRegionState>().KnowledgeContext!.ChangeVersion);
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
        Services.AddSingleton(new AudienceMutationService(new InMemoryAudienceMutationRepository(
            new WorkingAudienceMutationState(new SnapshotId(2), [], [], [], []))));
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
