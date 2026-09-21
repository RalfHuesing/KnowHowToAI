using Bunit;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Layout.PageRegions;
using KnowHowToAI.Server.Web.Features.Transactions;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowHowToAI.Web.Tests.Features.Transactions;

[Trait("Category", "Unit")]
public sealed class TransactionPageTests : BunitContext
{
    private static readonly DateTimeOffset Now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TransactionGuid = Guid.Parse("00000000-0000-0000-0000-000000000100");
    private static readonly Guid ValidationNodeGuid = Guid.Parse("00000000-0000-0000-0000-000000000101");
    private static readonly Guid SourceNodeGuid = Guid.Parse("00000000-0000-0000-0000-000000000102");
    private static readonly Guid DerivedNodeGuid = Guid.Parse("00000000-0000-0000-0000-000000000103");
    private static readonly Guid ContentRevisionGuid = Guid.Parse("00000000-0000-0000-0000-000000000104");
    private static readonly Guid SourceRevisionGuid = Guid.Parse("00000000-0000-0000-0000-000000000105");
    private static readonly Guid DerivedRevisionGuid = Guid.Parse("00000000-0000-0000-0000-000000000106");
    private readonly TransactionTestHarness _harness;
    private readonly PageRegionState _pageRegionState = new();
    private readonly WorkspaceState _workspaceState = new();

    public TransactionPageTests()
    {
        _harness = new TransactionTestHarness(now: Now);
        _harness.ValidationPolicy = new ValidationPolicy
        {
            ContentSizeWarningBytes = 1,
            ChildCountWarning = 10,
            HierarchyDepthWarning = 10
        };
        Services.AddWebPageStates(_pageRegionState, _workspaceState);
        Services.AddSingleton<IClock>(new FixedClock(Now));
        Services.AddSingleton<ICurrentUserService>(new TestCurrentUserService());
        Services.AddSingleton(_harness.CreateService());
        Services.AddSingleton(_harness.CreateHistoryService());
        JSInterop.SetupAppDialog();
    }

    [Fact]
    public void TransactionPage_ValidTransaction_RendersDetailsAndSetsWorkingContext()
    {
        var txId = new TransactionId(Guid.NewGuid());
        var tx = new KnowledgeTransaction(
            txId,
            new SnapshotId(1),
            new SnapshotId(10),
            TransactionState.Open,
            ChangeVersion: 3,
            CreatedAtUtc: Now.AddHours(-1),
            CommittedAtUtc: null,
            Purpose: "Glossar Überarbeitung",
            Actor: "Alice",
            Client: "Web UI",
            CommitMessage: null);

        _harness.AddTransaction(tx);
        _workspaceState.SetAudience("Architekt");

        var cut = Render<TransactionPage>(parameters => parameters
            .Add(p => p.TransactionId, txId.Value));

        // Header gerendert
        var header = cut.Find("[data-testid='transaction-header']");
        Assert.NotNull(header);
        Assert.Equal("Glossar Überarbeitung", cut.Find("[data-testid='tx-title']").TextContent.Trim());
        Assert.Equal("Open", cut.Find("[data-testid='tx-state']").TextContent.Trim());
        Assert.Equal("Alice", cut.Find("[data-testid='tx-actor']").TextContent.Trim());
        Assert.Equal("Web UI", cut.Find("[data-testid='tx-client']").TextContent.Trim());
        Assert.Equal("1", cut.Find("[data-testid='tx-base-snapshot']").TextContent.Trim());
        Assert.Equal("10", cut.Find("[data-testid='tx-working-snapshot']").TextContent.Trim());
        Assert.Equal("3", cut.Find("[data-testid='tx-change-version']").TextContent.Trim());

        // Arbeitskontext in PageRegions und WorkspaceState gesetzt
        Assert.Equal(KnowledgeReadContextKind.Transaction, _pageRegionState.KnowledgeContext?.ReadContext);
        Assert.Equal(txId.Value.ToString("D"), _pageRegionState.KnowledgeContext?.ContextId);
        Assert.Equal("Glossar Überarbeitung", _pageRegionState.KnowledgeContext?.DisplayName);

        Assert.Equal(KnowledgeReadContextKind.Transaction, _workspaceState.CurrentContext.ReadContext);
        Assert.Equal(txId, _workspaceState.CurrentReadContext.TransactionId);

        // Link in den Wissensbaum mit Zielgruppe
        var openKnowledgeLink = cut.Find("[data-testid='tx-open-knowledge-link']");
        Assert.Equal($"/knowledge?transactionId={txId.Value}&audienceId=Architekt", openKnowledgeLink.GetAttribute("href"));
    }

    [Fact]
    public void TransactionPage_GuidesValidationBeforeCompletionAndKeepsTechnicalDetailsProgressive()
    {
        var transaction = AddOpenTransaction();

        var cut = Render<TransactionPage>(parameters => parameters
            .Add(p => p.TransactionId, transaction.TransactionId.Value));

        var workflow = cut.Find("[data-testid='transaction-workflow']");
        var workflowMarkup = workflow.OuterHtml;
        Assert.True(
            workflowMarkup.IndexOf("data-testid=\"transaction-validation-step\"", StringComparison.Ordinal)
            < workflowMarkup.IndexOf("data-testid=\"transaction-completion-step\"", StringComparison.Ordinal));

        var technicalDetails = cut.Find("[data-testid='tx-technical-details']");
        Assert.False(technicalDetails.HasAttribute("open"));
        Assert.Contains(transaction.TransactionId.Value.ToString("D"), technicalDetails.TextContent);

        var completionActions = cut.Find("[data-testid='transaction-page-actions']").QuerySelectorAll("button");
        Assert.Equal(2, completionActions.Length);
        Assert.Contains("btn-primary", completionActions[0].GetAttribute("class"));
        Assert.Equal("Commit", completionActions[0].TextContent.Trim());
        Assert.Contains("btn-secondary", completionActions[1].GetAttribute("class"));
        Assert.Equal("Verwerfen", completionActions[1].TextContent.Trim());

        var navigationActions = cut.Find("[data-testid='transaction-next-steps']").QuerySelectorAll("a");
        Assert.Equal(3, navigationActions.Length);
        Assert.Equal("tx-open-knowledge-link", navigationActions[0].GetAttribute("data-testid"));
        Assert.Contains("btn-primary", navigationActions[0].GetAttribute("class"));
        Assert.Contains("btn-secondary", navigationActions[1].GetAttribute("class"));
        Assert.Contains("btn-secondary", navigationActions[2].GetAttribute("class"));
    }

    [Fact]
    public void TransactionPage_MissingTransaction_RendersErrorMessage()
    {
        var nonExistentId = Guid.NewGuid();

        var cut = Render<TransactionPage>(parameters => parameters
            .Add(p => p.TransactionId, nonExistentId));

        var error = cut.Find("[data-testid='transaction-error']");
        Assert.NotNull(error);
        Assert.Contains("existiert nicht", error.TextContent);

        Assert.Equal("Fehlerhafter Kontext", _pageRegionState.KnowledgeContext?.DisplayName);
        Assert.Equal(KnowledgeReadContextKind.Current, _pageRegionState.KnowledgeContext?.ReadContext);

        var backLink = cut.Find("[data-testid='tx-back-link']");
        Assert.Equal("/transactions", backLink.GetAttribute("href"));
    }

    [Fact]
    public void TransactionPage_AgeWarningBadge_ShownWhenOlderThan7Days()
    {
        var oldTxId = new TransactionId(Guid.NewGuid());
        var oldTx = new KnowledgeTransaction(
            oldTxId,
            new SnapshotId(1),
            new SnapshotId(11),
            TransactionState.Open,
            ChangeVersion: 5,
            CreatedAtUtc: Now.AddDays(-8),
            CommittedAtUtc: null,
            Purpose: "Alte Transaction",
            Actor: "Bob",
            Client: "Desktop",
            CommitMessage: null);

        _harness.AddTransaction(oldTx);

        var cut = Render<TransactionPage>(parameters => parameters
            .Add(p => p.TransactionId, oldTxId.Value));

        Assert.Single(cut.FindAll("[data-testid='tx-age-warning']"));
    }

    [Fact]
    public void TransactionPage_ValidationWithNoFindings_ShowsValidState()
    {
        var transaction = AddOpenTransaction();
        _harness.SetValidationData(transaction.TransactionId, EmptyValidationData());

        var cut = Render<TransactionPage>(parameters => parameters.Add(p => p.TransactionId, transaction.TransactionId.Value));

        var validationButton = cut.Find("[data-testid='validate-transaction-button']");
        Assert.False(validationButton.HasAttribute("disabled"));
        validationButton.Click();

        Assert.Single(cut.FindAll("[data-testid='validation-valid']"));
        Assert.Empty(cut.FindAll("[data-testid='validation-errors']"));
        Assert.Empty(cut.FindAll("[data-testid='validation-warnings']"));
    }

    [Fact]
    public void TransactionPage_ValidationWithError_ShowsErrorAndNodeNavigation()
    {
        var transaction = AddOpenTransaction();
        var nodeId = ValidationNodeGuid;
        _harness.SetValidationData(transaction.TransactionId, ValidationData(nodeId, "# Nicht erlaubt"));
        _workspaceState.SetAudience("Architekt");

        var cut = Render<TransactionPage>(parameters => parameters.Add(p => p.TransactionId, transaction.TransactionId.Value));

        cut.Find("[data-testid='validate-transaction-button']").Click();

        Assert.Single(cut.FindAll("[data-testid='validation-invalid']"));
        Assert.Contains("HeadingNotAllowed", cut.Find("[data-testid='validation-errors']").TextContent);
        Assert.Equal(
            $"/knowledge/{nodeId:D}?transactionId={transaction.TransactionId.Value:D}&audienceId=Architekt",
            cut.Find($"[data-testid='validation-error-node-{nodeId:D}']").GetAttribute("href"));
    }

    [Fact]
    public void TransactionPage_ValidationWithWarning_ShowsWarningAsNonBlocking()
    {
        var transaction = AddOpenTransaction();
        var nodeId = ValidationNodeGuid;
        _harness.SetValidationData(transaction.TransactionId, ValidationData(nodeId, "Zu groß"));

        var cut = Render<TransactionPage>(parameters => parameters.Add(p => p.TransactionId, transaction.TransactionId.Value));

        cut.Find("[data-testid='validate-transaction-button']").Click();

        Assert.Single(cut.FindAll("[data-testid='validation-valid']"));
        Assert.Contains("NodeTooLarge", cut.Find("[data-testid='validation-warnings']").TextContent);
        Assert.Equal(
            $"/knowledge/{nodeId:D}?transactionId={transaction.TransactionId.Value:D}",
            cut.Find($"[data-testid='validation-warning-node-{nodeId:D}']").GetAttribute("href"));
    }

    [Fact]
    public void TransactionPage_ValidationWithMixedFindings_GroupsAllActionableCategories()
    {
        var transaction = AddOpenTransaction();
        var sourceId = SourceNodeGuid;
        var derivedId = DerivedNodeGuid;
        _harness.SetValidationData(transaction.TransactionId, MixedValidationData(sourceId, derivedId));

        var cut = Render<TransactionPage>(parameters => parameters.Add(p => p.TransactionId, transaction.TransactionId.Value));

        cut.Find("[data-testid='validate-transaction-button']").Click();

        Assert.Single(cut.FindAll("[data-testid='validation-errors']"));
        Assert.Single(cut.FindAll("[data-testid='validation-warnings']"));
        Assert.Single(cut.FindAll("[data-testid='validation-stale-contents']"));
        Assert.Single(cut.FindAll("[data-testid='validation-refactoring-candidates']"));
        Assert.Equal(
            $"/knowledge/{derivedId:D}?transactionId={transaction.TransactionId.Value:D}",
            cut.Find($"[data-testid='validation-stale-node-link-{derivedId:D}']").GetAttribute("href"));
    }

    [Fact]
    public void TransactionPage_ChangeAfterValidation_MarksFindingsAsStale()
    {
        var transaction = AddOpenTransaction(changeVersion: 1);
        _harness.SetValidationData(transaction.TransactionId, EmptyValidationData());

        var cut = Render<TransactionPage>(parameters => parameters.Add(p => p.TransactionId, transaction.TransactionId.Value));
        cut.Find("[data-testid='validate-transaction-button']").Click();

        _workspaceState.SetChangeVersion(2);

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='validation-results-stale']")));
    }

    [Fact]
    public void TransactionPage_ValidationUsesReadChangeVersionAsFindingProvenance()
    {
        var transaction = AddOpenTransaction(changeVersion: 1);
        _harness.SetValidationData(transaction.TransactionId, EmptyValidationData(changeVersion: 1));

        var cut = Render<TransactionPage>(parameters => parameters.Add(p => p.TransactionId, transaction.TransactionId.Value));
        _workspaceState.SetChangeVersion(2);

        cut.Find("[data-testid='validate-transaction-button']").Click();

        var stale = cut.Find("[data-testid='validation-results-stale']");
        Assert.Contains("ChangeVersion 1", stale.TextContent);
    }

    [Fact]
    public void TransactionPage_TransactionDiff_ShowsCreatedModifiedMovedAndDeletedNodes()
    {
        var transaction = AddOpenTransaction();
        var baseSnapshotId = transaction.BaseSnapshotId;
        var workingSnapshotId = transaction.WorkingSnapshotId;
        var modifiedId = new NodeId(Guid.Parse("00000000-0000-0000-0000-000000000201"));
        var movedId = new NodeId(Guid.Parse("00000000-0000-0000-0000-000000000202"));
        var deletedId = new NodeId(Guid.Parse("00000000-0000-0000-0000-000000000203"));
        var createdId = new NodeId(Guid.Parse("00000000-0000-0000-0000-000000000204"));

        _harness.Store.Snapshots.Add(new Snapshot(workingSnapshotId, baseSnapshotId, SnapshotState.Working, Now, null));
        _harness.Store.Nodes.AddRange(
        [
            new Node(baseSnapshotId, modifiedId, null, "Alt", "Alte Beschreibung", 0, false),
            new Node(baseSnapshotId, movedId, null, "Verschieben", null, 1, false),
            new Node(baseSnapshotId, deletedId, null, "Löschen", null, 2, false),
            new Node(workingSnapshotId, modifiedId, null, "Neu", "Neue Beschreibung", 0, false),
            new Node(workingSnapshotId, movedId, modifiedId, "Verschieben", null, 0, false),
            new Node(workingSnapshotId, deletedId, null, "Löschen", null, 2, true),
            new Node(workingSnapshotId, createdId, null, "Erstellen", null, 3, false)
        ]);

        var cut = Render<TransactionPage>(parameters => parameters.Add(p => p.TransactionId, transaction.TransactionId.Value));

        var diff = cut.Find("[data-testid='transaction-diff-list']");
        Assert.Contains("Hinzugefügt", diff.TextContent);
        Assert.Contains("Geändert", diff.TextContent);
        Assert.Contains("Gelöscht", diff.TextContent);
        Assert.Contains("Neue Beschreibung", diff.TextContent);
        Assert.Contains("Parent:", diff.TextContent);
        Assert.Equal(4, cut.FindAll("[data-testid^='transaction-diff-entry-Node-']").Count);
    }

    [Fact]
    public void TransactionPage_TransactionDiff_EmptyTransactionExplainsThatNoCommitIsPlanned()
    {
        var transaction = AddOpenTransaction();
        _harness.Store.Snapshots.Add(new Snapshot(transaction.WorkingSnapshotId, transaction.BaseSnapshotId, SnapshotState.Working, Now, null));

        var cut = Render<TransactionPage>(parameters => parameters.Add(p => p.TransactionId, transaction.TransactionId.Value));

        Assert.Single(cut.FindAll("[data-testid='transaction-diff-empty']"));
    }

    [Fact]
    public void TransactionPage_TransactionDiff_PaginatesAllChangesAndMarksStaleResults()
    {
        var transaction = AddOpenTransaction(changeVersion: 4);
        var baseSnapshotId = transaction.BaseSnapshotId;
        var workingSnapshotId = transaction.WorkingSnapshotId;
        _harness.Store.Snapshots.Add(new Snapshot(workingSnapshotId, baseSnapshotId, SnapshotState.Working, Now, null));
        _harness.Store.Nodes.AddRange(
        [
            new Node(workingSnapshotId, new NodeId(Guid.Parse("00000000-0000-0000-0000-000000000211")), null, "Erste Änderung", null, 0, false),
            new Node(workingSnapshotId, new NodeId(Guid.Parse("00000000-0000-0000-0000-000000000212")), null, "Zweite Änderung", null, 1, false),
            new Node(workingSnapshotId, new NodeId(Guid.Parse("00000000-0000-0000-0000-000000000213")), null, "Dritte Änderung", null, 2, false)
        ]);
        Services.RemoveAll<HistoryService>();
        Services.AddSingleton(new HistoryService(
            new SnapshotReadRepositories(
                new InMemorySnapshotRepository(_harness.Store),
                new InMemoryTransactionRepository(_harness.Store),
                new InMemoryHierarchyRepository(_harness.Store),
                new InMemoryContentRepository(_harness.Store),
                new InMemoryAudienceRepository(_harness.Store),
                new InMemoryDependencyRepository(_harness.Store),
                new InMemoryWorkingSnapshotReadRepository(_harness.Store)),
            new RetrievalPolicy
            {
                DefaultPageSize = 2,
                MaximumPageSize = 2,
                SearchPageSize = 2,
                SearchMaximumPageSize = 2,
                SnippetMaximumCharacters = 200
            }));

        var cut = Render<TransactionPage>(parameters => parameters.Add(p => p.TransactionId, transaction.TransactionId.Value));

        Assert.Single(cut.FindAll("[data-testid='transaction-diff-next']"));
        cut.FindComponent<KnowHowToAI.Server.Web.Features.Transactions.TransactionDiff>()
            .Find("[data-testid='transaction-diff-next']")
            .Click();
        Assert.Equal(3, cut.FindAll("[data-testid^='transaction-diff-entry-Node-']").Count);

        _workspaceState.SetChangeVersion(5);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='transaction-diff-stale']")));
    }

    private KnowledgeTransaction AddOpenTransaction(long changeVersion = 1)
    {
        var transaction = new KnowledgeTransaction(
            new TransactionId(TransactionGuid),
            new SnapshotId(1),
            new SnapshotId(2),
            TransactionState.Open,
            changeVersion,
            Now,
            null,
            "Validierungs-Transaction",
            "Alice",
            "Web UI",
            null);
        _harness.AddTransaction(transaction);
        return transaction;
    }

    private static WorkingSnapshotValidationData EmptyValidationData(long changeVersion = 1) => new([], [], [], [], [], changeVersion);

    private static WorkingSnapshotValidationData ValidationData(Guid nodeId, string contentMd)
    {
        var snapshotId = new SnapshotId(2);
        var audienceId = new AudienceId("Default");
        return new WorkingSnapshotValidationData(
            [new Node(snapshotId, new NodeId(nodeId), null, "Root", null, 0, false)],
            [new Audience(snapshotId, audienceId, "Default", null, false)],
            [new AudienceResolution(snapshotId, audienceId, audienceId, 1)],
            [new NodeContent(snapshotId, new NodeId(nodeId), audienceId, new ContentRevisionId(ContentRevisionGuid), ContentMode.Independent, contentMd, false)],
            [],
            1);
    }

    private static WorkingSnapshotValidationData MixedValidationData(Guid sourceId, Guid derivedId)
    {
        var snapshotId = new SnapshotId(2);
        var audienceId = new AudienceId("Default");
        var sourceRevisionId = new ContentRevisionId(SourceRevisionGuid);
        return new WorkingSnapshotValidationData(
            [
                new Node(snapshotId, new NodeId(sourceId), null, "Source", null, 0, false),
                new Node(snapshotId, new NodeId(derivedId), new NodeId(sourceId), "Derived", null, 0, false)
            ],
            [new Audience(snapshotId, audienceId, "Default", null, false)],
            [new AudienceResolution(snapshotId, audienceId, audienceId, 1)],
            [
                new NodeContent(snapshotId, new NodeId(sourceId), audienceId, sourceRevisionId, ContentMode.Independent, "Quelle", true),
                new NodeContent(snapshotId, new NodeId(derivedId), audienceId, new ContentRevisionId(DerivedRevisionGuid), ContentMode.Derived, "# Fehler", false)
            ],
            [new ContentDependency(snapshotId, new NodeId(derivedId), audienceId, new NodeId(sourceId), audienceId, sourceRevisionId)],
            1);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public CurrentUser GetCurrentUser() => new("transaction-page-test", "TestUser");
    }
}
