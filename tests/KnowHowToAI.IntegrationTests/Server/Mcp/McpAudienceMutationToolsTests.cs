using KnowHowToAI.Core.Application.Mutations.Audiences;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Server.Mcp.Tools.Mutations;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Handler-Vertragstests der Zielgruppen-Tools (create_audience, update_audience, delete_audience,
/// set_audience_resolution): dünne Delegation an den AudienceMutationService mit
/// protokollkonformer Error-Struktur und vollständiger Resolution-Order-Antwort.
/// Keine SQL- oder Server-Infrastruktur.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpAudienceMutationToolsTests
{
    private static readonly TransactionId TransactionId = new(Guid.Parse("0e3af35a-0e85-4f24-8ae9-7dd2b3d124b3"));
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly AudienceId AudienceDeveloper = new("Developer");
    private static readonly AudienceId AudienceConsultant = new("Consultant");
    private static readonly NodeId NodeId = new(Guid.Parse("30000000-0000-0000-0000-000000000001"));
    private static readonly ContentRevisionId RevisionId =
        new(Guid.Parse("b4e0e04a-2dce-4b5e-8b34-4bd2b9f0e020"));

    [Fact]
    public async Task CreateAudience_MapsAudienceDataWithAudienceIdDerivedFromName()
    {
        var tools = CreateTools(EmptyState());

        var envelope = await tools.CreateAudience(TransactionId.ToString(), "Developer", 0, "Entwicklerzielgruppe");

        Assert.True(envelope.IsSuccess);
        Assert.Equal("Developer", envelope.Data!.AudienceId);
        Assert.Equal("Developer", envelope.Data.Name);
        Assert.Equal("Entwicklerzielgruppe", envelope.Data.Description);
        Assert.Equal(SnapshotId.ToString(), envelope.Data.SnapshotId);
        Assert.Equal(1, envelope.Data.ChangeVersion);
        Assert.Null(envelope.Message);
    }

    [Fact]
    public async Task UpdateAudience_StaleChangeVersion_IsRejectedWithStableDetails()
    {
        var repository = StateWithAudiences(AudienceDeveloper);
        var tools = CreateTools(repository);

        var current = await tools.UpdateAudience(
            TransactionId.ToString(), AudienceDeveloper.Value, "Aktuell", expectedChangeVersion: 0, description: null);
        var stale = await tools.UpdateAudience(
            TransactionId.ToString(), AudienceDeveloper.Value, "Veraltet", expectedChangeVersion: 0, description: null);

        Assert.True(current.IsSuccess);
        Assert.False(stale.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.ChangeVersionConflict, stale.Code);
        Assert.Equal("0", stale.Details![TransactionValidationErrorCodes.ExpectedChangeVersionDetail]);
        Assert.Equal("1", stale.Details[TransactionValidationErrorCodes.ActualChangeVersionDetail]);
        Assert.Equal("Aktuell", repository.State.Audiences.Single().Name);
    }

    [Fact]
    public async Task CreateAudience_WhitespaceName_IsRejectedWithStableAudienceNameRequired()
    {
        var tools = CreateTools(EmptyState());

        var envelope = await tools.CreateAudience(TransactionId.ToString(), "   ", 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal("AudienceNameRequired", envelope.Code);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task CreateAudience_WithExistingName_IsRejectedWithStableAudienceInUse()
    {
        var tools = CreateTools(StateWithAudiences(AudienceDeveloper));

        var envelope = await tools.CreateAudience(TransactionId.ToString(), "Developer", 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal("AudienceInUse", envelope.Code);
        Assert.Equal("Developer", envelope.Details!["audienceId"]);
    }

    [Fact]
    public async Task UpdateAudience_ChangesNameAndDescription()
    {
        var tools = CreateTools(StateWithAudiences(AudienceDeveloper));

        var envelope = await tools.UpdateAudience(
            TransactionId.ToString(), AudienceDeveloper.Value, "Entwickler", 0, "Neue Beschreibung");

        Assert.True(envelope.IsSuccess);
        Assert.Equal(AudienceDeveloper.Value, envelope.Data!.AudienceId);
        Assert.Equal("Entwickler", envelope.Data.Name);
        Assert.Equal("Neue Beschreibung", envelope.Data.Description);
    }

    [Fact]
    public async Task UpdateAudience_UnknownAudience_ReturnsStableAudienceNotFound()
    {
        var tools = CreateTools(EmptyState());

        var envelope = await tools.UpdateAudience(TransactionId.ToString(), "Fehlend", "Name", 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal("AudienceNotFound", envelope.Code);
        Assert.Equal("Fehlend", envelope.Details!["audienceId"]);
    }

    [Fact]
    public async Task DeleteAudience_WithBlockingContent_IsRejectedWithStableAudienceInUse()
    {
        var tools = CreateTools(StateWithAudiencesAndDeveloperContent());

        var envelope = await tools.DeleteAudience(TransactionId.ToString(), AudienceDeveloper.Value, 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal("AudienceInUse", envelope.Code);
        Assert.Equal("1", envelope.Details![AudienceMutationErrorCodes.BlockingContentCountDetail]);
    }

    [Fact]
    public async Task DeleteAudience_WithoutReferences_TombstonesTheAudience()
    {
        var repository = StateWithAudiences(AudienceDeveloper, AudienceConsultant);
        var tools = CreateTools(repository);

        var envelope = await tools.DeleteAudience(TransactionId.ToString(), AudienceDeveloper.Value, 0);

        Assert.True(envelope.IsSuccess);
        Assert.True(repository.State.Audiences.Single(audience => audience.AudienceId == AudienceDeveloper).IsDeleted);
        Assert.False(repository.State.Audiences.Single(audience => audience.AudienceId == AudienceConsultant).IsDeleted);
    }

    [Fact]
    public async Task SetAudienceResolution_MapsCandidatesInGivenOrderToPriorities()
    {
        var repository = StateWithAudiences(AudienceDeveloper, AudienceConsultant);
        var tools = CreateTools(repository);

        var envelope = await tools.SetAudienceResolution(
            TransactionId.ToString(),
            AudienceDeveloper.Value,
            [AudienceConsultant.Value, AudienceDeveloper.Value],
            0);

        Assert.True(envelope.IsSuccess);
        Assert.Equal(AudienceDeveloper.Value, envelope.Data!.RequestedAudienceId);
        Assert.Equal(2, envelope.Data.Items.Count);
        Assert.Equal(AudienceConsultant.Value, envelope.Data.Items[0].CandidateAudienceId);
        Assert.Equal(1, envelope.Data.Items[0].Priority);
        Assert.Equal(AudienceDeveloper.Value, envelope.Data.Items[1].CandidateAudienceId);
        Assert.Equal(2, envelope.Data.Items[1].Priority);
        Assert.Equal(2, repository.State.Resolutions.Count);
    }

    [Fact]
    public async Task SetAudienceResolution_WithDuplicateCandidate_ReturnsStableDuplicateCandidateAudience()
    {
        var tools = CreateTools(StateWithAudiences(AudienceDeveloper, AudienceConsultant));

        var envelope = await tools.SetAudienceResolution(
            TransactionId.ToString(),
            AudienceDeveloper.Value,
            [AudienceConsultant.Value, AudienceConsultant.Value],
            0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal("DuplicateCandidateAudience", envelope.Code);
        Assert.Equal(AudienceConsultant.Value, envelope.Details!["candidateAudienceId"]);
    }

    [Fact]
    public async Task SetAudienceResolution_WithUnknownCandidate_ReturnsStableCandidateAudienceNotFound()
    {
        var tools = CreateTools(StateWithAudiences(AudienceDeveloper));

        var envelope = await tools.SetAudienceResolution(
            TransactionId.ToString(),
            AudienceDeveloper.Value,
            ["Fehlend"],
            0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal("CandidateAudienceNotFound", envelope.Code);
        Assert.Equal("Fehlend", envelope.Details!["candidateAudienceId"]);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task CreateAudience_MalformedTransactionId_IsRejectedAsTransactionNotFound(string rawTransactionId)
    {
        var tools = CreateTools(EmptyState());

        var envelope = await tools.CreateAudience(rawTransactionId, "Developer", 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal("TransactionNotFound", envelope.Code);
    }

    private static AudienceMutationTools CreateTools(InMemoryAudienceMutationRepository repository) =>
        new(new AudienceMutationService(repository));

    private static InMemoryAudienceMutationRepository EmptyState() => new(new WorkingAudienceMutationState(SnapshotId, [], [], [], []));

    private static InMemoryAudienceMutationRepository StateWithAudiences(params AudienceId[] audienceIds) =>
        new(new WorkingAudienceMutationState(
        SnapshotId,
        audienceIds.Select(audienceId => new Audience(SnapshotId, audienceId, audienceId.Value, null, IsDeleted: false)).ToArray(),
        [],
        [],
        []));

    private static InMemoryAudienceMutationRepository StateWithAudiencesAndDeveloperContent() =>
        new(new WorkingAudienceMutationState(
        SnapshotId,
        [new Audience(SnapshotId, AudienceDeveloper, AudienceDeveloper.Value, null, IsDeleted: false)],
        [],
        [new NodeContent(
            SnapshotId, NodeId, AudienceDeveloper, RevisionId, ContentMode.Independent, "Inhalt", IsDeleted: false)],
        []));
}
