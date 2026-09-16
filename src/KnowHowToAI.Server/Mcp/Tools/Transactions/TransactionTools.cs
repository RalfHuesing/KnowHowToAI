using System.ComponentModel;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.Transactions;
using KnowHowToAI.Server.Mcp.Mapping;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Server.Mcp.Tools.Transactions;

/// <summary>
/// Dünne MCP-Handler der Transaktions-Engine: ausschließlich Mapping und Delegation
/// an den transportneutralen <see cref="TransactionService"/> (verbindlich:
/// docs/konzept/05-MCP-API.md, Abschnitte 61 und 64).
/// </summary>
[McpServerToolType]
internal sealed class TransactionTools
{
    private readonly TransactionService _transactionService;

    public TransactionTools(TransactionService transactionService) =>
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));

    [McpServerTool(Name = "begin_transaction", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Eröffnet eine KnowHowTo-AI-Transaction mit vollständigem Working Snapshot "
        + "auf Basis des aktuellen committed Standes und liefert deren Metadaten.")]
    public async Task<McpToolEnvelope<McpTransactionData>> BeginTransaction(
        [Description("Optionaler Verwendungszweck (Audit-Metadatum).")] string? purpose = null,
        [Description("Optionaler Akteur (Audit-Metadatum).")] string? actor = null,
        [Description("Optionaler Client (Audit-Metadatum).")] string? client = null,
        CancellationToken cancellationToken = default)
    {
        var options = new BeginTransactionOptions(purpose, actor, client);
        var transaction = await _transactionService.BeginAsync(options, cancellationToken).ConfigureAwait(false);
        return McpTransactionMapper.ToEnvelope(transaction);
    }

    [McpServerTool(Name = "get_transaction", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Liefert die Metadaten einer KnowHowTo-AI-Transaction, unabhängig von ihrem Zustand.")]
    public async Task<McpToolEnvelope<McpTransactionData>> GetTransaction(
        [Description("Transaction-ID aus einer vorherigen Tool-Antwort (GUID-String).")] string transactionId,
        CancellationToken cancellationToken = default)
    {
        var parsed = McpTransactionMapper.ParseTransactionId(transactionId);
        return parsed.IsSuccess
            ? McpTransactionMapper.ToEnvelope(
                await _transactionService.GetAsync(parsed.Value!, cancellationToken).ConfigureAwait(false))
            : McpToolEnvelope<McpTransactionData>.Failure(parsed.Error!);
    }

    [McpServerTool(Name = "validate_transaction", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Prüft den vollständigen Zustand des Working Snapshots einer offenen Transaction, "
        + "ohne ihn zu verändern. isValid zeigt, ob der Commit derzeit zulässig wäre.")]
    public async Task<McpToolEnvelope<McpValidationReportData>> ValidateTransaction(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        CancellationToken cancellationToken = default)
    {
        var parsed = McpTransactionMapper.ParseTransactionId(transactionId);
        return parsed.IsSuccess
            ? McpTransactionMapper.ToEnvelope(
                await _transactionService.ValidateAsync(parsed.Value!, cancellationToken).ConfigureAwait(false))
            : McpToolEnvelope<McpValidationReportData>.Failure(parsed.Error!);
    }

    [McpServerTool(Name = "commit_transaction", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Validiert den Working Snapshot atomar und aktiviert ihn als neuen committed "
        + "Stand. Qualitätsbefunde bleiben als Warnungen sichtbar und blockieren den Commit nicht.")]
    public async Task<McpToolEnvelope<McpTransactionData>> CommitTransaction(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        [Description("Optionale Freigabemeldung, die mit der Transaction historisiert wird.")] string? commitMessage = null,
        CancellationToken cancellationToken = default)
    {
        var parsed = McpTransactionMapper.ParseTransactionId(transactionId);
        return parsed.IsSuccess
            ? McpTransactionMapper.ToEnvelope(
                await _transactionService.CommitAsync(parsed.Value!, commitMessage, cancellationToken).ConfigureAwait(false))
            : McpToolEnvelope<McpTransactionData>.Failure(parsed.Error!);
    }

    [McpServerTool(Name = "discard_transaction", Destructive = true, Idempotent = false, OpenWorld = false)]
    [Description("Verwirft eine offene KnowHowTo-AI-Transaction samt Working Snapshot, "
        + "ohne den aktuellen committed Stand zu verändern.")]
    public async Task<McpToolEnvelope<object>> DiscardTransaction(
        [Description("Transaction-ID einer offenen Transaction (GUID-String).")] string transactionId,
        CancellationToken cancellationToken = default)
    {
        var parsed = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsed.IsSuccess)
            return McpToolEnvelope<object>.Failure(parsed.Error!);

        var result = await _transactionService.DiscardAsync(parsed.Value!, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? McpToolEnvelope<object>.Success()
            : McpToolEnvelope<object>.Failure(result.Error!);
    }
}
