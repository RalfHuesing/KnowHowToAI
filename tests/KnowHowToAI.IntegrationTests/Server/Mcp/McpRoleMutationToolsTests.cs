using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.IntegrationTests.TestSupport;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Server.Mcp.Tools.Mutations;

namespace KnowHowToAI.IntegrationTests.Server.Mcp;

/// <summary>
/// Handler-Vertragstests der Rollen-Tools (create_role, update_role, delete_role,
/// set_role_resolution): dünne Delegation an den RoleMutationService mit
/// protokollkonformer Error-Struktur und vollständiger Resolution-Order-Antwort.
/// Keine SQL- oder Server-Infrastruktur.
/// </summary>
[Trait("Category", "Unit")]
public sealed class McpRoleMutationToolsTests
{
    private static readonly TransactionId TransactionId = new(Guid.Parse("0e3af35a-0e85-4f24-8ae9-7dd2b3d124b3"));
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly RoleId RoleDeveloper = new("Developer");
    private static readonly RoleId RoleConsultant = new("Consultant");
    private static readonly NodeId NodeId = new(Guid.Parse("30000000-0000-0000-0000-000000000001"));
    private static readonly ContentRevisionId RevisionId =
        new(Guid.Parse("b4e0e04a-2dce-4b5e-8b34-4bd2b9f0e020"));

    [Fact]
    public async Task CreateRole_MapsRoleDataWithRoleIdDerivedFromName()
    {
        var tools = CreateTools(EmptyState());

        var envelope = await tools.CreateRole(TransactionId.ToString(), "Developer", 0, "Entwicklerrolle");

        Assert.True(envelope.IsSuccess);
        Assert.Equal("Developer", envelope.Data!.RoleId);
        Assert.Equal("Developer", envelope.Data.Name);
        Assert.Equal("Entwicklerrolle", envelope.Data.Description);
        Assert.Equal(SnapshotId.ToString(), envelope.Data.SnapshotId);
        Assert.Equal(1, envelope.Data.ChangeVersion);
        Assert.Null(envelope.Message);
    }

    [Fact]
    public async Task UpdateRole_StaleChangeVersion_IsRejectedWithStableDetails()
    {
        var repository = StateWithRoles(RoleDeveloper);
        var tools = CreateTools(repository);

        var current = await tools.UpdateRole(
            TransactionId.ToString(), RoleDeveloper.Value, "Aktuell", expectedChangeVersion: 0, description: null);
        var stale = await tools.UpdateRole(
            TransactionId.ToString(), RoleDeveloper.Value, "Veraltet", expectedChangeVersion: 0, description: null);

        Assert.True(current.IsSuccess);
        Assert.False(stale.IsSuccess);
        Assert.Equal(TransactionValidationErrorCodes.ChangeVersionConflict, stale.Code);
        Assert.Equal("0", stale.Details![TransactionValidationErrorCodes.ExpectedChangeVersionDetail]);
        Assert.Equal("1", stale.Details[TransactionValidationErrorCodes.ActualChangeVersionDetail]);
        Assert.Equal("Aktuell", repository.State.Roles.Single().Name);
    }

    [Fact]
    public async Task CreateRole_WhitespaceName_IsRejectedWithStableRoleNameRequired()
    {
        var tools = CreateTools(EmptyState());

        var envelope = await tools.CreateRole(TransactionId.ToString(), "   ", 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(RoleMutationErrorCodes.RoleNameRequired, envelope.Code);
        Assert.Null(envelope.Data);
    }

    [Fact]
    public async Task CreateRole_WithExistingName_IsRejectedWithStableRoleInUse()
    {
        var tools = CreateTools(StateWithRoles(RoleDeveloper));

        var envelope = await tools.CreateRole(TransactionId.ToString(), "Developer", 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(RoleMutationErrorCodes.RoleInUse, envelope.Code);
        Assert.Equal("Developer", envelope.Details![RoleMutationErrorCodes.RoleIdDetail]);
    }

    [Fact]
    public async Task UpdateRole_ChangesNameAndDescription()
    {
        var tools = CreateTools(StateWithRoles(RoleDeveloper));

        var envelope = await tools.UpdateRole(
            TransactionId.ToString(), RoleDeveloper.Value, "Entwickler", 0, "Neue Beschreibung");

        Assert.True(envelope.IsSuccess);
        Assert.Equal(RoleDeveloper.Value, envelope.Data!.RoleId);
        Assert.Equal("Entwickler", envelope.Data.Name);
        Assert.Equal("Neue Beschreibung", envelope.Data.Description);
    }

    [Fact]
    public async Task UpdateRole_UnknownRole_ReturnsStableRoleNotFound()
    {
        var tools = CreateTools(EmptyState());

        var envelope = await tools.UpdateRole(TransactionId.ToString(), "Fehlend", "Name", 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(RoleMutationErrorCodes.RoleNotFound, envelope.Code);
        Assert.Equal("Fehlend", envelope.Details![RoleMutationErrorCodes.RoleIdDetail]);
    }

    [Fact]
    public async Task DeleteRole_WithBlockingContent_IsRejectedWithStableRoleInUse()
    {
        var tools = CreateTools(StateWithRolesAndDeveloperContent());

        var envelope = await tools.DeleteRole(TransactionId.ToString(), RoleDeveloper.Value, 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(RoleMutationErrorCodes.RoleInUse, envelope.Code);
        Assert.Equal("1", envelope.Details![RoleMutationErrorCodes.BlockingContentCountDetail]);
    }

    [Fact]
    public async Task DeleteRole_WithoutReferences_TombstonesTheRole()
    {
        var repository = StateWithRoles(RoleDeveloper, RoleConsultant);
        var tools = CreateTools(repository);

        var envelope = await tools.DeleteRole(TransactionId.ToString(), RoleDeveloper.Value, 0);

        Assert.True(envelope.IsSuccess);
        Assert.True(repository.State.Roles.Single(role => role.RoleId == RoleDeveloper).IsDeleted);
        Assert.False(repository.State.Roles.Single(role => role.RoleId == RoleConsultant).IsDeleted);
    }

    [Fact]
    public async Task SetRoleResolution_MapsCandidatesInGivenOrderToPriorities()
    {
        var repository = StateWithRoles(RoleDeveloper, RoleConsultant);
        var tools = CreateTools(repository);

        var envelope = await tools.SetRoleResolution(
            TransactionId.ToString(),
            RoleDeveloper.Value,
            [RoleConsultant.Value, RoleDeveloper.Value],
            0);

        Assert.True(envelope.IsSuccess);
        Assert.Equal(RoleDeveloper.Value, envelope.Data!.RequestedRoleId);
        Assert.Equal(2, envelope.Data.Items.Count);
        Assert.Equal(RoleConsultant.Value, envelope.Data.Items[0].CandidateRoleId);
        Assert.Equal(1, envelope.Data.Items[0].Priority);
        Assert.Equal(RoleDeveloper.Value, envelope.Data.Items[1].CandidateRoleId);
        Assert.Equal(2, envelope.Data.Items[1].Priority);
        Assert.Equal(2, repository.State.Resolutions.Count);
    }

    [Fact]
    public async Task SetRoleResolution_WithDuplicateCandidate_ReturnsStableDuplicateCandidateRole()
    {
        var tools = CreateTools(StateWithRoles(RoleDeveloper, RoleConsultant));

        var envelope = await tools.SetRoleResolution(
            TransactionId.ToString(),
            RoleDeveloper.Value,
            [RoleConsultant.Value, RoleConsultant.Value],
            0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.DuplicateCandidateRole, envelope.Code);
        Assert.Equal(RoleConsultant.Value, envelope.Details![RoleResolutionErrorCodes.CandidateRoleIdDetail]);
    }

    [Fact]
    public async Task SetRoleResolution_WithUnknownCandidate_ReturnsStableCandidateRoleNotFound()
    {
        var tools = CreateTools(StateWithRoles(RoleDeveloper));

        var envelope = await tools.SetRoleResolution(
            TransactionId.ToString(),
            RoleDeveloper.Value,
            ["Fehlend"],
            0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal(RoleResolutionErrorCodes.CandidateRoleNotFound, envelope.Code);
        Assert.Equal("Fehlend", envelope.Details![RoleResolutionErrorCodes.CandidateRoleIdDetail]);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task CreateRole_MalformedTransactionId_IsRejectedAsTransactionNotFound(string rawTransactionId)
    {
        var tools = CreateTools(EmptyState());

        var envelope = await tools.CreateRole(rawTransactionId, "Developer", 0);

        Assert.False(envelope.IsSuccess);
        Assert.Equal("TransactionNotFound", envelope.Code);
    }

    private static RoleMutationTools CreateTools(InMemoryRoleMutationRepository repository) =>
        new(new RoleMutationService(repository));

    private static InMemoryRoleMutationRepository EmptyState() => new(new WorkingRoleMutationState(SnapshotId, [], [], [], []));

    private static InMemoryRoleMutationRepository StateWithRoles(params RoleId[] roleIds) =>
        new(new WorkingRoleMutationState(
        SnapshotId,
        roleIds.Select(roleId => new Role(SnapshotId, roleId, roleId.Value, null, IsDeleted: false)).ToArray(),
        [],
        [],
        []));

    private static InMemoryRoleMutationRepository StateWithRolesAndDeveloperContent() =>
        new(new WorkingRoleMutationState(
        SnapshotId,
        [new Role(SnapshotId, RoleDeveloper, RoleDeveloper.Value, null, IsDeleted: false)],
        [],
        [new NodeContent(
            SnapshotId, NodeId, RoleDeveloper, RevisionId, ContentMode.Independent, "Inhalt", IsDeleted: false)],
        []));
}
