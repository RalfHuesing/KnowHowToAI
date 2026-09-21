using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
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
    private static readonly AudienceId AudienceDeveloper = new("Developer");
    private static readonly AudienceId AudienceEndUser = new("EndUser");

    [Fact]
    public async Task ReplaceContent_Independent_CreatesExplicitContentWithFreshnessCurrent()
    {
        var tools = CreateTools(StateWithNodeAndAudience());

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), AudienceDeveloper.Value, "Independent", "Inhalt ohne Struktur.", 0);

        Assert.True(envelope.IsSuccess);
        Assert.Equal(NodeId.ToString(), envelope.Data!.NodeId);
        Assert.Equal(AudienceDeveloper.Value, envelope.Data.AudienceId);
        Assert.Equal(nameof(ContentMode.Independent), envelope.Data.ContentMode);
        Assert.Equal(nameof(Freshness.Current), envelope.Data.Freshness);
        Assert.Equal(SnapshotId.ToString(), envelope.Data.SnapshotId);
        Assert.Equal(1, envelope.Data.ChangeVersion);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Data.ContentRevisionId));
    }

    [Fact]
    public async Task ReplaceContent_StaleChangeVersion_IsRejectedWithStableDetails()
    {
        var repository = StateWithNodeAudienceAndDeveloperContent("Alt");
        var tools = CreateTools(repository);

        var current = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), AudienceDeveloper.Value, "Independent", "Aktuell", expectedChangeVersion: 0);
        var stale = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), AudienceDeveloper.Value, "Independent", "Veraltet", expectedChangeVersion: 0);

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
        var tools = CreateTools(StateWithNodeAudienceAndSourceContent());

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(),
            NodeId.ToString(),
            AudienceEndUser.Value,
            "Derived",
            "Abgeleiteter Inhalt.",
            0,
            [new McpContentSourceData(SourceNodeId.ToString(), AudienceDeveloper.Value, SourceRevisionId.ToString())]);

        Assert.True(envelope.IsSuccess);
        Assert.Equal(nameof(ContentMode.Derived), envelope.Data!.ContentMode);
        Assert.Equal(nameof(Freshness.Current), envelope.Data.Freshness);
    }

    [Fact]
    public async Task ReplaceContent_WithMarkdownHeading_IsRejectedWithStableHeadingNotAllowed()
    {
        var tools = CreateTools(StateWithNodeAndAudience());

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), AudienceDeveloper.Value, "Independent", "# Überschrift", 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(ContentStructureCodes.HeadingNotAllowed, envelope.Code);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task ReplaceContent_StandaloneTitleParagraph_KeepsSuccessWithEmbeddedHeadingWarning()
    {
        var tools = CreateTools(StateWithNodeAndAudience());

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), AudienceDeveloper.Value, "Independent", "Titel", 0);

        Assert.True(envelope.IsSuccess);
        Assert.Contains(
            envelope.Warnings!,
            warning => warning.Code == ContentStructureCodes.PossibleEmbeddedHeading);
    }

    [Theory]
    [InlineData("<span>unsicher</span>", ContentStructureCodes.RawHtmlNotAllowed)]
    [InlineData("[Link](http://example.test)", ContentStructureCodes.LinkTargetNotAllowed)]
    [InlineData("![Bild](https://example.test/image.png)", ContentStructureCodes.ExternalImageNotAllowed)]
    public async Task ReplaceContent_UnsafeContentPolicy_IsRejectedByMcpWithoutMutatingState(
        string content,
        string expectedCode)
    {
        var repository = StateWithNodeAndAudience();
        var tools = CreateTools(repository);

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), AudienceDeveloper.Value, "Independent", content, 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(expectedCode, envelope.Code);
        Assert.Empty(repository.State.Contents);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("independent")]
    [InlineData("")]
    public async Task ReplaceContent_UnknownContentMode_IsRejectedAsInvalidDependency(string rawContentMode)
    {
        var tools = CreateTools(StateWithNodeAndAudience());

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), AudienceDeveloper.Value, rawContentMode, "Inhalt", 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(DependencyErrorCodes.InvalidDependency, envelope.Code);
        Assert.Equal(rawContentMode, envelope.Details!["contentMode"]);
    }

    [Fact]
    public async Task ReplaceContent_MalformedSourceNodeId_IsRejectedAsInvalidDependency()
    {
        var tools = CreateTools(StateWithNodeAndAudience());

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(),
            NodeId.ToString(),
            AudienceEndUser.Value,
            "Derived",
            "Abgeleiteter Inhalt.",
            0,
            [new McpContentSourceData("not-a-guid", AudienceDeveloper.Value, SourceRevisionId.ToString())]);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(DependencyErrorCodes.InvalidDependency, envelope.Code);
        Assert.Equal("not-a-guid", envelope.Details![DependencyErrorCodes.SourceNodeIdDetail]);
    }

    [Fact]
    public async Task ReplaceContent_UnknownNode_ReturnsStableNodeNotFound()
    {
        var tools = CreateTools(EmptyState());

        var envelope = await tools.ReplaceContent(
            TransactionId.ToString(), NodeId.ToString(), AudienceDeveloper.Value, "Independent", "Inhalt", 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(HierarchyErrorCodes.NodeNotFound, envelope.Code);
        Assert.Equal(NodeId.ToString(), envelope.Details![HierarchyErrorCodes.NodeIdDetail]);
    }

    [Fact]
    public async Task ReplaceText_ReplacesSingleOccurrenceAndKeepsFreshnessCurrent()
    {
        var repository = StateWithNodeAudienceAndDeveloperContent("Inhalt mit Suchbegriff.");
        var tools = CreateTools(repository);

        var envelope = await tools.ReplaceText(
            TransactionId.ToString(), NodeId.ToString(), AudienceDeveloper.Value, "Suchbegriff", "Ersatz", 0);

        Assert.True(envelope.IsSuccess);
        Assert.Equal(nameof(Freshness.Current), envelope.Data!.Freshness);
        Assert.Equal(nameof(ContentMode.Independent), envelope.Data.ContentMode);
        Assert.Contains("Ersatz", repository.State.Contents.Single().ContentMd);
    }

    [Fact]
    public async Task ReplaceText_WithoutMatch_ReturnsStableTextNotFound()
    {
        var tools = CreateTools(StateWithNodeAudienceAndDeveloperContent("Inhalt."));

        var envelope = await tools.ReplaceText(
            TransactionId.ToString(), NodeId.ToString(), AudienceDeveloper.Value, "Fehlt", "Ersatz", 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(TextOperationCodes.TextNotFound, envelope.Code);
    }

    [Fact]
    public async Task ReplaceText_WithMultipleMatches_ReturnsStableMultipleTextMatches()
    {
        var tools = CreateTools(StateWithNodeAudienceAndDeveloperContent("Doppelt Doppelt."));

        var envelope = await tools.ReplaceText(
            TransactionId.ToString(), NodeId.ToString(), AudienceDeveloper.Value, "Doppelt", "Einfach", 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(TextOperationCodes.MultipleTextMatches, envelope.Code);
    }

    [Fact]
    public async Task ReplaceText_WithoutExplicitContent_ReturnsStableExplicitContentNotFound()
    {
        var tools = CreateTools(StateWithNodeAndAudience());

        var envelope = await tools.ReplaceText(
            TransactionId.ToString(), NodeId.ToString(), AudienceDeveloper.Value, "Alt", "Neu", 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(TextOperationCodes.ExplicitContentNotFound, envelope.Code);
        Assert.Equal(NodeId.ToString(), envelope.Details![TextOperationCodes.NodeIdDetail]);
    }

    [Fact]
    public async Task DeleteContent_TombstonesOnlyTheRequestedAudienceContent()
    {
        var repository = StateWithNodeAudienceAndTwoAudienceContents();
        var tools = CreateTools(repository);

        var envelope = await tools.DeleteContent(
            TransactionId.ToString(), NodeId.ToString(), AudienceDeveloper.Value, 0);

        Assert.True(envelope.IsSuccess);
        Assert.Equal(nameof(Freshness.Unknown), envelope.Data!.Freshness);
        Assert.True(repository.State.Contents.Single(content => content.AudienceId == AudienceDeveloper).IsDeleted);
        Assert.False(repository.State.Contents.Single(content => content.AudienceId == AudienceEndUser).IsDeleted);
    }

    [Fact]
    public async Task DeleteContent_WithoutExplicitContent_ReturnsStableExplicitContentNotFound()
    {
        var tools = CreateTools(StateWithNodeAndAudience());

        var envelope = await tools.DeleteContent(
            TransactionId.ToString(), NodeId.ToString(), AudienceDeveloper.Value, 0);

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

    private static InMemoryContentMutationRepository StateWithNodeAudienceAndDeveloperContent(string contentMd) =>
        new(new WorkingContentMutationState(
            SnapshotId,
            [Node(NodeId)],
            [Audience(AudienceDeveloper), Audience(AudienceEndUser)],
            [Content(NodeId, AudienceDeveloper, contentMd)],
            []));

    private static InMemoryContentMutationRepository StateWithNodeAudienceAndSourceContent() =>
        new(new WorkingContentMutationState(
        SnapshotId,
        [Node(NodeId), Node(SourceNodeId)],
        [Audience(AudienceDeveloper), Audience(AudienceEndUser)],
        [Content(SourceNodeId, AudienceDeveloper, "Quelle.")],
        []));

    private static InMemoryContentMutationRepository StateWithNodeAudienceAndTwoAudienceContents() =>
        new(new WorkingContentMutationState(
        SnapshotId,
        [Node(NodeId)],
        [Audience(AudienceDeveloper), Audience(AudienceEndUser)],
        [Content(NodeId, AudienceDeveloper, "Developer-Inhalt."), Content(NodeId, AudienceEndUser, "EndUser-Inhalt.")],
        []));

    private static InMemoryContentMutationRepository StateWithNodeAndAudience() =>
        new(new WorkingContentMutationState(
        SnapshotId,
        [Node(NodeId)],
        [Audience(AudienceDeveloper), Audience(AudienceEndUser)],
        [],
        []));

    private static InMemoryContentMutationRepository EmptyState() => new(new WorkingContentMutationState(SnapshotId, [], [], [], []));

    private static Node Node(NodeId nodeId) =>
        new(SnapshotId, nodeId, null, "Titel", null, 0, IsDeleted: false);

    private static Audience Audience(AudienceId audienceId) =>
        new(SnapshotId, audienceId, audienceId.Value, null, IsDeleted: false);

    private static NodeContent Content(NodeId nodeId, AudienceId audienceId, string contentMd) =>
        new(SnapshotId, nodeId, audienceId, SourceRevisionId, ContentMode.Independent, contentMd, IsDeleted: false);

    private sealed class FixedIdentifierGenerator : IIdentifierGenerator
    {
        private static readonly ContentRevisionId GeneratedRevisionId =
            new(Guid.Parse("c5f1f15b-3edf-4c6f-9c45-5ce3c0a1f131"));

        public TransactionId CreateTransactionId() => throw new NotSupportedException();

        public NodeId CreateNodeId() => throw new NotSupportedException();

        public ContentRevisionId CreateContentRevisionId() => GeneratedRevisionId;
    }
}
