using KnowHowToAI.Core.Application.Dashboard;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Features.Dashboard;

namespace KnowHowToAI.Web.Tests.Features.Dashboard;

[Trait("Category", "Unit")]
public sealed class DashboardMapperTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 19, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToDashboardViewModel_MapsAllFieldsCorrectly()
    {
        var snapshot = new Snapshot(new SnapshotId(1), null, SnapshotState.Committed, Now.AddDays(-1), Now.AddDays(-1));
        var release = new Release(new ReleaseId(1), new SnapshotId(1), "v1.0", "Beschreibung", Now.AddHours(-12));

        var txId = new TransactionId(Guid.NewGuid());
        var tx = new KnowledgeTransaction(
            txId,
            new SnapshotId(1),
            new SnapshotId(2),
            TransactionState.Open,
            ChangeVersion: 5,
            CreatedAtUtc: Now.AddDays(-3),
            CommittedAtUtc: null,
            Purpose: "Feature X",
            Actor: "Developer",
            Client: "Web",
            CommitMessage: null);

        var openTxSummary = new OpenTransactionSummary(tx, [new DomainError("TestError", "Harter Fehler")]);

        var staleContent = new StaleContent(new NodeId(Guid.NewGuid()), new RoleId("Dev"), new ContentRevisionId(Guid.NewGuid()));
        var warning = new DomainWarning("WarnungCode", "Qualitätswarnung");
        var qualitySummary = new CurrentQualitySummary([staleContent], [warning], []);

        var change = new RecentNodeChange(new NodeId(Guid.NewGuid()), "Test Node", DiffChangeKind.Added);

        var result = new DashboardResult(
            snapshot,
            release,
            [openTxSummary],
            qualitySummary,
            [change]);

        var vm = DashboardMapper.ToDashboardViewModel(result, Now);

        Assert.Equal(1, vm.CurrentSnapshot.SnapshotId);
        Assert.NotNull(vm.LatestRelease);
        Assert.Equal("v1.0", vm.LatestRelease.Name);

        Assert.Single(vm.OpenTransactions);
        var txVm = vm.OpenTransactions[0];
        Assert.Equal(txId.Value, txVm.TransactionId);
        Assert.False(txVm.IsOlderThan7Days);
        Assert.Single(txVm.ValidationErrors);
        Assert.Equal("TestError", txVm.ValidationErrors[0].Code);
        Assert.Equal("Harter Fehler", txVm.ValidationErrors[0].Message);

        Assert.Equal(1, vm.QualitySummary.StaleContentCount);
        Assert.Equal(1, vm.QualitySummary.WarningCount);
        Assert.Equal("WarnungCode", vm.QualitySummary.Warnings[0].Code);
        Assert.Equal("Qualitätswarnung", vm.QualitySummary.Warnings[0].Message);

        Assert.Single(vm.RecentChanges);
        Assert.Equal("Test Node", vm.RecentChanges[0].Title);
        Assert.Equal("Added", vm.RecentChanges[0].ChangeKind);
    }

    [Theory]
    [InlineData(6, false)]
    [InlineData(7, true)]
    [InlineData(8, true)]
    public void ToOpenTransactionItemViewModel_Calculates7DayWarningCorrectly(int daysAgo, bool expectedWarning)
    {
        var tx = new KnowledgeTransaction(
            new TransactionId(Guid.NewGuid()),
            new SnapshotId(1),
            new SnapshotId(2),
            TransactionState.Open,
            ChangeVersion: 1,
            CreatedAtUtc: Now.AddDays(-daysAgo),
            CommittedAtUtc: null,
            Purpose: "Zweck",
            Actor: "Akteur",
            Client: "Client",
            CommitMessage: null);

        var summary = new OpenTransactionSummary(tx, []);
        var vm = DashboardMapper.ToOpenTransactionItemViewModel(summary, Now);

        Assert.Equal(expectedWarning, vm.IsOlderThan7Days);
    }

    [Fact]
    public void ToDashboardResult_PreservesErrorsAndWarnings()
    {
        var error = new DomainError("DashboardError", "Konnte nicht geladen werden.");
        var warning = new DomainWarning("DashboardWarn", "Verzögerung");
        var failedResult = Result<DashboardResult>.Failure(error, [warning]);

        var vmResult = DashboardMapper.ToDashboardResult(failedResult, Now);

        Assert.False(vmResult.IsSuccess);
        Assert.Equal("DashboardError", vmResult.Error!.Code);
        Assert.Single(vmResult.Warnings);
        Assert.Equal("DashboardWarn", vmResult.Warnings[0].Code);
    }
}
