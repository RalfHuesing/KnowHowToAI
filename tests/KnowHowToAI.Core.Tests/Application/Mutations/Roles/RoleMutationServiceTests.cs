using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Tests.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.Mutations.Roles;

[Trait("Category", "Unit")]
public sealed class RoleMutationServiceTests
{
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly TransactionId TransactionId = new(Guid.Parse("3c7a4e9d-17bf-4317-9d78-f2ee938121c9"));
    private static readonly NodeId NodeId = new(Guid.Parse("42d48a41-742a-483c-9bb7-91995e09612e"));
    private static readonly ContentRevisionId ContentRevisionId = new(Guid.Parse("4e4675d3-20ec-4075-bfaf-6733069f4f3b"));
    private static readonly RoleId DefaultRoleId = new("Default");
    private static readonly RoleId DeveloperRoleId = new("Developer");
    private static readonly RoleId EndUserRoleId = new("EndUser");

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task CreateRoleAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutWriting(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var repository = new InMemoryRoleMutationRepository(State()) { Rejection = rejection };
        var service = new RoleMutationService(repository);

        var result = await service.CreateRoleAsync(TransactionId, "Developer", null, 0);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.Empty(repository.State.Roles);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task UpdateRoleAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var repository = new InMemoryRoleMutationRepository(State([Role(DefaultRoleId)])) { Rejection = rejection };
        var service = new RoleMutationService(repository);

        var result = await service.UpdateRoleAsync(TransactionId, new UpdateRoleMutationRequest(DefaultRoleId, "Neu", null, 0));

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.Equal(DefaultRoleId.ToString(), repository.State.Roles.Single().Name);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task DeleteRoleAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var repository = new InMemoryRoleMutationRepository(State([Role(DefaultRoleId)])) { Rejection = rejection };
        var service = new RoleMutationService(repository);

        var result = await service.DeleteRoleAsync(TransactionId, DefaultRoleId, 0);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.False(repository.State.Roles.Single().IsDeleted);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task SetRoleResolutionAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var initialResolution = new RoleResolution(SnapshotId, DefaultRoleId, DefaultRoleId, 1);
        var repository = new InMemoryRoleMutationRepository(
            State([Role(DefaultRoleId), Role(DeveloperRoleId)], [initialResolution]))
        {
            Rejection = rejection
        };
        var service = new RoleMutationService(repository);

        var result = await service.SetRoleResolutionAsync(TransactionId, DefaultRoleId, [DeveloperRoleId], 0);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.Equal([initialResolution], repository.State.Resolutions);
    }

    [Fact]
    public async Task UpdateRoleAsync_IdenticalValues_DoesNotIncrementChangeVersion()
    {
        var role = Role(DefaultRoleId, "Beschreibung");
        var repository = new InMemoryRoleMutationRepository(State([role]));
        var service = new RoleMutationService(repository);

        var result = await service.UpdateRoleAsync(TransactionId, new UpdateRoleMutationRequest(DefaultRoleId, DefaultRoleId.ToString(), "Beschreibung", 0));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, repository.ChangeVersion);
    }

    [Fact]
    public async Task SetRoleResolutionAsync_IdenticalOrder_DoesNotIncrementChangeVersion()
    {
        var resolution = new RoleResolution(SnapshotId, DefaultRoleId, DeveloperRoleId, 1);
        var repository = new InMemoryRoleMutationRepository(
            State([Role(DefaultRoleId), Role(DeveloperRoleId)], [resolution]));
        var service = new RoleMutationService(repository);

        var result = await service.SetRoleResolutionAsync(TransactionId, DefaultRoleId, [DeveloperRoleId], 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, repository.ChangeVersion);
    }

    [Fact]
    public async Task UpdateRoleAsync_ChangedValues_IncrementsChangeVersionExactlyOnce()
    {
        var role = Role(DefaultRoleId, "Alt");
        var repository = new InMemoryRoleMutationRepository(State([role]));
        var service = new RoleMutationService(repository);

        var result = await service.UpdateRoleAsync(TransactionId, new UpdateRoleMutationRequest(DefaultRoleId, DefaultRoleId.ToString(), "Neu", 0));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.ChangeVersion);
    }

    [Fact]
    public async Task CreateRoleAsync_ValidRole_NormalizesAndPersistsWorkingState()
    {
        var repository = new InMemoryRoleMutationRepository(State([Role(DefaultRoleId)]));
        var service = new RoleMutationService(repository);

        var result = await service.CreateRoleAsync(TransactionId, " Developer ", " Beschreibung ", 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(DeveloperRoleId, result.Value!.RoleId);
        Assert.Equal("Developer", result.Value.Name);
        Assert.Equal("Beschreibung", result.Value.Description);
        Assert.Equal(1, repository.ChangeVersion);
        Assert.Contains(repository.State.Roles, role => role == result.Value);
    }

    [Fact]
    public async Task CreateRoleAsync_PreviouslyDeletedRole_ReactivatesSinglePersistedRole()
    {
        var deletedRole = Role(DeveloperRoleId) with { IsDeleted = true };
        var repository = new InMemoryRoleMutationRepository(State([Role(DefaultRoleId), deletedRole]));
        var service = new RoleMutationService(repository);

        var result = await service.CreateRoleAsync(TransactionId, " Developer ", " Neu ", 0);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsDeleted);
        Assert.Equal("Neu", result.Value.Description);
        Assert.Equal(1, repository.State.Roles.Count(role => role.RoleId == DeveloperRoleId));
        Assert.Equal(result.Value, repository.State.Roles.Single(role => role.RoleId == DeveloperRoleId));
        Assert.Equal(1, repository.ChangeVersion);
    }

    [Fact]
    public async Task UpdateRoleAsync_ExistingRole_ChangesOnlyRequestedRole()
    {
        var repository = new InMemoryRoleMutationRepository(State([Role(DefaultRoleId), Role(DeveloperRoleId, "Alt")]));
        var service = new RoleMutationService(repository);

        var result = await service.UpdateRoleAsync(TransactionId, new UpdateRoleMutationRequest(DeveloperRoleId, " Entwickler ", " Neu ", 0));

        Assert.True(result.IsSuccess);
        Assert.Equal("Entwickler", result.Value!.Name);
        Assert.Equal("Neu", result.Value.Description);
        Assert.Equal("Default", repository.State.Roles.Single(role => role.RoleId == DefaultRoleId).Name);
        Assert.Equal(1, repository.ChangeVersion);
    }

    [Fact]
    public async Task SetRoleResolutionAsync_ReplacesOneCompleteOrderAndPreservesOtherOrders()
    {
        var preservedResolution = new RoleResolution(SnapshotId, EndUserRoleId, DefaultRoleId, 1);
        var repository = new InMemoryRoleMutationRepository(State(
            [Role(DefaultRoleId), Role(DeveloperRoleId), Role(EndUserRoleId)],
            [new RoleResolution(SnapshotId, DefaultRoleId, DefaultRoleId, 1), preservedResolution]));
        var service = new RoleMutationService(repository);

        var result = await service.SetRoleResolutionAsync(TransactionId, DefaultRoleId, [DeveloperRoleId, EndUserRoleId], 0);

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value!,
            resolution => Assert.Equal((DeveloperRoleId, 1), (resolution.CandidateRoleId, resolution.Priority)),
            resolution => Assert.Equal((EndUserRoleId, 2), (resolution.CandidateRoleId, resolution.Priority)));
        Assert.Equal(3, repository.State.Resolutions.Count);
        Assert.Contains(preservedResolution, repository.State.Resolutions);
        Assert.Equal(1, repository.ChangeVersion);
    }

    [Fact]
    public async Task DeleteRoleAsync_BlockingReferences_ReturnsDetailsWithoutChangingWorkingState()
    {
        var content = new NodeContent(SnapshotId, NodeId, DeveloperRoleId, ContentRevisionId, ContentMode.Independent, "Text", IsDeleted: false);
        var dependency = new ContentDependency(SnapshotId, NodeId, DefaultRoleId, NodeId, DeveloperRoleId, ContentRevisionId);
        var resolution = new RoleResolution(SnapshotId, DefaultRoleId, DeveloperRoleId, 1);
        var repository = new InMemoryRoleMutationRepository(State([Role(DefaultRoleId), Role(DeveloperRoleId)], [resolution], [content], [dependency]));
        var service = new RoleMutationService(repository);

        var result = await service.DeleteRoleAsync(TransactionId, DeveloperRoleId, 0);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoleMutationErrorCodes.RoleInUse, result.Code);
        Assert.Equal("1", result.Details[RoleMutationErrorCodes.BlockingContentCountDetail]);
        Assert.Equal("1", result.Details[RoleMutationErrorCodes.BlockingDependencyCountDetail]);
        Assert.Equal("1", result.Details[RoleMutationErrorCodes.BlockingResolutionCountDetail]);
        Assert.False(repository.State.Roles.Single(role => role.RoleId == DeveloperRoleId).IsDeleted);
        Assert.Equal(0, repository.ChangeVersion);
    }

    [Fact]
    public async Task DeleteRoleAsync_UnreferencedRole_TombstonesRole()
    {
        var repository = new InMemoryRoleMutationRepository(State([Role(DefaultRoleId), Role(DeveloperRoleId)]));
        var service = new RoleMutationService(repository);

        var result = await service.DeleteRoleAsync(TransactionId, DeveloperRoleId, 0);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsDeleted);
        Assert.True(repository.State.Roles.Single(role => role.RoleId == DeveloperRoleId).IsDeleted);
        Assert.Equal(1, repository.ChangeVersion);
    }

    [Fact]
    public async Task UpdateRoleAsync_StaleChangeVersion_IsRejectedWithoutChangingWorkingState()
    {
        var repository = new InMemoryRoleMutationRepository(State([Role(DefaultRoleId, "Alt")]));
        var service = new RoleMutationService(repository);

        var current = await service.UpdateRoleMutationAsync(
            TransactionId,
            new UpdateRoleMutationRequest(DefaultRoleId, DefaultRoleId.Value, "Aktuell", ExpectedChangeVersion: 0));
        var stale = await service.UpdateRoleMutationAsync(
            TransactionId,
            new UpdateRoleMutationRequest(DefaultRoleId, DefaultRoleId.Value, "Veraltet", ExpectedChangeVersion: 0));

        Assert.True(current.IsSuccess);
        Assert.False(stale.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.ChangeVersionConflict, stale.Code);
        Assert.Equal("0", stale.Details[TransactionValidationErrorCodes.ExpectedChangeVersionDetail]);
        Assert.Equal("1", stale.Details[TransactionValidationErrorCodes.ActualChangeVersionDetail]);
        Assert.Equal("Aktuell", repository.State.Roles.Single().Description);
        Assert.Equal(1, repository.ChangeVersion);
    }

    private static WorkingRoleMutationState State(
        IReadOnlyList<Role>? roles = null,
        IReadOnlyList<RoleResolution>? resolutions = null,
        IReadOnlyList<NodeContent>? contents = null,
        IReadOnlyList<ContentDependency>? dependencies = null) =>
        new(SnapshotId, roles ?? [], resolutions ?? [], contents ?? [], dependencies ?? []);

    private static Role Role(RoleId roleId, string? description = null) =>
        new(SnapshotId, roleId, roleId.ToString(), description, IsDeleted: false);
}
