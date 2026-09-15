using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Validation;

namespace KnowHowToAI.Core.Tests.Domain.Validation;

[Trait("Category", "Unit")]
public sealed class TransactionValidatorTests
{
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly RoleId RoleId = new("Default");
    private static readonly NodeId RootId = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    private static readonly NodeId SourceId = new(Guid.Parse("00000000-0000-0000-0000-000000000002"));
    private static readonly NodeId DerivedId = new(Guid.Parse("00000000-0000-0000-0000-000000000003"));
    private static readonly ContentRevisionId SourceRevisionId = new(Guid.Parse("00000000-0000-0000-0000-000000000004"));
    private static readonly ContentRevisionId DerivedRevisionId = new(Guid.Parse("00000000-0000-0000-0000-000000000005"));

    [Fact]
    public void Validate_AggregatesAndSortsFindingsWithoutChangingTheInput()
    {
        var request = CreateRequest();

        var first = TransactionValidator.Validate(request);
        var second = TransactionValidator.Validate(request);

        Assert.Contains(first.Errors, error => error.Code == ContentStructureCodes.HeadingNotAllowed);
        var stale = Assert.Single(first.StaleContents);
        Assert.Equal(DerivedId, stale.NodeId);
        Assert.Contains(first.Warnings, warning => warning.Code == QualityWarningCodes.StaleDerivedContent);
        Assert.Contains(first.RefactoringCandidates, candidate => candidate.NodeId == DerivedId && candidate.ReasonCodes.Contains(QualityWarningCodes.NodeTooLarge));
        Assert.Equal(Findings(first), Findings(second));
        Assert.Equal("# heading\nlarge", request.Contents.Single(content => content.NodeId == DerivedId).ContentMd);
    }

    private static TransactionValidationRequest CreateRequest() =>
        new(
            [
                new Node(SnapshotId, RootId, null, "Root", null, 0, false),
                new Node(SnapshotId, SourceId, RootId, "Source", null, 0, false),
                new Node(SnapshotId, DerivedId, RootId, "Derived", null, 1, false)
            ],
            [new Role(SnapshotId, RoleId, "Default", null, false)],
            [new RoleResolution(SnapshotId, RoleId, RoleId, 1)],
            [
                new NodeContent(SnapshotId, SourceId, RoleId, SourceRevisionId, ContentMode.Independent, "source", true),
                new NodeContent(SnapshotId, DerivedId, RoleId, DerivedRevisionId, ContentMode.Derived, "# heading\nlarge", false)
            ],
            [new ContentDependency(SnapshotId, DerivedId, RoleId, SourceId, RoleId, SourceRevisionId)],
            new QualityWarningThresholds(4, 2, 2),
            WarnOnPossibleEmbeddedHeading: false);

    private static string[] Findings(TransactionValidationReport report) =>
        report.Errors.Select(error => error.Code + error.Details[TransactionValidationCodes.NodeIdDetail])
            .Concat(report.Warnings.Select(warning => warning.Code + warning.Details.GetValueOrDefault(TransactionValidationCodes.NodeIdDetail, string.Empty)))
            .Concat(report.StaleContents.Select(content => content.NodeId.ToString()))
            .ToArray();
}
