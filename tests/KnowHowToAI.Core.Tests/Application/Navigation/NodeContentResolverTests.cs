using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Tests.Application.Navigation;

[Trait("Category", "Unit")]
public sealed class NodeContentResolverTests
{
    private static readonly SnapshotId Snapshot = new(1);
    private static readonly NodeId TestNodeId = new(Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly RoleId RoleDeveloper = new("Developer");
    private static readonly RoleId RoleConsultant = new("Consultant");
    private static readonly RoleId RoleArchived = new("Archived");

    [Fact]
    public void Resolve_ExplicitContent_ReturnsResolvedContentWithEvaluatedFreshnessCurrent()
    {
        var roles = new[] { new Role(Snapshot, RoleDeveloper, "Developer", null, false) };
        var resolutions = new[] { new RoleResolution(Snapshot, RoleDeveloper, RoleDeveloper, 1) };
        var content = new NodeContent(
            Snapshot,
            TestNodeId,
            RoleDeveloper,
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            "Explicit Content",
            false);

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            RoleDeveloper,
            Snapshot,
            roles,
            resolutions,
            new[] { content },
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Content);
        Assert.Equal(content.ContentRevisionId, result.Value.Content!.ContentRevisionId);
        Assert.Equal(RoleDeveloper, result.Value.RequestedRole);
        Assert.Equal(RoleDeveloper, result.Value.ResolvedRole);
        Assert.Equal(Availability.Explicit, result.Value.Availability);
        Assert.False(result.Value.FallbackUsed);
        Assert.Equal(Freshness.Current, result.Value.Freshness);
    }

    [Fact]
    public void Resolve_DerivedContentWithStaleSource_ReturnsResolvedContentWithFreshnessStale()
    {
        var roles = new[] { new Role(Snapshot, RoleDeveloper, "Developer", null, false) };
        var resolutions = new[] { new RoleResolution(Snapshot, RoleDeveloper, RoleDeveloper, 1) };
        var derivedContent = new NodeContent(
            Snapshot,
            TestNodeId,
            RoleDeveloper,
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Derived,
            "Derived Content",
            false);
        var missingDependency = new ContentDependency(
            Snapshot,
            TestNodeId,
            RoleDeveloper,
            new NodeId(Guid.NewGuid()),
            new RoleId("SourceRole"),
            new ContentRevisionId(Guid.NewGuid()));

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            RoleDeveloper,
            Snapshot,
            roles,
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
        var roles = new[]
        {
            new Role(Snapshot, RoleDeveloper, "Developer", null, false),
            new Role(Snapshot, RoleConsultant, "Consultant", null, false)
        };
        var resolutions = new[]
        {
            new RoleResolution(Snapshot, RoleDeveloper, RoleDeveloper, 1),
            new RoleResolution(Snapshot, RoleDeveloper, RoleConsultant, 2)
        };
        var consultantContent = new NodeContent(
            Snapshot,
            TestNodeId,
            RoleConsultant,
            new ContentRevisionId(Guid.NewGuid()),
            ContentMode.Independent,
            "Consultant Fallback Content",
            false);

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            RoleDeveloper,
            Snapshot,
            roles,
            resolutions,
            new[] { consultantContent },
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Content);
        Assert.Equal(RoleConsultant, result.Value.Content!.RoleId);
        Assert.Equal(RoleDeveloper, result.Value.RequestedRole);
        Assert.Equal(RoleConsultant, result.Value.ResolvedRole);
        Assert.Equal(Availability.Fallback, result.Value.Availability);
        Assert.True(result.Value.FallbackUsed);
        Assert.Equal(Freshness.Current, result.Value.Freshness);
    }

    [Fact]
    public void Resolve_UnconfiguredResolutionOrder_ReturnsAvailabilityNoneWithNullContent()
    {
        var roles = new[] { new Role(Snapshot, RoleDeveloper, "Developer", null, false) };
        var request = new NodeContentResolutionRequest(
            TestNodeId,
            RoleDeveloper,
            Snapshot,
            roles,
            Array.Empty<RoleResolution>(),
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Content);
        Assert.Equal(RoleDeveloper, result.Value.RequestedRole);
        Assert.Null(result.Value.ResolvedRole);
        Assert.Equal(Availability.None, result.Value.Availability);
        Assert.False(result.Value.FallbackUsed);
        Assert.Equal(Freshness.Unknown, result.Value.Freshness);
    }

    [Fact]
    public void Resolve_RequestedRoleNotFound_ReturnsRequestedRoleNotFoundDomainError()
    {
        var missingRole = new RoleId("MissingRole");
        var request = new NodeContentResolutionRequest(
            TestNodeId,
            missingRole,
            Snapshot,
            Array.Empty<Role>(),
            Array.Empty<RoleResolution>(),
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleNotFound, result.Error!.Code);
        Assert.Equal(missingRole.ToString(), result.Error.Details[RoleResolutionErrorCodes.RequestedRoleIdDetail]);
    }

    [Fact]
    public void Resolve_RequestedRoleDeleted_ReturnsRequestedRoleDeletedDomainError()
    {
        var roles = new[] { new Role(Snapshot, RoleDeveloper, "Developer", null, true) };
        var request = new NodeContentResolutionRequest(
            TestNodeId,
            RoleDeveloper,
            Snapshot,
            roles,
            Array.Empty<RoleResolution>(),
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleDeleted, result.Error!.Code);
        Assert.Equal(RoleDeveloper.ToString(), result.Error.Details[RoleResolutionErrorCodes.RequestedRoleIdDetail]);
    }

    [Fact]
    public void Resolve_CandidateRoleNotFound_ReturnsCandidateRoleNotFoundDomainError()
    {
        var roles = new[] { new Role(Snapshot, RoleDeveloper, "Developer", null, false) };
        var missingCandidate = new RoleId("MissingCandidate");
        var resolutions = new[] { new RoleResolution(Snapshot, RoleDeveloper, missingCandidate, 1) };

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            RoleDeveloper,
            Snapshot,
            roles,
            resolutions,
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.CandidateRoleNotFound, result.Error!.Code);
        Assert.Equal(missingCandidate.ToString(), result.Error.Details[RoleResolutionErrorCodes.CandidateRoleIdDetail]);
    }

    [Fact]
    public void Resolve_CandidateRoleDeleted_ReturnsCandidateRoleDeletedDomainError()
    {
        var roles = new[]
        {
            new Role(Snapshot, RoleDeveloper, "Developer", null, false),
            new Role(Snapshot, RoleArchived, "Archived", null, true)
        };
        var resolutions = new[] { new RoleResolution(Snapshot, RoleDeveloper, RoleArchived, 1) };

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            RoleDeveloper,
            Snapshot,
            roles,
            resolutions,
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.CandidateRoleDeleted, result.Error!.Code);
        Assert.Equal(RoleArchived.ToString(), result.Error.Details[RoleResolutionErrorCodes.CandidateRoleIdDetail]);
    }

    [Fact]
    public void Resolve_InvalidPriority_ReturnsInvalidPriorityDomainError()
    {
        var roles = new[] { new Role(Snapshot, RoleDeveloper, "Developer", null, false) };
        var resolutions = new[] { new RoleResolution(Snapshot, RoleDeveloper, RoleDeveloper, 0) };

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            RoleDeveloper,
            Snapshot,
            roles,
            resolutions,
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.InvalidPriority, result.Error!.Code);
    }

    [Fact]
    public void Resolve_DuplicateCandidateRole_ReturnsDuplicateCandidateRoleDomainError()
    {
        var roles = new[] { new Role(Snapshot, RoleDeveloper, "Developer", null, false) };
        var resolutions = new[]
        {
            new RoleResolution(Snapshot, RoleDeveloper, RoleDeveloper, 1),
            new RoleResolution(Snapshot, RoleDeveloper, RoleDeveloper, 2)
        };

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            RoleDeveloper,
            Snapshot,
            roles,
            resolutions,
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.DuplicateCandidateRole, result.Error!.Code);
    }

    [Fact]
    public void Resolve_DuplicatePriority_ReturnsDuplicatePriorityDomainError()
    {
        var roles = new[]
        {
            new Role(Snapshot, RoleDeveloper, "Developer", null, false),
            new Role(Snapshot, RoleConsultant, "Consultant", null, false)
        };
        var resolutions = new[]
        {
            new RoleResolution(Snapshot, RoleDeveloper, RoleDeveloper, 1),
            new RoleResolution(Snapshot, RoleDeveloper, RoleConsultant, 1)
        };

        var request = new NodeContentResolutionRequest(
            TestNodeId,
            RoleDeveloper,
            Snapshot,
            roles,
            resolutions,
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.DuplicatePriority, result.Error!.Code);
    }

    [Fact]
    public void Resolve_NullNodeId_WithValidRole_ReturnsAvailabilityNoneAndUnknownFreshness()
    {
        var roles = new[] { new Role(Snapshot, RoleDeveloper, "Developer", null, false) };
        var resolutions = new[] { new RoleResolution(Snapshot, RoleDeveloper, RoleDeveloper, 1) };

        var request = new NodeContentResolutionRequest(
            null,
            RoleDeveloper,
            Snapshot,
            roles,
            resolutions,
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Content);
        Assert.Equal(RoleDeveloper, result.Value.RequestedRole);
        Assert.Null(result.Value.ResolvedRole);
        Assert.Equal(Availability.None, result.Value.Availability);
        Assert.False(result.Value.FallbackUsed);
        Assert.Equal(Freshness.Unknown, result.Value.Freshness);
    }

    [Fact]
    public void Resolve_NullNodeId_WithMissingRequestedRole_ReturnsRequestedRoleNotFoundDomainError()
    {
        var missingRole = new RoleId("MissingRole");
        var request = new NodeContentResolutionRequest(
            null,
            missingRole,
            Snapshot,
            Array.Empty<Role>(),
            Array.Empty<RoleResolution>(),
            Array.Empty<NodeContent>(),
            Array.Empty<ContentDependency>());

        var result = NodeContentResolver.Resolve(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.RequestedRoleNotFound, result.Error!.Code);
        Assert.Equal(missingRole.ToString(), result.Error.Details[RoleResolutionErrorCodes.RequestedRoleIdDetail]);
    }
}
