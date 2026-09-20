using KnowHowToAI.Core.Application.Mutations.Audiences;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Tests.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.TestSupport;

namespace KnowHowToAI.Core.Tests.Application.Mutations.Audiences;

[Trait("Category", "Unit")]
public sealed class AudienceMutationServiceTests
{
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly TransactionId TransactionId = new(Guid.Parse("3c7a4e9d-17bf-4317-9d78-f2ee938121c9"));
    private static readonly NodeId NodeId = new(Guid.Parse("42d48a41-742a-483c-9bb7-91995e09612e"));
    private static readonly ContentRevisionId ContentRevisionId = new(Guid.Parse("4e4675d3-20ec-4075-bfaf-6733069f4f3b"));
    private static readonly AudienceId DefaultAudienceId = new("Default");
    private static readonly AudienceId DeveloperAudienceId = new("Developer");
    private static readonly AudienceId EndUserAudienceId = new("EndUser");

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task CreateAudienceAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutWriting(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var repository = new InMemoryAudienceMutationRepository(State()) { Rejection = rejection };
        var service = new AudienceMutationService(repository);

        var result = await service.CreateAudienceAsync(TransactionId, "Developer", null, 0);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.Empty(repository.State.Audiences);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task UpdateAudienceAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var repository = new InMemoryAudienceMutationRepository(State([Audience(DefaultAudienceId)])) { Rejection = rejection };
        var service = new AudienceMutationService(repository);

        var result = await service.UpdateAudienceAsync(TransactionId, new UpdateAudienceMutationRequest(DefaultAudienceId, "Neu", null, 0));

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.Equal(DefaultAudienceId.ToString(), repository.State.Audiences.Single().Name);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task DeleteAudienceAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var repository = new InMemoryAudienceMutationRepository(State([Audience(DefaultAudienceId)])) { Rejection = rejection };
        var service = new AudienceMutationService(repository);

        var result = await service.DeleteAudienceAsync(TransactionId, DefaultAudienceId, 0);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.False(repository.State.Audiences.Single().IsDeleted);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task SetAudienceResolutionAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var initialResolution = new AudienceResolution(SnapshotId, DefaultAudienceId, DefaultAudienceId, 1);
        var repository = new InMemoryAudienceMutationRepository(
            State([Audience(DefaultAudienceId), Audience(DeveloperAudienceId)], [initialResolution]))
        {
            Rejection = rejection
        };
        var service = new AudienceMutationService(repository);

        var result = await service.SetAudienceResolutionAsync(TransactionId, DefaultAudienceId, [DeveloperAudienceId], 0);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.Equal([initialResolution], repository.State.Resolutions);
    }

    [Fact]
    public async Task UpdateAudienceAsync_IdenticalValues_DoesNotIncrementChangeVersion()
    {
        var audience = Audience(DefaultAudienceId, "Beschreibung");
        var repository = new InMemoryAudienceMutationRepository(State([audience]));
        var service = new AudienceMutationService(repository);

        var result = await service.UpdateAudienceAsync(TransactionId, new UpdateAudienceMutationRequest(DefaultAudienceId, DefaultAudienceId.ToString(), "Beschreibung", 0));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, repository.ChangeVersion);
    }

    [Fact]
    public async Task SetAudienceResolutionAsync_IdenticalOrder_DoesNotIncrementChangeVersion()
    {
        var resolution = new AudienceResolution(SnapshotId, DefaultAudienceId, DeveloperAudienceId, 1);
        var repository = new InMemoryAudienceMutationRepository(
            State([Audience(DefaultAudienceId), Audience(DeveloperAudienceId)], [resolution]));
        var service = new AudienceMutationService(repository);

        var result = await service.SetAudienceResolutionAsync(TransactionId, DefaultAudienceId, [DeveloperAudienceId], 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, repository.ChangeVersion);
    }

    [Fact]
    public async Task UpdateAudienceAsync_ChangedValues_IncrementsChangeVersionExactlyOnce()
    {
        var audience = Audience(DefaultAudienceId, "Alt");
        var repository = new InMemoryAudienceMutationRepository(State([audience]));
        var service = new AudienceMutationService(repository);

        var result = await service.UpdateAudienceAsync(TransactionId, new UpdateAudienceMutationRequest(DefaultAudienceId, DefaultAudienceId.ToString(), "Neu", 0));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.ChangeVersion);
    }

    [Fact]
    public async Task CreateAudienceAsync_ValidAudience_NormalizesAndPersistsWorkingState()
    {
        var repository = new InMemoryAudienceMutationRepository(State([Audience(DefaultAudienceId)]));
        var service = new AudienceMutationService(repository);

        var result = await service.CreateAudienceAsync(TransactionId, " Developer ", " Beschreibung ", 0);

        Assert.True(result.IsSuccess);
        Assert.Equal(DeveloperAudienceId, result.Value!.AudienceId);
        Assert.Equal("Developer", result.Value.Name);
        Assert.Equal("Beschreibung", result.Value.Description);
        Assert.Equal(1, repository.ChangeVersion);
        Assert.Contains(repository.State.Audiences, audience => audience == result.Value);
    }

    [Fact]
    public async Task CreateAudienceAsync_PreviouslyDeletedAudience_ReactivatesSinglePersistedAudience()
    {
        var deletedAudience = Audience(DeveloperAudienceId) with { IsDeleted = true };
        var repository = new InMemoryAudienceMutationRepository(State([Audience(DefaultAudienceId), deletedAudience]));
        var service = new AudienceMutationService(repository);

        var result = await service.CreateAudienceAsync(TransactionId, " Developer ", " Neu ", 0);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsDeleted);
        Assert.Equal("Neu", result.Value.Description);
        Assert.Equal(1, repository.State.Audiences.Count(audience => audience.AudienceId == DeveloperAudienceId));
        Assert.Equal(result.Value, repository.State.Audiences.Single(audience => audience.AudienceId == DeveloperAudienceId));
        Assert.Equal(1, repository.ChangeVersion);
    }

    [Fact]
    public async Task UpdateAudienceAsync_ExistingAudience_ChangesOnlyRequestedAudience()
    {
        var repository = new InMemoryAudienceMutationRepository(State([Audience(DefaultAudienceId), Audience(DeveloperAudienceId, "Alt")]));
        var service = new AudienceMutationService(repository);

        var result = await service.UpdateAudienceAsync(TransactionId, new UpdateAudienceMutationRequest(DeveloperAudienceId, " Entwickler ", " Neu ", 0));

        Assert.True(result.IsSuccess);
        Assert.Equal("Entwickler", result.Value!.Name);
        Assert.Equal("Neu", result.Value.Description);
        Assert.Equal("Default", repository.State.Audiences.Single(audience => audience.AudienceId == DefaultAudienceId).Name);
        Assert.Equal(1, repository.ChangeVersion);
    }

    [Fact]
    public async Task SetAudienceResolutionAsync_ReplacesOneCompleteOrderAndPreservesOtherOrders()
    {
        var preservedResolution = new AudienceResolution(SnapshotId, EndUserAudienceId, DefaultAudienceId, 1);
        var repository = new InMemoryAudienceMutationRepository(State(
            [Audience(DefaultAudienceId), Audience(DeveloperAudienceId), Audience(EndUserAudienceId)],
            [new AudienceResolution(SnapshotId, DefaultAudienceId, DefaultAudienceId, 1), preservedResolution]));
        var service = new AudienceMutationService(repository);

        var result = await service.SetAudienceResolutionAsync(TransactionId, DefaultAudienceId, [DeveloperAudienceId, EndUserAudienceId], 0);

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value!,
            resolution => Assert.Equal((DeveloperAudienceId, 1), (resolution.CandidateAudienceId, resolution.Priority)),
            resolution => Assert.Equal((EndUserAudienceId, 2), (resolution.CandidateAudienceId, resolution.Priority)));
        Assert.Equal(3, repository.State.Resolutions.Count);
        Assert.Contains(preservedResolution, repository.State.Resolutions);
        Assert.Equal(1, repository.ChangeVersion);
    }

    [Fact]
    public async Task DeleteAudienceAsync_BlockingReferences_ReturnsDetailsWithoutChangingWorkingState()
    {
        var content = new NodeContent(SnapshotId, NodeId, DeveloperAudienceId, ContentRevisionId, ContentMode.Independent, "Text", IsDeleted: false);
        var dependency = new ContentDependency(SnapshotId, NodeId, DefaultAudienceId, NodeId, DeveloperAudienceId, ContentRevisionId);
        var resolution = new AudienceResolution(SnapshotId, DefaultAudienceId, DeveloperAudienceId, 1);
        var repository = new InMemoryAudienceMutationRepository(State([Audience(DefaultAudienceId), Audience(DeveloperAudienceId)], [resolution], [content], [dependency]));
        var service = new AudienceMutationService(repository);

        var result = await service.DeleteAudienceAsync(TransactionId, DeveloperAudienceId, 0);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceMutationErrorCodes.AudienceInUse, result.Code);
        Assert.Equal("1", result.Details[AudienceMutationErrorCodes.BlockingContentCountDetail]);
        Assert.Equal("1", result.Details[AudienceMutationErrorCodes.BlockingDependencyCountDetail]);
        Assert.Equal("1", result.Details[AudienceMutationErrorCodes.BlockingResolutionCountDetail]);
        Assert.False(repository.State.Audiences.Single(audience => audience.AudienceId == DeveloperAudienceId).IsDeleted);
        Assert.Equal(0, repository.ChangeVersion);
    }

    [Fact]
    public async Task DeleteAudienceAsync_UnreferencedAudience_TombstonesAudience()
    {
        var repository = new InMemoryAudienceMutationRepository(State([Audience(DefaultAudienceId), Audience(DeveloperAudienceId)]));
        var service = new AudienceMutationService(repository);

        var result = await service.DeleteAudienceAsync(TransactionId, DeveloperAudienceId, 0);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsDeleted);
        Assert.True(repository.State.Audiences.Single(audience => audience.AudienceId == DeveloperAudienceId).IsDeleted);
        Assert.Equal(1, repository.ChangeVersion);
    }

    [Fact]
    public async Task UpdateAudienceAsync_DuplicateName_IsRejectedWithoutChangingWorkingState()
    {
        var repository = new InMemoryAudienceMutationRepository(State([Audience(DefaultAudienceId), Audience(DeveloperAudienceId)]));
        var service = new AudienceMutationService(repository);

        var result = await service.UpdateAudienceAsync(
            TransactionId,
            new UpdateAudienceMutationRequest(DeveloperAudienceId, DefaultAudienceId.Value, null, 0));

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceMutationErrorCodes.AudienceInUse, result.Code);
        Assert.Equal(DefaultAudienceId.Value, result.Details[AudienceMutationErrorCodes.AudienceIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.Equal("Developer", repository.State.Audiences.Single(audience => audience.AudienceId == DeveloperAudienceId).Name);
    }

    [Fact]
    public async Task CreateAudienceAsync_VisibleNameAlreadyUsedByDifferentAudienceId_IsRejectedWithoutChangingWorkingState()
    {
        var existing = new Audience(SnapshotId, new AudienceId("Developer"), "Foo", null, IsDeleted: false);
        var repository = new InMemoryAudienceMutationRepository(State([existing]));
        var service = new AudienceMutationService(repository);

        var result = await service.CreateAudienceAsync(TransactionId, " Foo ", null, 0);

        Assert.False(result.IsSuccess);
        Assert.Equal(AudienceMutationErrorCodes.AudienceInUse, result.Code);
        Assert.Equal("Developer", result.Details[AudienceMutationErrorCodes.AudienceIdDetail]);
        Assert.Single(repository.State.Audiences);
        Assert.Equal("Foo", repository.State.Audiences.Single().Name);
        Assert.Equal(0, repository.ChangeVersion);
    }

    [Fact]
    public async Task UpdateAudienceAsync_StaleChangeVersion_IsRejectedWithoutChangingWorkingState()
    {
        var repository = new InMemoryAudienceMutationRepository(State([Audience(DefaultAudienceId, "Alt")]));
        var service = new AudienceMutationService(repository);

        var current = await service.UpdateAudienceMutationAsync(
            TransactionId,
            new UpdateAudienceMutationRequest(DefaultAudienceId, DefaultAudienceId.Value, "Aktuell", ExpectedChangeVersion: 0));
        var stale = await service.UpdateAudienceMutationAsync(
            TransactionId,
            new UpdateAudienceMutationRequest(DefaultAudienceId, DefaultAudienceId.Value, "Veraltet", ExpectedChangeVersion: 0));

        Assert.True(current.IsSuccess);
        Assert.False(stale.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.ChangeVersionConflict, stale.Code);
        Assert.Equal("0", stale.Details[TransactionValidationErrorCodes.ExpectedChangeVersionDetail]);
        Assert.Equal("1", stale.Details[TransactionValidationErrorCodes.ActualChangeVersionDetail]);
        Assert.Equal("Aktuell", repository.State.Audiences.Single().Description);
        Assert.Equal(1, repository.ChangeVersion);
    }

    private static WorkingAudienceMutationState State(
        IReadOnlyList<Audience>? audiences = null,
        IReadOnlyList<AudienceResolution>? resolutions = null,
        IReadOnlyList<NodeContent>? contents = null,
        IReadOnlyList<ContentDependency>? dependencies = null) =>
        new(SnapshotId, audiences ?? [], resolutions ?? [], contents ?? [], dependencies ?? []);

    private static Audience Audience(AudienceId audienceId, string? description = null) =>
        new(SnapshotId, audienceId, audienceId.ToString(), description, IsDeleted: false);
}
