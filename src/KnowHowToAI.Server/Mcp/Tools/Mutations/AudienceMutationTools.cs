using System.ComponentModel;
using KnowHowToAI.Core.Application.Mutations.Audiences;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Mutations.Audiences;
using KnowHowToAI.Server.Mcp.Contracts.Navigation;
using KnowHowToAI.Server.Mcp.Mapping;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Server.Mcp.Tools.Mutations;

/// <summary>
/// Dünne MCP-Handler der Zielgruppen- und Resolution-Order-Mutationen: ausschließlich
/// Mapping und Delegation an den transportneutralen <see cref="AudienceMutationService"/>.
/// </summary>
[McpServerToolType]
internal sealed class AudienceMutationTools
{
    private readonly AudienceMutationService _audienceMutationService;

    public AudienceMutationTools(AudienceMutationService audienceMutationService) =>
        _audienceMutationService = audienceMutationService ?? throw new ArgumentNullException(nameof(audienceMutationService));

    [McpServerTool(Name = "create_audience", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Legt eine Wissenszielgruppe innerhalb einer offenen Transaction an; der " +
        "ausgegebene audienceId entspricht dem Zielgruppennamen.")]
    public async Task<McpToolEnvelope<McpAudienceData>> CreateAudience(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Name der neuen Zielgruppe; er bestimmt den audienceId.")] string name,
        [Description("Erwarteter ChangeVersion-Stand der offenen Transaction; Stale Writes werden atomar abgelehnt.")] long expectedChangeVersion,
        [Description("Optionale Beschreibung der Zielgruppe.")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsedTransactionId.IsSuccess)
            return McpToolEnvelope<McpAudienceData>.Failure(parsedTransactionId.Error!);

        return McpMutationMapper.ToAudienceMutationEnvelope(await _audienceMutationService
            .CreateAudienceMutationAsync(parsedTransactionId.Value, name, description, expectedChangeVersion, cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "update_audience", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Ändert Name und Beschreibung einer bestehenden Wissenszielgruppe innerhalb " +
        "einer offenen Transaction.")]
    public async Task<McpToolEnvelope<McpAudienceData>> UpdateAudience(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Zielgruppen-ID aus einer vorherigen Tool-Antwort.")] string audienceId,
        [Description("Neuer Name der Zielgruppe.")] string name,
        [Description("Erwarteter ChangeVersion-Stand der offenen Transaction; Stale Writes werden atomar abgelehnt.")] long expectedChangeVersion,
        [Description("Optionale neue Beschreibung der Zielgruppe.")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsedTransactionId.IsSuccess)
            return McpToolEnvelope<McpAudienceData>.Failure(parsedTransactionId.Error!);

        return McpMutationMapper.ToAudienceMutationEnvelope(await _audienceMutationService
            .UpdateAudienceMutationAsync(parsedTransactionId.Value, new UpdateAudienceMutationRequest(new AudienceId(audienceId), name, description, expectedChangeVersion), cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "delete_audience", Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Entfernt eine Wissenszielgruppe durch Soft-Delete, solange sie nicht noch " +
        "von Content, Dependencies oder Resolution Orders referenziert wird.")]
    public async Task<McpToolEnvelope<McpAudienceData>> DeleteAudience(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Zielgruppen-ID aus einer vorherigen Tool-Antwort.")] string audienceId,
        [Description("Erwarteter ChangeVersion-Stand der offenen Transaction; Stale Writes werden atomar abgelehnt.")] long expectedChangeVersion,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsedTransactionId.IsSuccess)
            return McpToolEnvelope<McpAudienceData>.Failure(parsedTransactionId.Error!);

        return McpMutationMapper.ToAudienceMutationEnvelope(await _audienceMutationService
            .DeleteAudienceMutationAsync(parsedTransactionId.Value, new AudienceId(audienceId), expectedChangeVersion, cancellationToken)
            .ConfigureAwait(false));
    }

    [McpServerTool(Name = "set_audience_resolution", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Setzt die vollständige, nicht rekursive Resolution Order einer Zielgruppe; " +
        "die Reihenfolge von candidateAudienceIds bestimmt die Priorität (1 = höchste).")]
    public async Task<McpToolEnvelope<McpAudienceResolutionData>> SetAudienceResolution(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Zielgruppen-ID der angefragten Zielgruppe.")] string audienceId,
        [Description("Kandidaten-Zielgruppen in gewünschter Reihenfolge; ersetzt die bisherige Order vollständig.")] string[] candidateAudienceIds,
        [Description("Erwarteter ChangeVersion-Stand der offenen Transaction; Stale Writes werden atomar abgelehnt.")] long expectedChangeVersion,
        CancellationToken cancellationToken = default)
    {
        var parsedTransactionId = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsedTransactionId.IsSuccess)
            return McpToolEnvelope<McpAudienceResolutionData>.Failure(parsedTransactionId.Error!);

        var result = await _audienceMutationService
            .SetAudienceResolutionMutationAsync(
                parsedTransactionId.Value,
                new AudienceId(audienceId),
                candidateAudienceIds.Select(candidateAudienceId => new AudienceId(candidateAudienceId)),
                expectedChangeVersion,
                cancellationToken)
            .ConfigureAwait(false);
        return McpMutationMapper.ToAudienceResolutionMutationEnvelope(result);
    }
}
