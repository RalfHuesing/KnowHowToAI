using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Tests.Application.Navigation;

[Trait("Category", "Unit")]
public sealed class NodeContentResolverTests
{
    private static readonly SnapshotId Snapshot = new(1);
    private static readonly NodeId TestNodeId = new(Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly AudienceId AudienceDeveloper = new("Developer");
    private static readonly AudienceId AudienceConsultant = new("Consultant");
    private static readonly AudienceId AudienceArchived = new("Archived");

    [Fact]
    public void Resolve_ExplicitContent_ReturnsResolvedContentWithEvaluatedFreshnessCurrent()
    {
        var audiences = new[] { new Audience(Snapshot, AudienceDeveloper, "Developer", null, false) };
        var resolutions = new[] { new AudienceResolution(Snapshot, AudienceDeveloper, AudienceDeveloper, 1) };
        var content = new NodeContent(
            Snapshot,
            TestNodeId,
            AudienceDeveloper,
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            "Explicit Content",
            false);

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            AudienceDeveloper,
            Snapshot,
            audiences,
            resolutions,
            new[] { content },
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Content);
        Assert.Equal(content.ContentRevisionId, result.Value.Content!.ContentRevisionId);
        Assert.Equal(AudienceDeveloper, result.Value.RequestedAudience);
        Assert.Equal(AudienceDeveloper, result.Value.ResolvedAudience);
        Assert.Equal(Availability.Explicit, result.Value.Availability);
        Assert.False(result.Value.FallbackUsed);
        Assert.Equal(Freshness.Current, result.Value.Freshness);
    }

    [Fact]
    public void Resolve_DerivedContentWithStaleSource_ReturnsResolvedContentWithFreshnessStale()
    {
        var audiences = new[] { new Audience(Snapshot, AudienceDeveloper, "Developer", null, false) };
        var resolutions = new[] { new AudienceResolution(Snapshot, AudienceDeveloper, AudienceDeveloper, 1) };
        var derivedContent = new NodeContent(
            Snapshot,
            TestNodeId,
            AudienceDeveloper,
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Derived,
            "Derived Content",
            false);
        var missingDependency = new ContentDependency(
            Snapshot,
            TestNodeId,
            AudienceDeveloper,
            new NodeId(Guid.NewGuid()),
            new AudienceId("SourceAudience"),
            new ContentRevisionId(Guid.NewGuid()));

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            AudienceDeveloper,
            Snapshot,
            audiences,
            resolutions,
            new[] { derivedContent },
            new[] { missingDependency });

        var result = NodeContentResolver.Resolve(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Content);
        Assert.Equal(Availability.Explicit, result.Value.Availability);
        Assert.Equal(Freshness.Stale, result.Value.Freshness);
    }

    [Fact]
    public void Resolve_FallbackContent_ReturnsResolvedContentWithAvailabilityFallbackAndFallbackUsedTrue()
    {
        var audiences = new[]
        {
            new Audience(Snapshot, AudienceDeveloper, "Developer", null, false),
            new Audience(Snapshot, AudienceConsultant, "Consultant", null, false)
        };
        var resolutions = new[]
        {
            new AudienceResolution(Snapshot, AudienceDeveloper, AudienceDeveloper, 1),
            new AudienceResolution(Snapshot, AudienceDeveloper, AudienceConsultant, 2)
        };
        var consultantContent = new NodeContent(
            Snapshot,
            TestNodeId,
            AudienceConsultant,
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            "Consultant Fallback Content",
            false);

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            AudienceDeveloper,
            Snapshot,
            audiences,
            resolutions,
            new[] { consultantContent },
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Content);
        Assert.Equal(AudienceConsultant, result.Value.Content!.AudienceId);
        Assert.Equal(AudienceDeveloper, result.Value.RequestedAudience);
        Assert.Equal(AudienceConsultant, result.Value.ResolvedAudience);
        Assert.Equal(Availability.Fallback, result.Value.Availability);
        Assert.True(result.Value.FallbackUsed);
        Assert.Equal(Freshness.Current, result.Value.Freshness);
    }

    [Fact]
    public void Resolve_UnconfiguredResolutionOrder_ReturnsAvailabilityNoneWithNullContent()
    {
        var audiences = new[] { new Audience(Snapshot, AudienceDeveloper, "Developer", null, false) };
        var request = new NodeContentResolutionRequest(
            TestNodeId,
            AudienceDeveloper,
            Snapshot,
            audiences,
            Array.Empty<AudienceResolution>(),
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Content);
        Assert.Equal(AudienceDeveloper, result.Value.RequestedAudience);
        Assert.Null(result.Value.ResolvedAudience);
        Assert.Equal(Availability.None, result.Value.Availability);
        Assert.False(result.Value.FallbackUsed);
        Assert.Equal(Freshness.Unknown, result.Value.Freshness);
    }

    [Fact]
    public void Resolve_RequestedAudienceNotFound_ReturnsRequestedAudienceNotFoundDomainError()
    {
        var missingAudience = new AudienceId("MissingAudience");
        var request = new NodeContentResolutionRequest(
            TestNodeId,
            missingAudience,
            Snapshot,
            Array.Empty<Audience>(),
            Array.Empty<AudienceResolution>(),
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.RequestedAudienceNotFound, result.Error!.Code);
        Assert.Equal(missingAudience.ToString(), result.Error.Details[AudienceResolutionErrorCodes.RequestedAudienceIdDetail]);
    }

    [Fact]
    public void Resolve_RequestedAudienceDeleted_ReturnsRequestedAudienceDeletedDomainError()
    {
        var audiences = new[] { new Audience(Snapshot, AudienceDeveloper, "Developer", null, true) };
        var request = new NodeContentResolutionRequest(
            TestNodeId,
            AudienceDeveloper,
            Snapshot,
            audiences,
            Array.Empty<AudienceResolution>(),
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.RequestedAudienceDeleted, result.Error!.Code);
        Assert.Equal(AudienceDeveloper.ToString(), result.Error.Details[AudienceResolutionErrorCodes.RequestedAudienceIdDetail]);
    }

    [Fact]
    public void Resolve_CandidateAudienceNotFound_ReturnsCandidateAudienceNotFoundDomainError()
    {
        var audiences = new[] { new Audience(Snapshot, AudienceDeveloper, "Developer", null, false) };
        var missingCandidate = new AudienceId("MissingCandidate");
        var resolutions = new[] { new AudienceResolution(Snapshot, AudienceDeveloper, missingCandidate, 1) };

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            AudienceDeveloper,
            Snapshot,
            audiences,
            resolutions,
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.CandidateAudienceNotFound, result.Error!.Code);
        Assert.Equal(missingCandidate.ToString(), result.Error.Details[AudienceResolutionErrorCodes.CandidateAudienceIdDetail]);
    }

    [Fact]
    public void Resolve_CandidateAudienceDeleted_ReturnsCandidateAudienceDeletedDomainError()
    {
        var audiences = new[]
        {
            new Audience(Snapshot, AudienceDeveloper, "Developer", null, false),
            new Audience(Snapshot, AudienceArchived, "Archived", null, true)
        };
        var resolutions = new[] { new AudienceResolution(Snapshot, AudienceDeveloper, AudienceArchived, 1) };

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            AudienceDeveloper,
            Snapshot,
            audiences,
            resolutions,
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.CandidateAudienceDeleted, result.Error!.Code);
        Assert.Equal(AudienceArchived.ToString(), result.Error.Details[AudienceResolutionErrorCodes.CandidateAudienceIdDetail]);
    }

    [Fact]
    public void Resolve_InvalidPriority_ReturnsInvalidPriorityDomainError()
    {
        var audiences = new[] { new Audience(Snapshot, AudienceDeveloper, "Developer", null, false) };
        var resolutions = new[] { new AudienceResolution(Snapshot, AudienceDeveloper, AudienceDeveloper, 0) };

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            AudienceDeveloper,
            Snapshot,
            audiences,
            resolutions,
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.InvalidPriority, result.Error!.Code);
    }

    [Fact]
    public void Resolve_DuplicateCandidateAudience_ReturnsDuplicateCandidateAudienceDomainError()
    {
        var audiences = new[] { new Audience(Snapshot, AudienceDeveloper, "Developer", null, false) };
        var resolutions = new[]
        {
            new AudienceResolution(Snapshot, AudienceDeveloper, AudienceDeveloper, 1),
            new AudienceResolution(Snapshot, AudienceDeveloper, AudienceDeveloper, 2)
        };

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            AudienceDeveloper,
            Snapshot,
            audiences,
            resolutions,
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.DuplicateCandidateAudience, result.Error!.Code);
    }

    [Fact]
    public void Resolve_DuplicatePriority_ReturnsDuplicatePriorityDomainError()
    {
        var audiences = new[]
        {
            new Audience(Snapshot, AudienceDeveloper, "Developer", null, false),
            new Audience(Snapshot, AudienceConsultant, "Consultant", null, false)
        };
        var resolutions = new[]
        {
            new AudienceResolution(Snapshot, AudienceDeveloper, AudienceDeveloper, 1),
            new AudienceResolution(Snapshot, AudienceDeveloper, AudienceConsultant, 1)
        };

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            AudienceDeveloper,
            Snapshot,
            audiences,
            resolutions,
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.DuplicatePriority, result.Error!.Code);
    }

    [Fact]
    public void Resolve_NullNodeId_WithValidAudience_ReturnsAvailabilityNoneAndUnknownFreshness()
    {
        var audiences = new[] { new Audience(Snapshot, AudienceDeveloper, "Developer", null, false) };
        var resolutions = new[] { new AudienceResolution(Snapshot, AudienceDeveloper, AudienceDeveloper, 1) };

        var request = new NodeContentResolutionRequest(
            null,
            AudienceDeveloper,
            Snapshot,
            audiences,
            resolutions,
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Content);
        Assert.Equal(AudienceDeveloper, result.Value.RequestedAudience);
        Assert.Null(result.Value.ResolvedAudience);
        Assert.Equal(Availability.None, result.Value.Availability);
        Assert.False(result.Value.FallbackUsed);
        Assert.Equal(Freshness.Unknown, result.Value.Freshness);
    }

    [Fact]
    public void Resolve_NullNodeId_WithMissingRequestedAudience_ReturnsRequestedAudienceNotFoundDomainError()
    {
        var missingAudience = new AudienceId("MissingAudience");
        var request = new NodeContentResolutionRequest(
            null,
            missingAudience,
            Snapshot,
            Array.Empty<Audience>(),
            Array.Empty<AudienceResolution>(),
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.RequestedAudienceNotFound, result.Error!.Code);
        Assert.Equal(missingAudience.ToString(), result.Error.Details[AudienceResolutionErrorCodes.RequestedAudienceIdDetail]);
    }
}
