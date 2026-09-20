using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Mcp.Contracts;
using KnowHowToAI.Server.Mcp.Contracts.History;

namespace KnowHowToAI.Server.Mcp.Mapping;

/// <summary>
/// Bildet die transportneutralen Ergebnisse der History- und Release-Use-Cases auf
/// den gemeinsamen MCP-Antwort-Envelope und die History-DTOs ab.
/// Enthält keine Fachlogik.
/// </summary>
internal static class McpHistoryMapper
{
    // ── Snapshot ──────────────────────────────────────────────────────────────

    public static McpToolEnvelope<McpSnapshotData> ToSnapshotEnvelope(Result<Snapshot> result) =>
        result.IsSuccess
            ? McpToolEnvelope<McpSnapshotData>.Success(ToSnapshotData(result.Value!))
            : McpToolEnvelope<McpSnapshotData>.Failure(result.Error!);

    private static McpSnapshotData ToSnapshotData(Snapshot s) => new(
        s.SnapshotId.ToString(),
        s.State.ToString(),
        s.CreatedAtUtc,
        s.BaseSnapshotId?.ToString(),
        s.CommittedAtUtc,
        s.CommitMetadata?.TransactionId.ToString(),
        s.CommitMetadata?.Actor,
        s.CommitMetadata?.Client,
        s.CommitMetadata?.Purpose,
        s.CommitMetadata?.CommitMessage);

    // ── compare_snapshots ─────────────────────────────────────────────────────

    public static McpToolEnvelope<McpSnapshotDiffData> ToSnapshotDiffEnvelope(Result<SnapshotDiff> result) =>
        result.IsSuccess
            ? McpToolEnvelope<McpSnapshotDiffData>.Success(ToSnapshotDiffData(result.Value!))
            : McpToolEnvelope<McpSnapshotDiffData>.Failure(result.Error!);

    private static McpSnapshotDiffData ToSnapshotDiffData(SnapshotDiff diff) => new(
        diff.BaseSnapshotId.ToString(),
        diff.TargetSnapshotId.ToString(),
        diff.TotalCount,
        ToFlatEntries(diff),
        diff.NextCursor);

    // ── get_transaction_changes ───────────────────────────────────────────────

    public static McpToolEnvelope<McpTransactionChangesData> ToTransactionChangesEnvelope(Result<TransactionDiff> result) =>
        result.IsSuccess
            ? McpToolEnvelope<McpTransactionChangesData>.Success(ToTransactionChangesData(result.Value!))
            : McpToolEnvelope<McpTransactionChangesData>.Failure(result.Error!);

    private static McpTransactionChangesData ToTransactionChangesData(TransactionDiff txDiff) => new(
        txDiff.Transaction.TransactionId.ToString(),
        txDiff.Transaction.State.ToString(),
        txDiff.Transaction.BaseSnapshotId.ToString(),
        txDiff.Transaction.WorkingSnapshotId.ToString(),
        ToSnapshotDiffData(txDiff.Changes));

    // ── Release ───────────────────────────────────────────────────────────────

    public static McpToolEnvelope<McpReleaseData> ToCreateReleaseEnvelope(Result<CreateReleaseResult> result) =>
        result.IsSuccess
            ? McpToolEnvelope<McpReleaseData>.Success(
                ToReleaseData(result.Value!.Release),
                result.Value.Findings.Select(McpResultMapper.ToWarning).ToArray())
            : McpToolEnvelope<McpReleaseData>.Failure(result.Error!);

    public static McpToolEnvelope<McpReleasePageData> ToReleasePageEnvelope(Result<ReleasePage> result) =>
        result.IsSuccess
            ? McpToolEnvelope<McpReleasePageData>.Success(ToReleasePageData(result.Value!))
            : McpToolEnvelope<McpReleasePageData>.Failure(result.Error!);

    private static McpReleasePageData ToReleasePageData(ReleasePage page) => new(
        page.Items.Select(ToReleaseData).ToArray(),
        page.NextCursor);

    private static McpReleaseData ToReleaseData(Release r) => new(
        r.ReleaseId.ToString(),
        r.SnapshotId.ToString(),
        r.Name,
        r.ReleasedAtUtc,
        r.Description);

    // ── Snapshot-ID-Parsing ───────────────────────────────────────────────────

    /// <summary>
    /// Parst einen Snapshot-ID-String exakt im dezimalen Format der Tool-Ausgaben.
    /// Ein nicht parsebarer Wert kann keinen existierenden Snapshot bezeichnen und
    /// führt zu <c>SnapshotNotFound</c> mit dem Rohwert in den Details.
    /// </summary>
    public static Result<SnapshotId> ParseSnapshotId(string? raw)
    {
        if (raw is not null &&
            long.TryParse(raw, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            return Result<SnapshotId>.Success(new SnapshotId(parsed));

        return Result<SnapshotId>.Failure(new DomainError(
            HistoryErrorCodes.SnapshotNotFound,
            "Der angefragte Snapshot existiert nicht.",
            new Dictionary<string, string>
            {
                [HistoryErrorCodes.SnapshotIdDetail] = raw ?? string.Empty
            }));
    }

    // ── Diff-Hilfsmethoden ────────────────────────────────────────────────────

    private static IReadOnlyList<McpDiffEntryData> ToFlatEntries(SnapshotDiff diff)
    {
        var entries = new List<McpDiffEntryData>(
            diff.Nodes.Count + diff.Audiences.Count + diff.AudienceResolutions.Count +
            diff.Contents.Count + diff.Dependencies.Count);

        foreach (var e in diff.Audiences)
        {
            var side = e.After ?? e.Before;
            entries.Add(new McpDiffEntryData(e.Kind.ToString(), "role", side!.AudienceId.ToString()));
        }

        foreach (var e in diff.AudienceResolutions)
        {
            var side = e.After ?? e.Before;
            entries.Add(new McpDiffEntryData(
                e.Kind.ToString(), "roleResolution",
                side!.RequestedAudienceId.ToString(),
                side.CandidateAudienceId.ToString()));
        }

        foreach (var e in diff.Nodes)
        {
            var side = e.After ?? e.Before;
            entries.Add(new McpDiffEntryData(e.Kind.ToString(), "node", side!.NodeId.ToString()));
        }

        foreach (var e in diff.Contents)
        {
            var side = e.After ?? e.Before;
            entries.Add(new McpDiffEntryData(
                e.Kind.ToString(), "content",
                side!.NodeId.ToString(),
                side.AudienceId.ToString()));
        }

        foreach (var e in diff.Dependencies)
        {
            var side = e.After ?? e.Before;
            entries.Add(new McpDiffEntryData(
                e.Kind.ToString(), "dependency",
                side!.TargetNodeId.ToString(),
                side.TargetAudienceId.ToString(),
                side.SourceNodeId.ToString(),
                side.SourceAudienceId.ToString()));
        }

        return entries;
    }
}
