using System.ComponentModel;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Mutations.Nodes;
using KnowHowToAI.Server.Mcp.Mapping;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Server.Mcp.Tools.Mutations;

/// <summary>
/// Dünne MCP-Handler der globalen Strukturänderungen: ausschließlich Mapping und
/// Delegation an den transportneutralen <see cref="NodeMutationApplicationService"/>.
/// </summary>
[McpServerToolType]
internal sealed class NodeMutationTools
{
    private readonly NodeMutationApplicationService _nodeMutationService;

    public NodeMutationTools(NodeMutationApplicationService nodeMutationService) =>
        _nodeMutationService = nodeMutationService ?? throw new ArgumentNullException(nameof(nodeMutationService));

    [McpServerTool(Name = "create_node", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Legt eine Node als globale Strukturänderung innerhalb einer offenen " +
        "Transaction an; ohne parentNodeId wird eine Root-Node erzeugt.")]
    public async Task<McpToolEnvelope<McpNodeMutationData>> CreateNode(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Titel der neuen Node.")] string title,
        [Description("Optionale Beschreibung der Node.")] string? description = null,
        [Description("Optionale Node-ID des Parents (GUID-String); ohne Wert wird eine Root-Node angelegt.")] string? parentNodeId = null,
        [Description("Optionale Sortierposition innerhalb der Geschwistergruppe (Standard 0).")] int sortOrder = 0,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsedTransactionId.IsSuccess)
            return McpToolEnvelope<McpNodeMutationData>.Failure(parsedTransactionId.Error!);

        var parsedParentNodeId = McpNavigationMapper.ParseOptionalNodeId(parentNodeId);
        if (!parsedParentNodeId.IsSuccess)
            return McpToolEnvelope<McpNodeMutationData>.Failure(parsedParentNodeId.Error!);

        var request = new CreateNodeRequest(parsedParentNodeId.Value, title, description, sortOrder);
        return McpMutationMapper.ToEnvelope(await _nodeMutationService
            .CreateAsync(parsedTransactionId.Value, request, cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "update_node", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Ändert Titel und Beschreibung einer Node als globale Strukturänderung " +
        "innerhalb einer offenen Transaction.")]
    public async Task<McpToolEnvelope<McpNodeMutationData>> UpdateNode(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Node-ID aus einer vorherigen Tool-Antwort (GUID-String).")] string nodeId,
        [Description("Neuer Titel der Node.")] string title,
        [Description("Optionale neue Beschreibung der Node.")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        var parsedNodeId = McpNavigationMapper.ParseOptionalNodeId(nodeId);
        if (!parsedTransactionId.IsSuccess || !parsedNodeId.IsSuccess)
            return Failure(parsedTransactionId.Error, parsedNodeId.Error);

        return McpMutationMapper.ToEnvelope(await _nodeMutationService
            .UpdateAsync(parsedTransactionId.Value, parsedNodeId.Value!.Value, title, description, cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "move_node", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Verschiebt eine Node unter einen neuen Parent und setzt ihre Sortierposition; " +
        "ohne parentNodeId wird die Node zur Root-Node.")]
    public async Task<McpToolEnvelope<McpNodeMutationData>> MoveNode(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Node-ID aus einer vorherigen Tool-Antwort (GUID-String).")] string nodeId,
        [Description("Sortierposition innerhalb der neuen Geschwistergruppe.")] int sortOrder,
        [Description("Optionale Node-ID des neuen Parents (GUID-String); ohne Wert wird die Node zur Root-Node.")] string? parentNodeId = null,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        var parsedNodeId = McpNavigationMapper.ParseOptionalNodeId(nodeId);
        var parsedParentNodeId = McpNavigationMapper.ParseOptionalNodeId(parentNodeId);
        if (!parsedTransactionId.IsSuccess || !parsedNodeId.IsSuccess || !parsedParentNodeId.IsSuccess)
            return Failure(parsedTransactionId.Error, parsedNodeId.Error, parsedParentNodeId.Error);

        return McpMutationMapper.ToEnvelope(await _nodeMutationService
            .MoveAsync(
                parsedTransactionId.Value,
                parsedNodeId.Value!.Value,
                parsedParentNodeId.Value,
                sortOrder,
                cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "reorder_node", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Setzt die Sortierposition einer Node innerhalb ihrer bestehenden " +
        "Geschwistergruppe als globale Strukturänderung.")]
    public async Task<McpToolEnvelope<McpNodeMutationData>> ReorderNode(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Node-ID aus einer vorherigen Tool-Antwort (GUID-String).")] string nodeId,
        [Description("Neue Sortierposition innerhalb der Geschwistergruppe.")] int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        var parsedNodeId = McpNavigationMapper.ParseOptionalNodeId(nodeId);
        if (!parsedTransactionId.IsSuccess || !parsedNodeId.IsSuccess)
            return Failure(parsedTransactionId.Error, parsedNodeId.Error);

        return McpMutationMapper.ToEnvelope(await _nodeMutationService
            .ReorderAsync(parsedTransactionId.Value, parsedNodeId.Value!.Value, sortOrder, cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "delete_node", Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Löscht eine Node als globale Strukturänderung über alle Rollen; eine Node " +
        "mit aktiven Children erfordert deleteSubtree=true.")]
    public async Task<McpToolEnvelope<McpNodeMutationData>> DeleteNode(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Node-ID aus einer vorherigen Tool-Antwort (GUID-String).")] string nodeId,
        [Description("Optional: gesamten Teilbaum einschließlich aller Children und Contents löschen (Standard false).")] bool deleteSubtree = false,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        var parsedNodeId = McpNavigationMapper.ParseOptionalNodeId(nodeId);
        if (!parsedTransactionId.IsSuccess || !parsedNodeId.IsSuccess)
            return Failure(parsedTransactionId.Error, parsedNodeId.Error);

        return McpMutationMapper.ToEnvelope(await _nodeMutationService
            .DeleteAsync(parsedTransactionId.Value, parsedNodeId.Value!.Value, deleteSubtree, cancellationToken)
            .ConfigureAwait(false));
    }

    private static McpToolEnvelope<McpNodeMutationData> Failure(params DomainError?[] errors) =>
        McpToolEnvelope<McpNodeMutationData>.Failure(errors.First(error => error is not null)!);
}
