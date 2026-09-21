using Bunit;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Components.Layout;

[Trait("Category", "Unit")]
public sealed class ContextSelectionFormTests : BunitContext
{
    [Fact]
    public async Task OverlappingAudienceLoads_KeepOnlyTheCurrentContextResult()
    {
        var selectorState = new ContextSelectorState();
        selectorState.Open(ContextSelectorMode.Full);
        var catalog = new DeferredAudienceCatalog();

        Services.AddSingleton(selectorState);
        Services.AddSingleton(new WorkspaceState());
        Services.AddSingleton<IAudienceStorageService>(new InMemoryAudienceStorageService());
        Services.AddSingleton<IContextSelectionCatalog>(new FixedContextSelectionCatalog());
        Services.AddSingleton<IContextSelectionAudienceCatalog>(catalog);

        var cut = Render<ContextSelectionForm>();
        cut.WaitForAssertion(() => Assert.Equal(1, catalog.Requests.Count));

        var contextId = Guid.Parse("00000000-0000-0000-0000-000000000201");
        await cut.InvokeAsync(() => cut.Find("input[type='radio'][value='Transaction']").Change(true));
        await cut.InvokeAsync(() => cut.Find("[data-testid='transaction-select']").Change(contextId.ToString("D")));
        cut.WaitForAssertion(() => Assert.Equal(2, catalog.Requests.Count));
        Assert.True(cut.Find("[data-testid='selector-apply-button']").HasAttribute("disabled"));

        await cut.InvokeAsync(() => catalog.Complete(1, "new-context"));
        cut.WaitForAssertion(() => Assert.True(cut.Markup.Contains("new-context", StringComparison.Ordinal), cut.Markup));

        await cut.InvokeAsync(() => catalog.Complete(0, "old-context"));
        cut.WaitForAssertion(() => Assert.Contains("new-context", cut.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("old-context", cut.Markup, StringComparison.Ordinal);
    }

    private sealed class FixedContextSelectionCatalog : IContextSelectionCatalog
    {
        public Task<ContextSelectionOptionsViewModel> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ContextSelectionOptionsViewModel(
                [],
                [new ContextSelectionTransactionOptionViewModel(
                    "00000000-0000-0000-0000-000000000201",
                    "Testtransaktion")],
                []));
    }

    private sealed class DeferredAudienceCatalog : IContextSelectionAudienceCatalog
    {
        private readonly List<TaskCompletionSource<ContextSelectionAudienceLoadResult>> _pending = [];

        public List<ReadContext> Requests { get; } = [];

        public Task<ContextSelectionAudienceLoadResult> LoadAsync(
            ReadContext readContext,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(readContext);
            var pending = new TaskCompletionSource<ContextSelectionAudienceLoadResult>();
            _pending.Add(pending);
            return pending.Task;
        }

        public void Complete(int requestIndex, string audienceId) =>
            _pending[requestIndex].SetResult(new ContextSelectionAudienceLoadResult(
                [new ContextSelectionAudienceOptionViewModel(audienceId, audienceId, null)],
                null));
    }
}
