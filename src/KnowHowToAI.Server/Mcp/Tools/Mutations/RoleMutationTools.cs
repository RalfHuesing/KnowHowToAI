using System.ComponentModel;
using KnowHowToAI.Core.Application.Mutations.Audiences;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Mutations.Roles;
using KnowHowToAI.Server.Mcp.Contracts.Navigation;
using KnowHowToAI.Server.Mcp.Mapping;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Server.Mcp.Tools.Mutations;

/// <summary>
/// Dünne MCP-Handler der Rollen- und Resolution-Order-Mutationen: ausschließlich
/// Mapping und Delegation an den transportneutralen <see cref="AudienceMutationService"/>.
/// </summary>
[McpServerToolType]
internal sealed class RoleMutationTools
{
    private readonly AudienceMutationService _roleMutationService;

    public RoleMutationTools(AudienceMutationService roleMutationService) =>
        _roleMutationService = roleMutationService ?? throw new ArgumentNullException(nameof(roleMutationService));

    [McpServerTool(Name = "create_role", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Legt eine Wissensrolle innerhalb einer offenen Transaction an; der " +
        "ausgegebene roleId entspricht dem Rollennamen.")]
    public async Task<McpToolEnvelope<McpRoleData>> CreateRole(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Name der neuen Rolle; er bestimmt den roleId.")] string name,
        [Description("Erwarteter ChangeVersion-Stand der offenen Transaction; Stale Writes werden atomar abgelehnt.")] long expectedChangeVersion,
        [Description("Optionale Beschreibung der Rolle.")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsedTransactionId.IsSuccess)
            return McpToolEnvelope<McpRoleData>.Failure(parsedTransactionId.Error!);

        return McpMutationMapper.ToRoleMutationEnvelope(await _roleMutationService
            .CreateAudienceMutationAsync(parsedTransactionId.Value, name, description, expectedChangeVersion, cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "update_role", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Ändert Name und Beschreibung einer bestehenden Wissensrolle innerhalb " +
        "einer offenen Transaction.")]
    public async Task<McpToolEnvelope<McpRoleData>> UpdateRole(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Rollen-ID aus einer vorherigen Tool-Antwort.")] string roleId,
        [Description("Neuer Name der Rolle.")] string name,
        [Description("Erwarteter ChangeVersion-Stand der offenen Transaction; Stale Writes werden atomar abgelehnt.")] long expectedChangeVersion,
        [Description("Optionale neue Beschreibung der Rolle.")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsedTransactionId.IsSuccess)
            return McpToolEnvelope<McpRoleData>.Failure(parsedTransactionId.Error!);

        return McpMutationMapper.ToRoleMutationEnvelope(await _roleMutationService
            .UpdateAudienceMutationAsync(parsedTransactionId.Value, new UpdateAudienceMutationRequest(new AudienceId(roleId), name, description, expectedChangeVersion), cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "delete_role", Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Entfernt eine Wissensrolle durch Soft-Delete, solange sie nicht noch " +
        "von Content, Dependencies oder Resolution Orders referenziert wird.")]
    public async Task<McpToolEnvelope<McpRoleData>> DeleteRole(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Rollen-ID aus einer vorherigen Tool-Antwort.")] string roleId,
        [Description("Erwarteter ChangeVersion-Stand der offenen Transaction; Stale Writes werden atomar abgelehnt.")] long expectedChangeVersion,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsedTransactionId.IsSuccess)
            return McpToolEnvelope<McpRoleData>.Failure(parsedTransactionId.Error!);

        return McpMutationMapper.ToRoleMutationEnvelope(await _roleMutationService
            .DeleteAudienceMutationAsync(parsedTransactionId.Value, new AudienceId(roleId), expectedChangeVersion, cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "set_role_resolution", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Setzt die vollständige, nicht rekursive Resolution Order einer Rolle; " +
        "die Reihenfolge von candidateRoleIds bestimmt die Priorität (1 = höchste).")]
    public async Task<McpToolEnvelope<McpRoleResolutionData>> SetRoleResolution(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Rollen-ID der angefragten Rolle.")] string roleId,
        [Description("Kandidaten-Rollen in gewünschter Reihenfolge; ersetzt die bisherige Order vollständig.")] string[] candidateRoleIds,
        [Description("Erwarteter ChangeVersion-Stand der offenen Transaction; Stale Writes werden atomar abgelehnt.")] long expectedChangeVersion,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsedTransactionId.IsSuccess)
            return McpToolEnvelope<McpRoleResolutionData>.Failure(parsedTransactionId.Error!);

        var result = await _roleMutationService
            .SetAudienceResolutionMutationAsync(
                parsedTransactionId.Value,
                new AudienceId(roleId),
                candidateRoleIds.Select(candidateRoleId => new AudienceId(candidateRoleId)),
                expectedChangeVersion,
                cancellationToken)
            .ConfigureAwait(false);
        return McpMutationMapper.ToRoleResolutionMutationEnvelope(result);
    }
}
