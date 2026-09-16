using System.ComponentModel;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Mutations.Content;
using KnowHowToAI.Server.Mcp.Mapping;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Server.Mcp.Tools.Mutations;

/// <summary>
/// Dünne MCP-Handler der Content-Mutationen: ausschließlich Mapping und Delegation
/// an den transportneutralen <see cref="ContentMutationApplicationService"/>
/// (verbindlich: docs/konzept/05-MCP-API.md, Abschnitte 62, 63 und 64).
/// </summary>
[McpServerToolType]
internal sealed class ContentMutationTools
{
    private readonly ContentMutationApplicationService _contentMutationService;

    public ContentMutationTools(ContentMutationApplicationService contentMutationService) =>
        _contentMutationService = contentMutationService ?? throw new ArgumentNullException(nameof(contentMutationService));

    [McpServerTool(Name = "replace_content", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Ersetzt oder legt den expliziten Rollen-Content einer Node vollständig an; " +
        "bei Derived werden die Source-Revisions unter sources übergeben.")]
    public async Task<McpToolEnvelope<McpContentMutationData>> ReplaceContent(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Node-ID aus einer vorherigen Tool-Antwort (GUID-String).")] string nodeId,
        [Description("Angefragte Rolle (roleId aus list_roles).")] string roleId,
        [Description("Content-Modus: exakt 'Independent' oder 'Derived'.")] string contentMode,
        [Description("Vollständiger Markdown-Inhalt ohne Überschriften.")] string contentMd,
        [Description("Optionale Source-Revisions für Derived Content (je nodeId, roleId, contentRevisionId).")] McpContentSourceData[]? sources = null,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        var parsedNodeId = McpNavigationMapper.ParseOptionalNodeId(nodeId);
        var parsedContentMode = McpMutationMapper.ParseContentMode(contentMode);
        var parsedSources = McpMutationMapper.ParseSources(sources);
        if (!parsedTransactionId.IsSuccess || !parsedNodeId.IsSuccess
            || !parsedContentMode.IsSuccess || !parsedSources.IsSuccess)
        {
            return Failure(
                parsedTransactionId.Error,
                parsedNodeId.Error,
                parsedContentMode.Error,
                parsedSources.Error);
        }

        var request = new ReplaceContentRequest(
            parsedNodeId.Value!.Value,
            new RoleId(roleId),
            parsedContentMode.Value,
            contentMd,
            parsedSources.Value!);
        return McpMutationMapper.ToEnvelope(await _contentMutationService
            .ReplaceContentAsync(parsedTransactionId.Value, request, cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "replace_text", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Ersetzt exakt ein Vorkommen von oldText im expliziten Rollen-Content " +
        "einer Node; 0 Treffer liefern TextNotFound, mehrere MultipleTextMatches.")]
    public async Task<McpToolEnvelope<McpContentMutationData>> ReplaceText(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Node-ID aus einer vorherigen Tool-Antwort (GUID-String).")] string nodeId,
        [Description("Angefragte Rolle (roleId aus list_roles).")] string roleId,
        [Description("Exakt einmal vorkommender Textabschnitt.")] string oldText,
        [Description("Ersatztext für das Vorkommen.")] string newText,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        var parsedNodeId = McpNavigationMapper.ParseOptionalNodeId(nodeId);
        if (!parsedTransactionId.IsSuccess || !parsedNodeId.IsSuccess)
            return Failure(parsedTransactionId.Error, parsedNodeId.Error);

        var request = new ReplaceTextRequest(
            parsedNodeId.Value!.Value,
            new RoleId(roleId),
            oldText,
            newText);
        return McpMutationMapper.ToEnvelope(await _contentMutationService
            .ReplaceTextAsync(parsedTransactionId.Value, request, cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "delete_content", Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Entfernt ausschließlich den expliziten Rollen-Content einer Node " +
        "durch Soft-Delete, ohne die Node selbst zu verändern.")]
    public async Task<McpToolEnvelope<McpContentMutationData>> DeleteContent(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Node-ID aus einer vorherigen Tool-Antwort (GUID-String).")] string nodeId,
        [Description("Angefragte Rolle (roleId aus list_roles).")] string roleId,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        var parsedNodeId = McpNavigationMapper.ParseOptionalNodeId(nodeId);
        if (!parsedTransactionId.IsSuccess || !parsedNodeId.IsSuccess)
            return Failure(parsedTransactionId.Error, parsedNodeId.Error);

        return McpMutationMapper.ToEnvelope(await _contentMutationService
            .DeleteContentAsync(parsedTransactionId.Value, parsedNodeId.Value!.Value, new RoleId(roleId), cancellationToken)
            .ConfigureAwait(false));
    }

    private static McpToolEnvelope<McpContentMutationData> Failure(params DomainError?[] errors) =>
        McpToolEnvelope<McpContentMutationData>.Failure(errors.First(error => error is not null)!);
}
