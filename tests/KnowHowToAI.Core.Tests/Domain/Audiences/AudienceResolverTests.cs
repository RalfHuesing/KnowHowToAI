using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Tests.Domain.Audiences;

[Trait("Category", "Unit")]
public sealed class AudienceResolverTests
{
    private static readonly SnapshotId SnapshotId = new(17);
    private static readonly NodeId NodeId = new(Guid.Parse("372b4cd9-6dee-4a60-9819-5aa8e3c32a1b"));
    private static readonly AudienceId Developer = new("Developer");
    private static readonly AudienceId Consultant = new("Consultant");
    private static readonly AudienceId Default = new("Default");

    [Fact]
    public void Resolve_UsesTheFirstAvailableExplicitCandidateInPriorityOrder()
    {
        var result = AudienceResolver.Resolve(Request(
            audiences: [Audience(Developer), Audience(Consultant), Audience(Default)],
            resolutions:
            [
                Resolution(Default, 3),
                Resolution(Developer, 1),
                Resolution(Consultant, 2)
            ],
            contents: [Content(Consultant, ContentMode.Derived), Content(Default, ContentMode.Independent)]));

        Assert.True(result.IsSuccess);
        Assert.Equal(Developer, result.Value!.RequestedAudience);
        Assert.Equal(Consultant, result.Value.ResolvedAudience);
        Assert.Equal(Availability.Fallback, result.Value.Availability);
        Assert.True(result.Value.FallbackUsed);
        Assert.True(result.Value.ResolutionConfigured);
        Assert.Equal(Consultant, result.Value.Content!.AudienceId);
        Assert.Equal(ContentMode.Derived, result.Value.Content.ContentMode);
        Assert.Equal("Fachlicher Inhalt".Length, result.Value.Content.ContentLength);
    }

    [Fact]
    public void Resolve_RequestedAudienceWithExplicitContent_IsNotFallback()
    {
        var result = AudienceResolver.Resolve(Request(
            audiences: [Audience(Developer)],
            resolutions: [Resolution(Developer, 1)],
            contents: [Content(Developer)]));

        Assert.True(result.IsSuccess);
        Assert.Equal(Availability.Explicit, result.Value!.Availability);
        Assert.Equal(Developer, result.Value.ResolvedAudience);
        Assert.False(result.Value.FallbackUsed);
    }

    [Fact]
    public void Resolve_MissingConfiguration_DoesNotAddAnImplicitRequestedCandidate()
    {
        var result = AudienceResolver.Resolve(Request(
            audiences: [Audience(Developer)],
            resolutions: [],
            contents: [Content(Developer)]));

        Assert.True(result.IsSuccess);
        Assert.Equal(Availability.None, result.Value!.Availability);
        Assert.Null(result.Value.ResolvedAudience);
        Assert.False(result.Value.FallbackUsed);
        Assert.False(result.Value.ResolutionConfigured);
        Assert.Null(result.Value.Content);
    }

    [Fact]
    public void Resolve_ConfiguredCandidatesWithoutContent_ReturnsConfiguredNone()
    {
        var result = AudienceResolver.Resolve(Request(
            audiences: [Audience(Developer), Audience(Default)],
            resolutions: [Resolution(Developer, 1), Resolution(Default, 2)],
            contents: []));

        Assert.True(result.IsSuccess);
        Assert.Equal(Availability.None, result.Value!.Availability);
        Assert.True(result.Value.ResolutionConfigured);
        Assert.Null(result.Value.ResolvedAudience);
    }

    [Fact]
    public void Resolve_DeletedCandidate_ReturnsCandidateAudienceDeleted()
    {
        var result = AudienceResolver.Resolve(Request(
            audiences: [Audience(Developer), Audience(Default, isDeleted: true)],
            resolutions: [Resolution(Developer, 1), Resolution(Default, 2)],
            contents: []));

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.CandidateAudienceDeleted, result.Code);
        Assert.Equal(Default.ToString(), result.Details[AudienceResolutionErrorCodes.CandidateAudienceIdDetail]);
    }

    [Fact]
    public void Resolve_MissingCandidate_ReturnsCandidateAudienceNotFound()
    {
        var result = AudienceResolver.Resolve(Request(
            audiences: [Audience(Developer)],
            resolutions: [Resolution(Developer, 1), Resolution(Default, 2)],
            contents: []));

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.CandidateAudienceNotFound, result.Code);
    }

    [Fact]
    public void Resolve_DuplicateCandidate_ReturnsDuplicateCandidateAudience()
    {
        var result = AudienceResolver.Resolve(Request(
            audiences: [Audience(Developer)],
            resolutions: [Resolution(Developer, 1), Resolution(Developer, 2)],
            contents: []));

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.DuplicateCandidateAudience, result.Code);
    }

    [Fact]
    public void Resolve_DuplicatePriority_ReturnsDuplicatePriority()
    {
        var result = AudienceResolver.Resolve(Request(
            audiences: [Audience(Developer), Audience(Default)],
            resolutions: [Resolution(Developer, 1), Resolution(Default, 1)],
            contents: []));

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.DuplicatePriority, result.Code);
        Assert.Equal("1", result.Details[AudienceResolutionErrorCodes.PriorityDetail]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Resolve_NonPositiveCandidatePriority_ReturnsInvalidPriority(int priority)
    {
        var result = AudienceResolver.Resolve(Request(
            audiences: [Audience(Developer)],
            resolutions: [Resolution(Developer, priority)],
            contents: []));

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.InvalidPriority, result.Code);
        Assert.Equal(priority.ToString(System.Globalization.CultureInfo.InvariantCulture), result.Details[AudienceResolutionErrorCodes.PriorityDetail]);
    }

    [Fact]
    public void Resolve_DeletedRequestedAudience_ReturnsRequestedAudienceDeleted()
    {
        var result = AudienceResolver.Resolve(Request(
            audiences: [Audience(Developer, isDeleted: true)],
            resolutions: [Resolution(Developer, 1)],
            contents: [Content(Developer)]));

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceResolutionErrorCodes.RequestedAudienceDeleted, result.Code);
        Assert.Equal(Developer.ToString(), result.Details[AudienceResolutionErrorCodes.RequestedAudienceIdDetail]);
    }

    [Fact]
    public void Resolve_DeletedContent_IsNotAvailable()
    {
        var result = AudienceResolver.Resolve(Request(
            audiences: [Audience(Developer)],
            resolutions: [Resolution(Developer, 1)],
            contents: [Content(Developer, isDeleted: true)]));

        Assert.True(result.IsSuccess);
        Assert.Equal(Availability.None, result.Value!.Availability);
    }

    private static AudienceResolutionRequest Request(
        IEnumerable<Audience> audiences,
        IEnumerable<AudienceResolution> resolutions,
        IEnumerable<NodeContent> contents) =>
        new(SnapshotId, NodeId, Developer, audiences, resolutions, contents);

    private static Audience Audience(AudienceId audienceId, bool isDeleted = false) =>
        new(SnapshotId, audienceId, audienceId.ToString(), null, isDeleted);

    private static AudienceResolution Resolution(AudienceId candidateAudienceId, int priority) =>
        new(SnapshotId, Developer, candidateAudienceId, priority);

    private static NodeContent Content(
        AudienceId audienceId,
        ContentMode contentMode = ContentMode.Independent,
        bool isDeleted = false) =>
        new(
            SnapshotId,
            NodeId,
            audienceId,
            new ContentRevisionId(Guid.Parse("50a7f4a2-2c43-44be-8f98-f403444d3e9f")),
            contentMode,
            "Fachlicher Inhalt",
            isDeleted);
}
