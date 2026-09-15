using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Tests.Domain.Roles;

[Trait("Category", "Unit")]
public sealed class RoleResolverTests
{
    private static readonly SnapshotId SnapshotId = new(17);
    private static readonly NodeId NodeId = new(Guid.Parse("372b4cd9-6dee-4a60-9819-5aa8e3c32a1b"));
    private static readonly RoleId Developer = new("Developer");
    private static readonly RoleId Consultant = new("Consultant");
    private static readonly RoleId Default = new("Default");

    [Fact]
    public void Resolve_UsesTheFirstAvailableExplicitCandidateInPriorityOrder()
    {
        var result = RoleResolver.Resolve(Request(
            roles: [Role(Developer), Role(Consultant), Role(Default)],
            resolutions:
            [
                Resolution(Default, 3),
                Resolution(Developer, 1),
                Resolution(Consultant, 2)
            ],
            contents: [Content(Consultant, ContentMode.Derived), Content(Default, ContentMode.Independent)]));

        Assert.True(result.IsSuccess);
        Assert.Equal(Developer, result.Value!.RequestedRole);
        Assert.Equal(Consultant, result.Value.ResolvedRole);
        Assert.Equal(Availability.Fallback, result.Value.Availability);
        Assert.True(result.Value.FallbackUsed);
        Assert.True(result.Value.ResolutionConfigured);
        Assert.Equal(Consultant, result.Value.Content!.RoleId);
        Assert.Equal(ContentMode.Derived, result.Value.Content.ContentMode);
        Assert.Equal("Fachlicher Inhalt".Length, result.Value.Content.ContentLength);
    }

    [Fact]
    public void Resolve_RequestedRoleWithExplicitContent_IsNotFallback()
    {
        var result = RoleResolver.Resolve(Request(
            roles: [Role(Developer)],
            resolutions: [Resolution(Developer, 1)],
            contents: [Content(Developer)]));

        Assert.True(result.IsSuccess);
        Assert.Equal(Availability.Explicit, result.Value!.Availability);
        Assert.Equal(Developer, result.Value.ResolvedRole);
        Assert.False(result.Value.FallbackUsed);
    }

    [Fact]
    public void Resolve_MissingConfiguration_DoesNotAddAnImplicitRequestedCandidate()
    {
        var result = RoleResolver.Resolve(Request(
            roles: [Role(Developer)],
            resolutions: [],
            contents: [Content(Developer)]));

        Assert.True(result.IsSuccess);
        Assert.Equal(Availability.None, result.Value!.Availability);
        Assert.Null(result.Value.ResolvedRole);
        Assert.False(result.Value.FallbackUsed);
        Assert.False(result.Value.ResolutionConfigured);
        Assert.Null(result.Value.Content);
    }

    [Fact]
    public void Resolve_ConfiguredCandidatesWithoutContent_ReturnsConfiguredNone()
    {
        var result = RoleResolver.Resolve(Request(
            roles: [Role(Developer), Role(Default)],
            resolutions: [Resolution(Developer, 1), Resolution(Default, 2)],
            contents: []));

        Assert.True(result.IsSuccess);
        Assert.Equal(Availability.None, result.Value!.Availability);
        Assert.True(result.Value.ResolutionConfigured);
        Assert.Null(result.Value.ResolvedRole);
    }

    [Fact]
    public void Resolve_DeletedCandidate_ReturnsCandidateRoleDeleted()
    {
        var result = RoleResolver.Resolve(Request(
            roles: [Role(Developer), Role(Default, isDeleted: true)],
            resolutions: [Resolution(Developer, 1), Resolution(Default, 2)],
            contents: []));

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.CandidateRoleDeleted, result.Code);
        Assert.Equal(Default.ToString(), result.Details[RoleResolutionErrorCodes.CandidateRoleIdDetail]);
    }

    [Fact]
    public void Resolve_MissingCandidate_ReturnsCandidateRoleNotFound()
    {
        var result = RoleResolver.Resolve(Request(
            roles: [Role(Developer)],
            resolutions: [Resolution(Developer, 1), Resolution(Default, 2)],
            contents: []));

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.CandidateRoleNotFound, result.Code);
    }

    [Fact]
    public void Resolve_DuplicateCandidate_ReturnsDuplicateCandidateRole()
    {
        var result = RoleResolver.Resolve(Request(
            roles: [Role(Developer)],
            resolutions: [Resolution(Developer, 1), Resolution(Developer, 2)],
            contents: []));

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.DuplicateCandidateRole, result.Code);
    }

    [Fact]
    public void Resolve_DuplicatePriority_ReturnsDuplicatePriority()
    {
        var result = RoleResolver.Resolve(Request(
            roles: [Role(Developer), Role(Default)],
            resolutions: [Resolution(Developer, 1), Resolution(Default, 1)],
            contents: []));

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.DuplicatePriority, result.Code);
        Assert.Equal("1", result.Details[RoleResolutionErrorCodes.PriorityDetail]);
    }

    [Fact]
    public void Resolve_DeletedRequestedRole_ReturnsRequestedRoleDeleted()
    {
        var result = RoleResolver.Resolve(Request(
            roles: [Role(Developer, isDeleted: true)],
            resolutions: [Resolution(Developer, 1)],
            contents: [Content(Developer)]));

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleDeleted, result.Code);
        Assert.Equal(Developer.ToString(), result.Details[RoleResolutionErrorCodes.RequestedRoleIdDetail]);
    }

    [Fact]
    public void Resolve_DeletedContent_IsNotAvailable()
    {
        var result = RoleResolver.Resolve(Request(
            roles: [Role(Developer)],
            resolutions: [Resolution(Developer, 1)],
            contents: [Content(Developer, isDeleted: true)]));

        Assert.True(result.IsSuccess);
        Assert.Equal(Availability.None, result.Value!.Availability);
    }

    private static RoleResolutionRequest Request(
        IEnumerable<Role> roles,
        IEnumerable<RoleResolution> resolutions,
        IEnumerable<NodeContent> contents) =>
        new(SnapshotId, NodeId, Developer, roles, resolutions, contents);

    private static Role Role(RoleId roleId, bool isDeleted = false) =>
        new(SnapshotId, roleId, roleId.ToString(), null, isDeleted);

    private static RoleResolution Resolution(RoleId candidateRoleId, int priority) =>
        new(SnapshotId, Developer, candidateRoleId, priority);

    private static NodeContent Content(
        RoleId roleId,
        ContentMode contentMode = ContentMode.Independent,
        bool isDeleted = false) =>
        new(
            SnapshotId,
            NodeId,
            roleId,
            new ContentRevisionId(Guid.Parse("50a7f4a2-2c43-44be-8f98-f403444d3e9f")),
            contentMode,
            "Fachlicher Inhalt",
            isDeleted);
}
