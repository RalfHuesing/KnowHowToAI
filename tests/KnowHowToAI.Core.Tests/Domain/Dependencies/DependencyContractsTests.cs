using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;

namespace KnowHowToAI.Core.Tests.Domain.Dependencies;

[Trait("Category", "Unit")]
public sealed class DependencyContractsTests
{
    private static readonly SnapshotId SnapshotId = new(17);
    private static readonly NodeId TargetNodeId = new(Guid.Parse("c197d2d2-a084-48e9-a9a1-bbfc7e6be126"));
    private static readonly NodeId SourceNodeId = new(Guid.Parse("515a7159-3db5-48ee-8e3c-04c13dc60cc3"));
    private static readonly AudienceId TargetAudienceId = new("EndUser");
    private static readonly AudienceId SourceAudienceId = new("Developer");
    private static readonly ContentRevisionId TargetRevisionId = new(Guid.Parse("200ffb07-ae8f-4f7a-bb10-f67cc74e993e"));
    private static readonly ContentRevisionId SourceRevisionId = new(Guid.Parse("4eea4d84-edca-45e2-b261-536cc5590f52"));

    [Fact]
    public void Validate_IndependentContentWithDependency_ReturnsInvalidDependency()
    {
        var report = DependencyValidator.ValidateSnapshot(
            [Content(TargetNodeId, TargetAudienceId, TargetRevisionId, ContentMode.Independent),
                Content(SourceNodeId, SourceAudienceId, SourceRevisionId, ContentMode.Independent)],
            [Dependency()]);

        Assert.Contains(report.Errors, error => error.Code == DependencyErrorCodes.InvalidDependency);
    }

    [Fact]
    public void Validate_DerivedContentWithoutDependency_ReturnsInvalidDependency()
    {
        var report = DependencyValidator.ValidateSnapshot(
            [Content(TargetNodeId, TargetAudienceId, TargetRevisionId, ContentMode.Derived)],
            []);

        Assert.Contains(report.Errors, error => error.Code == DependencyErrorCodes.InvalidDependency);
    }

    [Fact]
    public void Validate_UnknownContentMode_ReturnsInvalidDependency()
    {
        var report = DependencyValidator.ValidateSnapshot(
            [Content(TargetNodeId, TargetAudienceId, TargetRevisionId, ContentMode.Unknown)],
            []);

        Assert.Contains(report.Errors, error => error.Code == DependencyErrorCodes.InvalidDependency);
    }

    [Fact]
    public void ValidateSnapshot_DeletedSource_RemainsValidAndMakesDerivedContentStale()
    {
        var target = Content(TargetNodeId, TargetAudienceId, TargetRevisionId, ContentMode.Derived);
        var contents = new[]
        {
            target,
            Content(SourceNodeId, SourceAudienceId, SourceRevisionId, ContentMode.Independent, isDeleted: true)
        };
        var dependencies = new[] { Dependency() };

        var report = DependencyValidator.ValidateSnapshot(contents, dependencies);

        Assert.True(report.IsValid);
        Assert.Equal(Freshness.Stale, FreshnessEvaluator.Evaluate(target, contents, dependencies));
    }

    [Fact]
    public void ValidateNewOrChangedDependencies_MissingExplicitSource_ReturnsInvalidDependency()
    {
        var report = DependencyValidator.ValidateNewOrChangedDependencies(
            [Content(TargetNodeId, TargetAudienceId, TargetRevisionId, ContentMode.Derived)],
            [Dependency()],
            [Dependency()]);

        Assert.Contains(report.Errors, error => error.Code == DependencyErrorCodes.InvalidDependency);
    }

    [Fact]
    public void Validate_SelfDependency_ReturnsDependencyCycle()
    {
        var report = DependencyValidator.ValidateSnapshot(
            [Content(TargetNodeId, TargetAudienceId, TargetRevisionId, ContentMode.Derived)],
            [new ContentDependency(
                SnapshotId,
                TargetNodeId,
                TargetAudienceId,
                TargetNodeId,
                TargetAudienceId,
                TargetRevisionId)]);

        Assert.Contains(report.Errors, error => error.Code == DependencyErrorCodes.DependencyCycle);
    }

    [Fact]
    public void Validate_TransitiveCycle_ReturnsDependencyCycle()
    {
        var report = DependencyValidator.ValidateSnapshot(
            [Content(TargetNodeId, TargetAudienceId, TargetRevisionId, ContentMode.Derived),
                Content(SourceNodeId, SourceAudienceId, SourceRevisionId, ContentMode.Derived)],
            [Dependency(), new ContentDependency(
                SnapshotId,
                SourceNodeId,
                SourceAudienceId,
                TargetNodeId,
                TargetAudienceId,
                TargetRevisionId)]);

        Assert.Contains(report.Errors, error => error.Code == DependencyErrorCodes.DependencyCycle);
    }

    [Fact]
    public void Evaluate_DerivedContentWhoseSourceRevisionChanged_IsStale()
    {
        var target = Content(TargetNodeId, TargetAudienceId, TargetRevisionId, ContentMode.Derived);

        var freshness = FreshnessEvaluator.Evaluate(
            target,
            [target, Content(SourceNodeId, SourceAudienceId, NewRevision(), ContentMode.Independent)],
            [Dependency()]);

        Assert.Equal(Freshness.Stale, freshness);
    }

    [Fact]
    public void Evaluate_DerivedContentWhoseSourceWasDeleted_IsStale()
    {
        var target = Content(TargetNodeId, TargetAudienceId, TargetRevisionId, ContentMode.Derived);

        var freshness = FreshnessEvaluator.Evaluate(
            target,
            [target, Content(SourceNodeId, SourceAudienceId, SourceRevisionId, ContentMode.Independent, isDeleted: true)],
            [Dependency()]);

        Assert.Equal(Freshness.Stale, freshness);
    }

    [Fact]
    public void Evaluate_IndependentContent_IsCurrentWithoutDependencies()
    {
        var content = Content(TargetNodeId, TargetAudienceId, TargetRevisionId, ContentMode.Independent);

        var freshness = FreshnessEvaluator.Evaluate(content, [content], []);

        Assert.Equal(Freshness.Current, freshness);
    }

    [Fact]
    public void Evaluate_DerivedContentWhoseDerivedSourceIsStale_IsTransitivelyStale()
    {
        var intermediateNodeId = new NodeId(Guid.Parse("71b75070-03cf-4ba1-9edc-cc32ee2f2b5c"));
        var intermediateAudienceId = new AudienceId("Consultant");
        var intermediateRevisionId = new ContentRevisionId(Guid.Parse("528efba9-96f0-4d4f-8df0-b2f8fa9a9f81"));
        var target = Content(TargetNodeId, TargetAudienceId, TargetRevisionId, ContentMode.Derived);
        var intermediate = Content(intermediateNodeId, intermediateAudienceId, intermediateRevisionId, ContentMode.Derived);

        var freshness = FreshnessEvaluator.Evaluate(
            target,
            [target, intermediate, Content(SourceNodeId, SourceAudienceId, NewRevision(), ContentMode.Independent)],
            [new ContentDependency(
                    SnapshotId,
                    TargetNodeId,
                    TargetAudienceId,
                    intermediateNodeId,
                    intermediateAudienceId,
                    intermediateRevisionId),
                new ContentDependency(
                    SnapshotId,
                    intermediateNodeId,
                    intermediateAudienceId,
                    SourceNodeId,
                    SourceAudienceId,
                    SourceRevisionId)]);

        Assert.Equal(Freshness.Stale, freshness);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Evaluate_UsesTheSameRulesForEverySnapshotState(long snapshotValue)
    {
        var snapshotId = new SnapshotId(snapshotValue);
        var target = Content(TargetNodeId, TargetAudienceId, TargetRevisionId, ContentMode.Derived, snapshotId: snapshotId);

        var freshness = FreshnessEvaluator.Evaluate(
            target,
            [target, Content(SourceNodeId, SourceAudienceId, SourceRevisionId, ContentMode.Independent, snapshotId: snapshotId)],
            [Dependency(snapshotId)]);

        Assert.Equal(Freshness.Current, freshness);
    }

    private static NodeContent Content(
        NodeId nodeId,
        AudienceId audienceId,
        ContentRevisionId revisionId,
        ContentMode mode,
        bool isDeleted = false,
        SnapshotId? snapshotId = null) =>
        new(snapshotId ?? SnapshotId, nodeId, audienceId, revisionId, mode, "Content", isDeleted);

    private static ContentDependency Dependency(SnapshotId? snapshotId = null) =>
        new(snapshotId ?? SnapshotId, TargetNodeId, TargetAudienceId, SourceNodeId, SourceAudienceId, SourceRevisionId);

    private static ContentRevisionId NewRevision() =>
        new(Guid.Parse("d2f5c5c6-6504-4c0d-982d-032d1cc6c238"));
}
