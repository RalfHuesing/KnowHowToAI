using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Tests.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Validation;

namespace KnowHowToAI.Core.Tests.Application.Mutations.Content;

[Trait("Category", "Unit")]
public sealed class ContentMutationApplicationServiceTests
{
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly TransactionId TransactionId = new(Guid.Parse("ad7ab10a-3781-4c5a-99f2-79e886a69ceb"));
    private static readonly NodeId NodeId = new(Guid.Parse("feabf4b0-54ee-4c6b-a51e-188d1bd2c517"));
    private static readonly RoleId DeveloperRoleId = new("Developer");
    private static readonly RoleId EndUserRoleId = new("EndUser");
    private static readonly ContentRevisionId SourceRevisionId = new(Guid.Parse("99d37e38-d746-41dc-a653-5680c1b2146b"));
    private static readonly ContentRevisionId ExistingDerivedRevisionId = new(Guid.Parse("da43f88f-9e7d-4e81-a59c-5505144a1567"));
    private static readonly ContentRevisionId GeneratedRevisionId = new(Guid.Parse("c5a915d2-c89e-413c-b36a-6d47bbc51610"));

    [Fact]
    public async Task ReplaceContentAsync_DerivedContent_NormalizesAssignsRevisionAndReturnsFreshnessAndWarnings()
    {
        var source = Content(DeveloperRoleId, SourceRevisionId, "Quelle");
        var repository = new InMemoryContentMutationRepository(State([source]));
        var service = CreateService(repository);

        var result = await service.ReplaceContentAsync(
            TransactionId,
            new ReplaceContentRequest(
                NodeId,
                EndUserRoleId,
                ContentMode.Derived,
                "Langer\r\nText",
                [new ContentDependencySource(NodeId, DeveloperRoleId, SourceRevisionId)]));

        Assert.True(result.IsSuccess);
        Assert.Equal(GeneratedRevisionId, result.Value!.Content.ContentRevisionId);
        Assert.Equal("Langer\nText", result.Value.Content.ContentMd);
        Assert.Equal(Freshness.Current, result.Value.Freshness);
        Assert.Equal(1, result.Value.ChangeVersion);
        Assert.Contains(result.Warnings, warning => warning.Code == QualityWarningCodes.NodeTooLarge);
        Assert.Single(repository.State.Dependencies);
    }

    [Fact]
    public async Task ReplaceContentAsync_StaleSourceRevision_IsRejectedWithoutChangingWorkingState()
    {
        var source = Content(DeveloperRoleId, SourceRevisionId, "Quelle");
        var repository = new InMemoryContentMutationRepository(State([source]));
        var service = CreateService(repository);

        var result = await service.ReplaceContentAsync(
            TransactionId,
            new ReplaceContentRequest(
                NodeId,
                EndUserRoleId,
                ContentMode.Derived,
                "Abgeleitet",
                [new ContentDependencySource(NodeId, DeveloperRoleId, GeneratedRevisionId)]));

        Assert.False(result.IsSuccess);
        Assert.Equal(DependencyErrorCodes.InvalidDependency, result.Code);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.Single(repository.State.Contents);
    }

    [Fact]
    public async Task ReplaceTextAsync_ChangesExplicitContentAndKeepsExistingProvenance()
    {
        var source = Content(DeveloperRoleId, SourceRevisionId, "Quelle");
        var derived = Content(EndUserRoleId, ExistingDerivedRevisionId, "Alt") with { ContentMode = ContentMode.Derived };
        var dependency = new ContentDependency(SnapshotId, NodeId, EndUserRoleId, NodeId, DeveloperRoleId, SourceRevisionId);
        var repository = new InMemoryContentMutationRepository(State([source, derived], [dependency]));
        var service = CreateService(repository);

        var result = await service.ReplaceTextAsync(
            TransactionId,
            new ReplaceTextRequest(NodeId, EndUserRoleId, "Alt", "Neu"));

        Assert.True(result.IsSuccess);
        Assert.Equal(GeneratedRevisionId, result.Value!.Content.ContentRevisionId);
        Assert.Equal("Neu", result.Value.Content.ContentMd);
        Assert.Equal(Freshness.Current, result.Value.Freshness);
        Assert.Equal([dependency], repository.State.Dependencies);
    }

    [Fact]
    public async Task DeleteContentAsync_TombstonesExplicitContentAndReturnsUnknownFreshness()
    {
        var explicitContent = Content(EndUserRoleId, GeneratedRevisionId, "Text");
        var repository = new InMemoryContentMutationRepository(State([explicitContent]));
        var service = CreateService(repository);

        var result = await service.DeleteContentAsync(TransactionId, NodeId, EndUserRoleId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Content.IsDeleted);
        Assert.Equal(Freshness.Unknown, result.Value.Freshness);
        Assert.True(Assert.Single(repository.State.Contents).IsDeleted);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task ReplaceContentAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var source = Content(DeveloperRoleId, SourceRevisionId, "Quelle");
        var repository = new InMemoryContentMutationRepository(State([source])) { Rejection = rejection };
        var service = CreateService(repository);

        var result = await service.ReplaceContentAsync(
            TransactionId,
            new ReplaceContentRequest(NodeId, EndUserRoleId, ContentMode.Independent, "Neu", []));

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.Single(repository.State.Contents);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task ReplaceTextAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var source = Content(DeveloperRoleId, SourceRevisionId, "Quelle");
        var repository = new InMemoryContentMutationRepository(State([source])) { Rejection = rejection };
        var service = CreateService(repository);

        var result = await service.ReplaceTextAsync(
            TransactionId,
            new ReplaceTextRequest(NodeId, DeveloperRoleId, "Quelle", "Neu"));

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.Equal("Quelle", repository.State.Contents.Single().ContentMd);
    }

    [Theory]
    [InlineData(TransactionValidationErrorCodes.TransactionNotFound)]
    [InlineData(TransactionValidationErrorCodes.TransactionClosed)]
    public async Task DeleteContentAsync_MissingOrClosedTransaction_ReturnsStableErrorWithoutChangingState(string errorCode)
    {
        var rejection = errorCode == TransactionValidationErrorCodes.TransactionNotFound
            ? TransactionTestErrors.NotFound(TransactionId)
            : TransactionTestErrors.Closed(TransactionId);
        var source = Content(DeveloperRoleId, SourceRevisionId, "Quelle");
        var repository = new InMemoryContentMutationRepository(State([source])) { Rejection = rejection };
        var service = CreateService(repository);

        var result = await service.DeleteContentAsync(TransactionId, NodeId, DeveloperRoleId);

        Assert.False(result.IsSuccess);
        Assert.Equal(errorCode, result.Code);
        Assert.Equal(TransactionId.ToString(), result.Details[TransactionValidationErrorCodes.TransactionIdDetail]);
        Assert.Equal(0, repository.ChangeVersion);
        Assert.False(repository.State.Contents.Single().IsDeleted);
    }

    [Fact]
    public async Task ReplaceContentAsync_IdenticalContent_ReusesRevisionAndKeepsChangeVersion()
    {
        var source = Content(DeveloperRoleId, SourceRevisionId, "Inhalt");
        var repository = new InMemoryContentMutationRepository(State([source]));
        var service = CreateService(repository);

        var result = await service.ReplaceContentAsync(
            TransactionId,
            new ReplaceContentRequest(NodeId, DeveloperRoleId, ContentMode.Independent, "Inhalt", []));

        Assert.True(result.IsSuccess);
        Assert.Equal(SourceRevisionId, result.Value!.Content.ContentRevisionId);
        Assert.Equal(0, result.Value.ChangeVersion);
        Assert.Equal(0, repository.ChangeVersion);
    }

    [Fact]
    public async Task ReplaceTextAsync_IdenticalReplacement_ReusesRevisionAndKeepsChangeVersion()
    {
        var source = Content(DeveloperRoleId, SourceRevisionId, "Inhalt");
        var repository = new InMemoryContentMutationRepository(State([source]));
        var service = CreateService(repository);

        var result = await service.ReplaceTextAsync(
            TransactionId,
            new ReplaceTextRequest(NodeId, DeveloperRoleId, "Inhalt", "Inhalt"));

        Assert.True(result.IsSuccess);
        Assert.Equal(SourceRevisionId, result.Value!.Content.ContentRevisionId);
        Assert.Equal(0, result.Value.ChangeVersion);
        Assert.Equal(0, repository.ChangeVersion);
    }

    [Fact]
    public async Task ReplaceContentAsync_DifferentContent_CreatesNewRevisionAndIncrementsChangeVersion()
    {
        var source = Content(DeveloperRoleId, SourceRevisionId, "Inhalt");
        var repository = new InMemoryContentMutationRepository(State([source]));
        var service = CreateService(repository);

        var result = await service.ReplaceContentAsync(
            TransactionId,
            new ReplaceContentRequest(NodeId, DeveloperRoleId, ContentMode.Independent, "Neuer Inhalt", []));

        Assert.True(result.IsSuccess);
        Assert.Equal(GeneratedRevisionId, result.Value!.Content.ContentRevisionId);
        Assert.Equal(1, result.Value.ChangeVersion);
        Assert.Equal(1, repository.ChangeVersion);
    }

    private static ContentMutationApplicationService CreateService(InMemoryContentMutationRepository repository) =>
        new(
            repository,
            new ContentMutationService(new ContentRevisionService(new FixedIdentifierGenerator())),
            new ValidationPolicy
            {
                ContentSizeWarningBytes = 8,
                ChildCountWarning = 25,
                HierarchyDepthWarning = 8,
                PossibleEmbeddedHeadingWarning = true
            });

    private static WorkingContentMutationState State(
        IReadOnlyList<NodeContent> contents,
        IReadOnlyList<ContentDependency>? dependencies = null) =>
        new(
            SnapshotId,
            [new Node(SnapshotId, NodeId, null, "Titel", null, 0, IsDeleted: false)],
            [
                new Role(SnapshotId, DeveloperRoleId, "Developer", null, IsDeleted: false),
                new Role(SnapshotId, EndUserRoleId, "EndUser", null, IsDeleted: false)
            ],
            contents,
            dependencies ?? []);

    private static NodeContent Content(RoleId roleId, ContentRevisionId revisionId, string text) =>
        new(SnapshotId, NodeId, roleId, revisionId, ContentMode.Independent, text, IsDeleted: false);

    private sealed class InMemoryContentMutationRepository(WorkingContentMutationState state) : IContentMutationRepository
    {
        public WorkingContentMutationState State { get; private set; } = state;

        public long ChangeVersion { get; private set; }

        public DomainError? Rejection { get; init; }

        public Task<Result<WorkingContentMutationExecution<T>>> ExecuteAsync<T>(
            TransactionId transactionId,
            Func<WorkingContentMutationState, Result<WorkingContentMutationDecision<T>>> mutate,
            CancellationToken cancellationToken = default)
        {
            if (Rejection is not null)
                return Task.FromResult(Result<WorkingContentMutationExecution<T>>.Failure(Rejection));

            var previousState = State;
            var decisionResult = mutate(previousState);
            if (!decisionResult.IsSuccess)
                return Task.FromResult(Result<WorkingContentMutationExecution<T>>.Failure(decisionResult.Error!, decisionResult.Warnings));

            var decision = decisionResult.Value!;
            var stateChanged = HasStateChanged(previousState, decision.State);
            State = decision.State;
            if (stateChanged)
                ChangeVersion++;

            return Task.FromResult(Result<WorkingContentMutationExecution<T>>.Success(
                new WorkingContentMutationExecution<T>(decision.Value, State.SnapshotId, ChangeVersion, previousState, State)));
        }

        private static bool HasStateChanged(WorkingContentMutationState before, WorkingContentMutationState after) =>
            !before.Contents.SequenceEqual(after.Contents)
            || !before.Dependencies.SequenceEqual(after.Dependencies);
    }

    private sealed class FixedIdentifierGenerator : IIdentifierGenerator
    {
        public TransactionId CreateTransactionId() => throw new NotSupportedException();

        public NodeId CreateNodeId() => throw new NotSupportedException();

        public ContentRevisionId CreateContentRevisionId() => GeneratedRevisionId;
    }
}
