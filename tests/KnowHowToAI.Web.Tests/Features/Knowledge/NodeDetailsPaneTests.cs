using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.Features.Knowledge.Components;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using KnowHowToAI.Server.Web.Features.Knowledge.Node;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class NodeDetailsPaneTests : BunitContext
{
    [Fact]
    public void NodeDetailsPane_LoadsSelectedNodeAndBuildsContextPreservingDownloadUrl()
    {
        var snapshotId = new SnapshotId(1);
        var nodeId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000010"));
        var harness = new NavigationTestHarness(snapshotId);
        harness.AddNode(new Node(snapshotId, nodeId, null, "Pane-Knoten", "Details", 0, false));
        harness.AddContent(new NodeContent(
            snapshotId,
            nodeId,
            new AudienceId("Developer"),
            new ContentRevisionId(Guid.Parse("10000000-0000-0000-0000-000000000011")),
            ContentMode.Independent,
            "Pane-Inhalt",
            false));
        Services.AddSingleton(harness.CreateService());

        var cut = Render<NodeDetailsPane>(parameters => parameters
            .Add(pane => pane.NodeId, nodeId.Value)
            .Add(pane => pane.ReadContext, new ReadContext())
            .Add(pane => pane.AudienceId, "Developer")
            .Add(pane => pane.QuerySnapshotId, snapshotId.Value.ToString()));

        Assert.Equal("Pane-Knoten", cut.Find("[data-testid='node-details-title']").TextContent.Trim());
        var downloadUrl = cut.Find("[data-testid='node-details-markdown-download']").GetAttribute("href");
        Assert.Contains($"nodeId={nodeId.Value:D}", downloadUrl, StringComparison.Ordinal);
        Assert.Contains("audienceId=Developer", downloadUrl, StringComparison.Ordinal);
        Assert.Contains("snapshotId=1", downloadUrl, StringComparison.Ordinal);
    }

    [Fact]
    public void NodeDetailsPane_DerivedContentRemainsReadOnlyInWorkingTransaction()
    {
        var currentSnapshotId = new SnapshotId(1);
        var workingSnapshotId = new SnapshotId(2);
        var transactionId = new TransactionId(Guid.Parse("10000000-0000-0000-0000-000000000012"));
        var nodeId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000013"));
        var harness = new NavigationTestHarness(currentSnapshotId);
        harness.SetTransaction(new KnowledgeTransaction(
            transactionId,
            currentSnapshotId,
            workingSnapshotId,
            TransactionState.Open,
            4,
            DateTimeOffset.UtcNow,
            null,
            "Derived-Test",
            "Test",
            "Web",
            null));
        harness.AddNode(new Node(workingSnapshotId, nodeId, null, "Derived-Knoten", null, 0, false));
        harness.AddContent(new NodeContent(
            workingSnapshotId,
            nodeId,
            new AudienceId("Developer"),
            new ContentRevisionId(Guid.Parse("10000000-0000-0000-0000-000000000014")),
            ContentMode.Derived,
            "Abgeleiteter Inhalt",
            false));

        Services.AddSingleton(harness.CreateService());
        Services.AddSingleton(new WorkspaceState());
        Services.AddSingleton(TestNodeMutations.CreateService(
            new InMemoryNodeMutationRepository(new WorkingNodeMutationState(workingSnapshotId, [], [], [], []))));
        Services.AddSingleton(new NodeDeletionApplicationService(
            harness.CreateRepositories().WorkingSnapshots!,
            new InMemoryNodeMutationRepository(new WorkingNodeMutationState(workingSnapshotId, [], [], [], [])),
            new NodeMutationService(new FixedIdentifierGenerator()),
            TestPolicies.DefaultValidation));
        JSInterop.SetupAppDialog();

        var cut = Render<NodeDetailsPane>(parameters => parameters
            .Add(pane => pane.NodeId, nodeId.Value)
            .Add(pane => pane.ReadContext, new ReadContext(TransactionId: transactionId))
            .Add(pane => pane.AudienceId, "Developer")
            .Add(pane => pane.TransactionId, transactionId)
            .Add(pane => pane.ChangeVersion, 4L));

        Assert.Empty(cut.FindAll("[data-testid='content-editor']"));
        Assert.Contains("Abgeleiteter Inhalt", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid='content-editor-save']"));
        Assert.Contains("Abgeleiteter Inhalt", cut.Find("[data-testid='node-content-derived-context']").TextContent);
    }

    [Fact]
    public void NodeDetailsPane_ExplicitIndependentCurrent_ShowsEditEntryAndCompatibleWorkingCopy()
    {
        var currentSnapshotId = new SnapshotId(1);
        var workingSnapshotId = new SnapshotId(2);
        var transactionId = new TransactionId(Guid.Parse("10000000-0000-0000-0000-000000000020"));
        var incompatibleTransactionId = new TransactionId(Guid.Parse("10000000-0000-0000-0000-000000000023"));
        var nodeId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000021"));
        var harness = new NavigationTestHarness(currentSnapshotId);
        harness.SetTransaction(new KnowledgeTransaction(
            transactionId,
            currentSnapshotId,
            workingSnapshotId,
            TransactionState.Open,
            3,
            DateTimeOffset.UtcNow,
            null,
            "Glossar-Arbeitskopie",
            "Test",
            "Web",
            null));
        harness.SetTransaction(new KnowledgeTransaction(
            incompatibleTransactionId,
            currentSnapshotId,
            new SnapshotId(3),
            TransactionState.Open,
            1,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            null,
            "Abgeleitete Arbeitskopie",
            "Test",
            "Web",
            null));
        AddIndependentContent(harness, currentSnapshotId, nodeId, "Current-Inhalt");
        AddIndependentContent(harness, workingSnapshotId, nodeId, "Working-Inhalt");
        harness.AddNode(new Node(new SnapshotId(3), nodeId, null, "Bearbeitbarer Knoten", null, 0, false));
        harness.AddContent(new NodeContent(
            new SnapshotId(3),
            nodeId,
            new AudienceId("Developer"),
            new ContentRevisionId(Guid.Parse("10000000-0000-0000-0000-000000000024")),
            ContentMode.Derived,
            "Abgeleiteter Working-Inhalt",
            false));

        var navigation = harness.CreateService();
        Services.AddSingleton(navigation);
        Services.AddSingleton<IContextSelectionAudienceCatalog>(new ContextSelectionAudienceCatalog(navigation));
        Services.AddSingleton(new TransactionService(
            harness.CreateRepositories().Transactions,
            new InMemoryWorkingSnapshotValidationDataRepository(),
            new FixedIdentifierGenerator { FixedTransactionId = new TransactionId(Guid.Parse("10000000-0000-0000-0000-000000000022")) },
            TestPolicies.DefaultValidation));
        Services.AddSingleton<ICurrentUserService>(new TestCurrentUserService());

        var cut = Render<NodeDetailsPane>(parameters => parameters
            .Add(pane => pane.NodeId, nodeId.Value)
            .Add(pane => pane.ReadContext, new ReadContext())
            .Add(pane => pane.AudienceId, "Developer"));

        cut.WaitForElement("[data-testid='node-details-edit']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll("[data-testid='node-edit-dialog']"));
            Assert.Contains("Glossar-Arbeitskopie", cut.Markup);
            Assert.Null(cut.Find("[data-testid='node-edit-working-copy-select-10000000-0000-0000-0000-000000000020']").GetAttribute("disabled"));
            Assert.Contains("nicht Independent", cut.Find("[data-testid='node-edit-working-copy-reason-10000000-0000-0000-0000-000000000023']").TextContent);
            Assert.NotNull(cut.Find("[data-testid='node-edit-working-copy-select-10000000-0000-0000-0000-000000000023']").GetAttribute("disabled"));
        });

        cut.Find("[data-testid='node-edit-working-copy-select-10000000-0000-0000-0000-000000000020']").Click();
        Assert.Contains($"/knowledge/{nodeId.Value:D}?transactionId={transactionId.Value:D}&audienceId=Developer", Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void NodeDetailsPane_BeginFailure_KeepsDialogOpenWithoutNavigation()
    {
        var currentSnapshotId = new SnapshotId(1);
        var nodeId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000030"));
        var harness = CreateEditableCurrentHarness(currentSnapshotId, nodeId);
        var navigation = harness.CreateService();
        Services.AddSingleton(navigation);
        Services.AddSingleton<IContextSelectionAudienceCatalog>(new ContextSelectionAudienceCatalog(navigation));
        Services.AddSingleton(new TransactionService(
            new ControlledTransactionRepository(_ => throw new InvalidOperationException("Begin-Testfehler")),
            new InMemoryWorkingSnapshotValidationDataRepository(),
            new FixedIdentifierGenerator(),
            TestPolicies.DefaultValidation));
        Services.AddSingleton<ICurrentUserService>(new TestCurrentUserService());

        var cut = Render<NodeDetailsPane>(parameters => parameters
            .Add(pane => pane.NodeId, nodeId.Value)
            .Add(pane => pane.ReadContext, new ReadContext())
            .Add(pane => pane.AudienceId, "Developer"));

        cut.WaitForElement("[data-testid='node-details-edit']").Click();
        cut.WaitForElement("[data-testid='node-edit-begin-working-copy']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll("[data-testid='node-edit-dialog']"));
            Assert.Contains("Neue Arbeitskopie konnte nicht begonnen werden", cut.Find("[data-testid='node-edit-begin-error']").TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("transactionId=", Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void NodeDetailsPane_BeginSuccess_NavigatesToSameNodeAndAudience()
    {
        var currentSnapshotId = new SnapshotId(1);
        var workingSnapshotId = new SnapshotId(2);
        var transactionId = new TransactionId(Guid.Parse("10000000-0000-0000-0000-000000000031"));
        var nodeId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000032"));
        var transaction = new KnowledgeTransaction(
            transactionId,
            currentSnapshotId,
            workingSnapshotId,
            TransactionState.Open,
            1,
            DateTimeOffset.UtcNow,
            null,
            "Neue Arbeitskopie",
            "TestUser",
            "Web UI",
            null);
        var harness = CreateEditableCurrentHarness(currentSnapshotId, nodeId);
        harness.SetTransaction(transaction);
        harness.AddNode(new Node(workingSnapshotId, nodeId, null, "Bearbeitbarer Knoten", null, 0, false));
        AddIndependentContent(harness, workingSnapshotId, nodeId, "Working-Inhalt");
        var navigation = harness.CreateService();
        Services.AddSingleton(navigation);
        Services.AddSingleton<IContextSelectionAudienceCatalog>(new ContextSelectionAudienceCatalog(navigation));
        Services.AddSingleton(new TransactionService(
            new ControlledTransactionRepository(_ => transaction),
            new InMemoryWorkingSnapshotValidationDataRepository(),
            new FixedIdentifierGenerator { FixedTransactionId = transactionId },
            TestPolicies.DefaultValidation));
        Services.AddSingleton<ICurrentUserService>(new TestCurrentUserService());

        var cut = Render<NodeDetailsPane>(parameters => parameters
            .Add(pane => pane.NodeId, nodeId.Value)
            .Add(pane => pane.ReadContext, new ReadContext())
            .Add(pane => pane.AudienceId, "Developer"));

        cut.WaitForElement("[data-testid='node-details-edit']").Click();
        cut.WaitForElement("[data-testid='node-edit-begin-working-copy']").Click();

        cut.WaitForAssertion(() => Assert.EndsWith(
            $"/knowledge/{nodeId.Value:D}?transactionId={transactionId.Value:D}&audienceId=Developer",
            Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri,
            StringComparison.Ordinal));
    }

    [Fact]
    public void NodeDetailsPane_CurrentRace_KeepsCreatedWorkingCopyVisibleWithDetailsLink()
    {
        var currentSnapshotId = new SnapshotId(1);
        var workingSnapshotId = new SnapshotId(2);
        var transactionId = new TransactionId(Guid.Parse("10000000-0000-0000-0000-000000000033"));
        var nodeId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000034"));
        var transaction = new KnowledgeTransaction(
            transactionId,
            currentSnapshotId,
            workingSnapshotId,
            TransactionState.Open,
            1,
            DateTimeOffset.UtcNow,
            null,
            "Race-Arbeitskopie",
            "TestUser",
            "Web UI",
            null);
        var harness = CreateEditableCurrentHarness(currentSnapshotId, nodeId);
        harness.SetTransaction(transaction);
        harness.AddNode(new Node(workingSnapshotId, nodeId, null, "Bearbeitbarer Knoten", null, 0, false));
        harness.AddContent(new NodeContent(
            workingSnapshotId,
            nodeId,
            new AudienceId("Developer"),
            new ContentRevisionId(Guid.Parse("10000000-0000-0000-0000-000000000035")),
            ContentMode.Derived,
            "Race-Inhalt",
            false));
        var navigation = harness.CreateService();
        Services.AddSingleton(navigation);
        Services.AddSingleton<IContextSelectionAudienceCatalog>(new ContextSelectionAudienceCatalog(navigation));
        Services.AddSingleton(new TransactionService(
            new ControlledTransactionRepository(_ => transaction),
            new InMemoryWorkingSnapshotValidationDataRepository(),
            new FixedIdentifierGenerator { FixedTransactionId = transactionId },
            TestPolicies.DefaultValidation));
        Services.AddSingleton<ICurrentUserService>(new TestCurrentUserService());

        var cut = Render<NodeDetailsPane>(parameters => parameters
            .Add(pane => pane.NodeId, nodeId.Value)
            .Add(pane => pane.ReadContext, new ReadContext())
            .Add(pane => pane.AudienceId, "Developer"));

        cut.WaitForElement("[data-testid='node-details-edit']").Click();
        cut.WaitForElement("[data-testid='node-edit-begin-working-copy']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Current", cut.Find("[data-testid='node-edit-begin-error']").TextContent, StringComparison.Ordinal);
            Assert.Contains("Race-Arbeitskopie", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("transactionId=", Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(cut.Find("[data-testid='node-edit-working-copy-details-10000000-0000-0000-0000-000000000033']").GetAttribute("href"));
        });
    }

    [Theory]
    [InlineData("Fallback")]
    [InlineData("None")]
    [InlineData("Derived")]
    public void NodeDetailsPane_NonIndependentCurrent_DoesNotOfferEditAffordance(string state)
    {
        var snapshotId = new SnapshotId(1);
        var nodeId = new NodeId(Guid.Parse(state switch
        {
            "Fallback" => "10000000-0000-0000-0000-000000000040",
            "None" => "10000000-0000-0000-0000-000000000041",
            _ => "10000000-0000-0000-0000-000000000042"
        }));
        var harness = new NavigationTestHarness(snapshotId);
        harness.AddNode(new Node(snapshotId, nodeId, null, $"{state}-Knoten", null, 0, false));
        if (state == "Fallback")
        {
            harness.AddAudience(new Audience(snapshotId, new AudienceId("Reader"), "Reader", null, false));
            harness.AddAudienceResolution(new AudienceResolution(snapshotId, new AudienceId("Reader"), new AudienceId("Developer"), 1));
            harness.AddContent(new NodeContent(snapshotId, nodeId, new AudienceId("Developer"), new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Fallback-Quelle", false));
        }
        else if (state == "Derived")
        {
            harness.AddContent(new NodeContent(snapshotId, nodeId, new AudienceId("Developer"), new ContentRevisionId(Guid.NewGuid()), ContentMode.Derived, "Derived-Inhalt", false));
        }

        Services.AddSingleton(harness.CreateService());
        var audienceId = state == "Fallback" ? "Reader" : "Developer";
        var cut = Render<NodeDetailsPane>(parameters => parameters
            .Add(pane => pane.NodeId, nodeId.Value)
            .Add(pane => pane.ReadContext, new ReadContext())
            .Add(pane => pane.AudienceId, audienceId));

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll("[data-testid='node-details-edit']"));
            Assert.Empty(cut.FindAll("[data-testid='node-edit-dialog']"));
        });
    }

    [Fact]
    public void NodeDetailsPane_HistoricalSnapshot_DoesNotOfferEditAffordance()
    {
        var currentSnapshotId = new SnapshotId(1);
        var historicalSnapshotId = new SnapshotId(2);
        var nodeId = new NodeId(Guid.Parse("10000000-0000-0000-0000-000000000043"));
        var harness = new NavigationTestHarness(currentSnapshotId);
        harness.AddHistoricalSnapshot(new Snapshot(historicalSnapshotId, null, SnapshotState.Committed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        harness.AddNode(new Node(historicalSnapshotId, nodeId, null, "Historischer Knoten", null, 0, false));
        harness.AddContent(new NodeContent(historicalSnapshotId, nodeId, new AudienceId("Developer"), new ContentRevisionId(Guid.NewGuid()), ContentMode.Independent, "Historischer Inhalt", false));
        Services.AddSingleton(harness.CreateService());

        var cut = Render<NodeDetailsPane>(parameters => parameters
            .Add(pane => pane.NodeId, nodeId.Value)
            .Add(pane => pane.ReadContext, new ReadContext(SnapshotId: historicalSnapshotId))
            .Add(pane => pane.AudienceId, "Developer")
            .Add(pane => pane.QuerySnapshotId, historicalSnapshotId.Value.ToString()));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[data-testid='node-details-edit']")));
    }

    private static NavigationTestHarness CreateEditableCurrentHarness(SnapshotId snapshotId, NodeId nodeId)
    {
        var harness = new NavigationTestHarness(snapshotId);
        AddIndependentContent(harness, snapshotId, nodeId, "Current-Inhalt");
        return harness;
    }

    private static void AddIndependentContent(
        NavigationTestHarness harness,
        SnapshotId snapshotId,
        NodeId nodeId,
        string markdown)
    {
        harness.AddNode(new Node(snapshotId, nodeId, null, "Bearbeitbarer Knoten", null, 0, false));
        harness.AddContent(new NodeContent(
            snapshotId,
            nodeId,
            new AudienceId("Developer"),
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            markdown,
            false));
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public CurrentUser GetCurrentUser() => new("test-user", "TestUser");
    }

    private sealed class ControlledTransactionRepository(
        Func<BeginTransactionRequest, KnowledgeTransaction> begin) : ITransactionRepository
    {
        public Task<KnowledgeTransaction> BeginAsync(BeginTransactionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(begin(request));

        public Task<KnowledgeTransaction?> FindAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<KnowledgeTransaction?>(null);

        public Task<IReadOnlyList<KnowledgeTransaction>> ListOpenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<KnowledgeTransaction>>([]);

        public Task<CommitTransactionResult> CommitAsync(CommitTransactionRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<KnowledgeTransaction>> DiscardAsync(TransactionId transactionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
