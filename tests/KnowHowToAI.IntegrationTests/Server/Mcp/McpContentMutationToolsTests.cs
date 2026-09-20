using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Server.Mcp.Contracts.Mutations.Content;
using KnowHowToAI.Server.Mcp.Tools.Mutations;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Handler-Vertragstests der Content-Tools (replace_content, replace_text,
/// delete_content): dünne Delegation an den ContentMutationApplicationService mit
/// protokollkonformer Error-Struktur, Revisions- und Freshness-Mapping sowie
/// Heading-/Text-Operation-Verträgen. Keine SQL- oder Server-Infrastruktur.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpContentMutationToolsTests
{
    private static readonly TransactionId TransactionId = new(Guid.Parse("0e3af35a-0e85-4f24-8ae9-7dd2b3d124b3"));
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly NodeId NodeId = new(Guid.Parse("30000000-0000-0000-0000-000000000001"));
    private static readonly NodeId SourceNodeId = new(Guid.Parse("30000000-0000-0000-0000-000000000002"));
    private static readonly ContentRevisionId SourceRevisionId =
        new(Guid.Parse("b4e0e04a-2dce-4b5e-8b34-4bd2b9f0e020"));
    private static readonly RoleId RoleDeveloper = new("Developer");
    private static readonly RoleId RoleEndUser = new("EndUser");

    [Fact]
    public async Task ReplaceContent_Independent_CreatesExplicitContentWithFreshnessCurrent()
    {
        var tools = CreateTools(StateWithNodeAndRole());

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), RoleDeveloper.Value, "Independent", "Inhalt ohne Struktur.");

        Assert.True(envelope.IsSuccess);
        Assert.Equal(NodeId.ToString(), envelope.Data!.NodeId);
        Assert.Equal(RoleDeveloper.Value, envelope.Data.RoleId);
        Assert.Equal(nameof(ContentMode.Independent), envelope.Data.ContentMode);
        Assert.Equal(nameof(Freshness.Current), envelope.Data.Freshness);
        Assert.Equal(SnapshotId.ToString(), envelope.Data.SnapshotId);
        Assert.Equal(1, envelope.Data.ChangeVersion);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Data.ContentRevisionId));
    }

    [Fact]
    public async Task ReplaceContent_StaleChangeVersion_IsRejectedWithStableDetails()
    {
        var repository = StateWithNodeRoleAndDeveloperContent("Alt");
        var tools = CreateTools(repository);

        var current = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), RoleDeveloper.Value, "Independent", "Aktuell", expectedChangeVersion: 0);
        var stale = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), RoleDeveloper.Value, "Independent", "Veraltet", expectedChangeVersion: 0);

        Assert.True(current.IsSuccess);
        Assert.False(stale.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.ChangeVersionConflict, stale.Code);
        Assert.Equal("0", stale.Details![TransactionValidationErrorCodes.ExpectedChangeVersionDetail]);
        Assert.Equal("1", stale.Details[TransactionValidationErrorCodes.ActualChangeVersionDetail]);
        Assert.Equal("Aktuell", repository.State.Contents.Single().ContentMd);
    }

    [Fact]
    public async Task ReplaceContent_DerivedWithSource_MapsDerivedContentAndKeepsSourceFreshness()
    {
        var tools = CreateTools(StateWithNodeRoleAndSourceContent());

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(),
            NodeId.ToString(),
            RoleEndUser.Value,
            "Derived",
            "Abgeleiteter Inhalt.",
            [new McpContentSourceData(SourceNodeId.ToString(), RoleDeveloper.Value, SourceRevisionId.ToString())]);

        Assert.True(envelope.IsSuccess);
        Assert.Equal(nameof(ContentMode.Derived), envelope.Data!.ContentMode);
        Assert.Equal(nameof(Freshness.Current), envelope.Data.Freshness);
    }

    [Fact]
    public async Task ReplaceContent_WithMarkdownHeading_IsRejectedWithStableHeadingNotAllowed()
    {
        var tools = CreateTools(StateWithNodeAndRole());

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), RoleDeveloper.Value, "Independent", "# Überschrift");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(ContentStructureCodes.HeadingNotAllowed, envelope.Code);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task ReplaceContent_StandaloneTitleParagraph_KeepsSuccessWithEmbeddedHeadingWarning()
    {
        var tools = CreateTools(StateWithNodeAndRole());

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), RoleDeveloper.Value, "Independent", "Titel");

        Assert.True(envelope.IsSuccess);
        Assert.Contains(
            envelope.Warnings!,
            warning => warning.Code == ContentStructureCodes.PossibleEmbeddedHeading);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("independent")]
    [InlineData("")]
    public async Task ReplaceContent_UnknownContentMode_IsRejectedAsInvalidDependency(string rawContentMode)
    {
        var tools = CreateTools(StateWithNodeAndRole());

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), RoleDeveloper.Value, rawContentMode, "Inhalt");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(DependencyErrorCodes.InvalidDependency, envelope.Code);
        Assert.Equal(rawContentMode, envelope.Details!["contentMode"]);
    }

    [Fact]
    public async Task ReplaceContent_MalformedSourceNodeId_IsRejectedAsInvalidDependency()
    {
        var tools = CreateTools(StateWithNodeAndRole());

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(),
            NodeId.ToString(),
            RoleEndUser.Value,
            "Derived",
            "Abgeleiteter Inhalt.",
            [new McpContentSourceData("not-a-guid", RoleDeveloper.Value, SourceRevisionId.ToString())]);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(DependencyErrorCodes.InvalidDependency, envelope.Code);
        Assert.Equal("not-a-guid", envelope.Details![DependencyErrorCodes.SourceNodeIdDetail]);
    }

    [Fact]
    public async Task ReplaceContent_UnknownNode_ReturnsStableNodeNotFound()
    {
        var tools = CreateTools(EmptyState());

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), RoleDeveloper.Value, "Independent", "Inhalt");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(HierarchyErrorCodes.NodeNotFound, envelope.Code);
        Assert.Equal(NodeId.ToString(), envelope.Details![HierarchyErrorCodes.NodeIdDetail]);
    }

    [Fact]
    public async Task ReplaceText_ReplacesSingleOccurrenceAndKeepsFreshnessCurrent()
    {
        var repository = StateWithNodeRoleAndDeveloperContent("Inhalt mit Suchbegriff.");
        var tools = CreateTools(repository);

        var envelope = await tools.ReplaceText(
            TransactionId.ToString(), NodeId.ToString(), RoleDeveloper.Value, "Suchbegriff", "Ersatz");

        Assert.True(envelope.IsSuccess);
        Assert.Equal(nameof(Freshness.Current), envelope.Data!.Freshness);
        Assert.Equal(nameof(ContentMode.Independent), envelope.Data.ContentMode);
        Assert.Contains("Ersatz", repository.State.Contents.Single().ContentMd);
    }

    [Fact]
    public async Task ReplaceText_WithoutMatch_ReturnsStableTextNotFound()
    {
        var tools = CreateTools(StateWithNodeRoleAndDeveloperContent("Inhalt."));

        var envelope = await tools.ReplaceText(
            TransactionId.ToString(), NodeId.ToString(), RoleDeveloper.Value, "Fehlt", "Ersatz");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(TextOperationCodes.TextNotFound, envelope.Code);
    }

    [Fact]
    public async Task ReplaceText_WithMultipleMatches_ReturnsStableMultipleTextMatches()
    {
        var tools = CreateTools(StateWithNodeRoleAndDeveloperContent("Doppelt Doppelt."));

        var envelope = await tools.ReplaceText(
            TransactionId.ToString(), NodeId.ToString(), RoleDeveloper.Value, "Doppelt", "Einfach");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(TextOperationCodes.MultipleTextMatches, envelope.Code);
    }

    [Fact]
    public async Task ReplaceText_WithoutExplicitContent_ReturnsStableExplicitContentNotFound()
    {
        var tools = CreateTools(StateWithNodeAndRole());

        var envelope = await tools.ReplaceText(
            TransactionId.ToString(), NodeId.ToString(), RoleDeveloper.Value, "Alt", "Neu");

        Assert.False(envelope.IsSuccess);
        Assert.Equal(TextOperationCodes.ExplicitContentNotFound, envelope.Code);
        Assert.Equal(NodeId.ToString(), envelope.Details![TextOperationCodes.NodeIdDetail]);
    }

    [Fact]
    public async Task DeleteContent_TombstonesOnlyTheRequestedRoleContent()
    {
        var repository = StateWithNodeRoleAndTwoRoleContents();
        var tools = CreateTools(repository);

        var envelope = await tools.DeleteContent(
            TransactionId.ToString(), NodeId.ToString(), RoleDeveloper.Value);

        Assert.True(envelope.IsSuccess);
        Assert.Equal(nameof(Freshness.Unknown), envelope.Data!.Freshness);
        Assert.True(repository.State.Contents.Single(content => content.RoleId == RoleDeveloper).IsDeleted);
        Assert.False(repository.State.Contents.Single(content => content.RoleId == RoleEndUser).IsDeleted);
    }

    [Fact]
    public async Task DeleteContent_WithoutExplicitContent_ReturnsStableExplicitContentNotFound()
    {
        var tools = CreateTools(StateWithNodeAndRole());

        var envelope = await tools.DeleteContent(
            TransactionId.ToString(), NodeId.ToString(), RoleDeveloper.Value);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(TextOperationCodes.ExplicitContentNotFound, envelope.Code);
    }

    private static ContentMutationTools CreateTools(InMemoryContentMutationRepository repository) =>
        new(new ContentMutationApplicationService(
            repository,
            new ContentMutationService(new ContentRevisionService(new FixedIdentifierGenerator())),
            new ValidationPolicy
            {
                ContentSizeWarningBytes = 4096,
                ChildCountWarning = 2,
                HierarchyDepthWarning = 8,
                PossibleEmbeddedHeadingWarning = true
            }));

    private static InMemoryContentMutationRepository StateWithNodeRoleAndDeveloperContent(string contentMd) =>
        new(new WorkingContentMutationState(
            SnapshotId,
            [Node(NodeId)],
            [Role(RoleDeveloper), Role(RoleEndUser)],
            [Content(NodeId, RoleDeveloper, contentMd)],
            []));

    private static InMemoryContentMutationRepository StateWithNodeRoleAndSourceContent() =>
        new(new WorkingContentMutationState(
        SnapshotId,
        [Node(NodeId), Node(SourceNodeId)],
        [Role(RoleDeveloper), Role(RoleEndUser)],
        [Content(SourceNodeId, RoleDeveloper, "Quelle.")],
        []));

    private static InMemoryContentMutationRepository StateWithNodeRoleAndTwoRoleContents() =>
        new(new WorkingContentMutationState(
        SnapshotId,
        [Node(NodeId)],
        [Role(RoleDeveloper), Role(RoleEndUser)],
        [Content(NodeId, RoleDeveloper, "Developer-Inhalt."), Content(NodeId, RoleEndUser, "EndUser-Inhalt.")],
        []));

    private static InMemoryContentMutationRepository StateWithNodeAndRole() =>
        new(new WorkingContentMutationState(
        SnapshotId,
        [Node(NodeId)],
        [Role(RoleDeveloper), Role(RoleEndUser)],
        [],
        []));

    private static InMemoryContentMutationRepository EmptyState() => new(new WorkingContentMutationState(SnapshotId, [], [], [], []));

    private static Node Node(NodeId nodeId) =>
        new(SnapshotId, nodeId, null, "Titel", null, 0, IsDeleted: false);

    private static Role Role(RoleId roleId) =>
        new(SnapshotId, roleId, roleId.Value, null, IsDeleted: false);

    private static NodeContent Content(NodeId nodeId, RoleId roleId, string contentMd) =>
        new(SnapshotId, nodeId, roleId, SourceRevisionId, ContentMode.Independent, contentMd, IsDeleted: false);

    private sealed class FixedIdentifierGenerator : IIdentifierGenerator
    {
        private static readonly ContentRevisionId GeneratedRevisionId =
            new(Guid.Parse("c5f1f15b-3edf-4c6f-9c45-5ce3c0a1f131"));

        public TransactionId CreateTransactionId() => throw new NotSupportedException();

        public NodeId CreateNodeId() => throw new NotSupportedException();

        public ContentRevisionId CreateContentRevisionId() => GeneratedRevisionId;
    }
}
