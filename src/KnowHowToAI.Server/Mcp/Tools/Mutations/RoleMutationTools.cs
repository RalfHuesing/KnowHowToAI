using System.ComponentModel;
using KnowHowToAI.Core.Application.Mutations.Roles;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Mutations.Roles;
using KnowHowToAI.Server.Mcp.Contracts.Navigation;
using KnowHowToAI.Server.Mcp.Mapping;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Server.Mcp.Tools.Mutations;

/// <summary>
/// Dünne MCP-Handler der Rollen- und Resolution-Order-Mutationen: ausschließlich
/// Mapping und Delegation an den transportneutralen <see cref="RoleMutationService"/>
/// (verbindlich: docs/konzept/05-MCP-API.md, Abschnitte 62 und 64).
/// </summary>
[McpServerToolType]
internal sealed class RoleMutationTools
{
    private readonly RoleMutationService _roleMutationService;

    public RoleMutationTools(RoleMutationService roleMutationService) =>
        _roleMutationService = roleMutationService ?? throw new ArgumentNullException(nameof(roleMutationService));

    [McpServerTool(Name = "create_role", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Legt eine Wissensrolle innerhalb einer offenen Transaction an; der " +
        "ausgegebene roleId entspricht dem Rollennamen.")]
    public async Task<McpToolEnvelope<McpRoleData>> CreateRole(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Name der neuen Rolle; er bestimmt den roleId.")] string name,
        [Description("Optionale Beschreibung der Rolle.")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsedTransactionId.IsSuccess)
            return McpToolEnvelope<McpRoleData>.Failure(parsedTransactionId.Error!);

        return McpMutationMapper.ToEnvelope(await _roleMutationService
            .CreateRoleAsync(parsedTransactionId.Value, name, description, cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "update_role", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Ändert Name und Beschreibung einer bestehenden Wissensrolle innerhalb " +
        "einer offenen Transaction.")]
    public async Task<McpToolEnvelope<McpRoleData>> UpdateRole(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Rollen-ID aus einer vorherigen Tool-Antwort.")] string roleId,
        [Description("Neuer Name der Rolle.")] string name,
        [Description("Optionale neue Beschreibung der Rolle.")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsedTransactionId.IsSuccess)
            return McpToolEnvelope<McpRoleData>.Failure(parsedTransactionId.Error!);

        return McpMutationMapper.ToEnvelope(await _roleMutationService
            .UpdateRoleAsync(parsedTransactionId.Value, new RoleId(roleId), name, description, cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "delete_role", Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Entfernt eine Wissensrolle durch Soft-Delete, solange sie nicht noch " +
        "von Content, Dependencies oder Resolution Orders referenziert wird.")]
    public async Task<McpToolEnvelope<McpRoleData>> DeleteRole(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Rollen-ID aus einer vorherigen Tool-Antwort.")] string roleId,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsedTransactionId.IsSuccess)
            return McpToolEnvelope<McpRoleData>.Failure(parsedTransactionId.Error!);

        return McpMutationMapper.ToEnvelope(await _roleMutationService
            .DeleteRoleAsync(parsedTransactionId.Value, new RoleId(roleId), cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "set_role_resolution", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Setzt die vollständige, nicht rekursive Resolution Order einer Rolle; " +
        "die Reihenfolge von candidateRoleIds bestimmt die Priorität (1 = höchste).")]
    public async Task<McpToolEnvelope<McpRoleResolutionData>> SetRoleResolution(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Rollen-ID der angefragten Rolle.")] string roleId,
        [Description("Kandidaten-Rollen in gewünschter Reihenfolge; ersetzt die bisherige Order vollständig.")] string[] candidateRoleIds,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsedTransactionId.IsSuccess)
            return McpToolEnvelope<McpRoleResolutionData>.Failure(parsedTransactionId.Error!);

        var result = await _roleMutationService
            .SetRoleResolutionAsync(
                parsedTransactionId.Value,
                new RoleId(roleId),
                candidateRoleIds.Select(candidateRoleId => new RoleId(candidateRoleId)),
                cancellationToken)
            .ConfigureAwait(false);
        return McpMutationMapper.ToEnvelope(new RoleId(roleId), result);
    }
}
