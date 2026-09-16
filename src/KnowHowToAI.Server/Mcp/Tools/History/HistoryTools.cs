using System.ComponentModel;
using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.History;
using KnowHowToAI.Server.Mcp.Mapping;
using ModelContextProtocol.Server;

namespace KnowHowToAI.Server.Mcp.Tools.History;

/// <summary>
/// Dünne MCP-Handler der Historien- und Release-Use-Cases: ausschließlich
/// Mapping und Delegation an die transportneutralen Services (verbindlich:
/// docs/konzept/05-MCP-API.md, Abschnitte 61 und 64).
/// </summary>
[McpServerToolType]
internal sealed class HistoryTools
{
    private readonly HistoryService _historyService;
    private readonly ReleaseService _releaseService;
    private readonly RetrievalPolicy _retrievalPolicy;

    public HistoryTools(
        HistoryService historyService,
        ReleaseService releaseService,
        RetrievalPolicy retrievalPolicy)
    {
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _releaseService = releaseService ?? throw new ArgumentNullException(nameof(releaseService));
        _retrievalPolicy = retrievalPolicy ?? throw new ArgumentNullException(nameof(retrievalPolicy));
    }

    [McpServerTool(Name = "get_snapshot", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Liefert Zustand und Metadaten eines Snapshots anhand seiner ID.")]
    public async Task<McpToolEnvelope<McpSnapshotData>> GetSnapshot(
        [Description("Snapshot-ID (GUID-String aus einer vorherigen Tool-Antwort).")] string snapshotId,
        CancellationToken cancellationToken = default)
    {
        var parsed = McpHistoryMapper.ParseSnapshotId(snapshotId);
        if (!parsed.IsSuccess)
            return McpToolEnvelope<McpSnapshotData>.Failure(parsed.Error!);

        var result = await _historyService.GetSnapshotAsync(parsed.Value!, cancellationToken).ConfigureAwait(false);
        return McpHistoryMapper.ToSnapshotEnvelope(result);
    }

    [McpServerTool(Name = "compare_snapshots", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Strukturierter Netto-Diff zwischen zwei committed Snapshots. " +
        "Paginiert; gibt nur geänderte Einträge zurück ohne Rekonstruktion eines Operation Logs.")]
    public async Task<McpToolEnvelope<McpSnapshotDiffData>> CompareSnapshots(
        [Description("Basis-Snapshot-ID (GUID-String).")] string baseSnapshotId,
        [Description("Ziel-Snapshot-ID (GUID-String).")] string targetSnapshotId,
        [Description("Optionale Seitengröße; fehlend oder ≤ 0 ergibt die konfigurierte Standardseitengröße, " +
            "Werte oberhalb des Maximums werden geklemmt.")] int? limit = null,
        [Description("Optionaler opaker Folgecursor aus einer vorherigen Antwort.")] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var parsedBase = McpHistoryMapper.ParseSnapshotId(baseSnapshotId);
        if (!parsedBase.IsSuccess)
            return McpToolEnvelope<McpSnapshotDiffData>.Failure(parsedBase.Error!);

        var parsedTarget = McpHistoryMapper.ParseSnapshotId(targetSnapshotId);
        if (!parsedTarget.IsSuccess)
            return McpToolEnvelope<McpSnapshotDiffData>.Failure(parsedTarget.Error!);

        var effectiveLimit = McpPagingMapper.NormalizeLimit(
            limit, _retrievalPolicy.DefaultPageSize, _retrievalPolicy.MaximumPageSize);

        var result = await _historyService.CompareSnapshotsAsync(
            parsedBase.Value!, parsedTarget.Value!, effectiveLimit, cursor, cancellationToken).ConfigureAwait(false);
        return McpHistoryMapper.ToSnapshotDiffEnvelope(result);
    }

    [McpServerTool(Name = "get_transaction_changes", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Strukturierter Netto-Diff des Working- oder Committed-Snapshots einer Transaction " +
        "gegenüber ihrem Base-Snapshot. Paginiert.")]
    public async Task<McpToolEnvelope<McpTransactionChangesData>> GetTransactionChanges(
        [Description("Transaction-ID (GUID-String aus einer vorherigen Tool-Antwort).")] string transactionId,
        [Description("Optionale Seitengröße; fehlend oder ≤ 0 ergibt die konfigurierte Standardseitengröße, " +
            "Werte oberhalb des Maximums werden geklemmt.")] int? limit = null,
        [Description("Optionaler opaker Folgecursor aus einer vorherigen Antwort.")] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var parsed = McpTransactionMapper.ParseTransactionId(transactionId);
        if (!parsed.IsSuccess)
            return McpToolEnvelope<McpTransactionChangesData>.Failure(parsed.Error!);

        var effectiveLimit = McpPagingMapper.NormalizeLimit(
            limit, _retrievalPolicy.DefaultPageSize, _retrievalPolicy.MaximumPageSize);

        var result = await _historyService.GetTransactionChangesAsync(
            parsed.Value!, effectiveLimit, cursor, cancellationToken).ConfigureAwait(false);
        return McpHistoryMapper.ToTransactionChangesEnvelope(result);
    }

    [McpServerTool(Name = "create_release", Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Registriert atomar einen unveränderlichen Release-Verweis auf einen bereits committed Snapshot. " +
        "Benötigt keine KnowHowTo-AI-Transaction (reine Metadatenverwaltung). " +
        "Qualitätsbefunde des referenzierten Snapshots erscheinen transparent als Warnungen.")]
    public async Task<McpToolEnvelope<McpReleaseData>> CreateRelease(
        [Description("Eindeutiger Release-Name (nicht leer oder Whitespace).")] string name,
        [Description("Snapshot-ID des committed Ziel-Snapshots (GUID-String).")] string snapshotId,
        [Description("Optionale Release-Beschreibung.")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        var parsed = McpHistoryMapper.ParseSnapshotId(snapshotId);
        if (!parsed.IsSuccess)
            return McpToolEnvelope<McpReleaseData>.Failure(parsed.Error!);

        var result = await _releaseService.CreateReleaseAsync(
            name, parsed.Value!, description, cancellationToken).ConfigureAwait(false);
        return McpHistoryMapper.ToCreateReleaseEnvelope(result);
    }

    [McpServerTool(Name = "list_releases", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Paginierte, deterministisch sortierte Liste aller Releases.")]
    public async Task<McpToolEnvelope<McpReleasePageData>> ListReleases(
        [Description("Optionale Seitengröße; fehlend oder ≤ 0 ergibt die konfigurierte Standardseitengröße, " +
            "Werte oberhalb des Maximums werden geklemmt.")] int? limit = null,
        [Description("Optionaler opaker Folgecursor aus einer vorherigen Antwort.")] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveLimit = McpPagingMapper.NormalizeLimit(
            limit, _retrievalPolicy.DefaultPageSize, _retrievalPolicy.MaximumPageSize);

        var result = await _releaseService.ListReleasesAsync(
            effectiveLimit, cursor, cancellationToken).ConfigureAwait(false);
        return McpHistoryMapper.ToReleasePageEnvelope(result);
    }
}
