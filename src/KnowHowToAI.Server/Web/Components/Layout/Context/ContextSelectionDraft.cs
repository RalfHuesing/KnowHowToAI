using System.Globalization;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.WebUtilities;

namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Unpersistierter Eingabezustand des Kontextselektors einschließlich der
/// kontextbezogenen Validierung und der kanonischen Ziel-URL.
/// </summary>
public sealed class ContextSelectionDraft
{
    public KnowledgeReadContextKind SelectedKind { get; set; } = KnowledgeReadContextKind.Current;

    public string? SnapshotIdInput { get; set; }

    public string? ReleaseIdInput { get; set; }

    public string? SelectedReleaseId { get; set; }

    public string? TransactionIdInput { get; set; }

    public string? SelectedTransactionId { get; set; }

    public void ApplyInitialContext(ReadContext? initialContext)
    {
        if (initialContext is null)
        {
            SelectedKind = KnowledgeReadContextKind.Current;
            return;
        }

        if (initialContext.TransactionId is { } transactionId)
        {
            SelectedKind = KnowledgeReadContextKind.Transaction;
            SelectedTransactionId = transactionId.Value.ToString("D");
            TransactionIdInput = SelectedTransactionId;
            return;
        }

        if (initialContext.SnapshotId is { } snapshotId)
        {
            SelectedKind = KnowledgeReadContextKind.Snapshot;
            SnapshotIdInput = snapshotId.Value.ToString(CultureInfo.InvariantCulture);
            return;
        }

        SelectedKind = KnowledgeReadContextKind.Current;
    }

    public Result<ReadContext> BuildReadContext(ContextSelectionOptionsViewModel options) => SelectedKind switch
    {
        KnowledgeReadContextKind.Snapshot => BuildSnapshotContext(),
        KnowledgeReadContextKind.Release => BuildReleaseContext(options.Releases),
        KnowledgeReadContextKind.Transaction => BuildTransactionContext(),
        _ => Result<ReadContext>.Success(new ReadContext())
    };

    public string BuildTargetUrl(Uri currentUri, ContextSelectorMode mode, string? selectedRoleId)
    {
        var path = string.Equals(currentUri.AbsolutePath, "/", StringComparison.Ordinal)
            ? "/knowledge"
            : currentUri.AbsolutePath;
        var queryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(selectedRoleId))
        {
            queryParts.Add($"roleId={Uri.EscapeDataString(selectedRoleId)}");
        }

        if (mode == ContextSelectorMode.Full)
        {
            AppendSelectedContextQuery(queryParts);
        }
        else
        {
            AppendPreservedContextQuery(currentUri, queryParts);
        }

        return queryParts.Count == 0 ? path : $"{path}?{string.Join("&", queryParts)}";
    }

    private Result<ReadContext> BuildSnapshotContext()
    {
        if (string.IsNullOrWhiteSpace(SnapshotIdInput) ||
            !long.TryParse(SnapshotIdInput, NumberStyles.None, CultureInfo.InvariantCulture, out var snapshotId) ||
            snapshotId <= 0)
        {
            return Invalid("Bitte geben Sie eine gültige positive Snapshot-ID ein.");
        }

        return Result<ReadContext>.Success(new ReadContext(SnapshotId: new SnapshotId(snapshotId)));
    }

    private Result<ReadContext> BuildReleaseContext(IReadOnlyList<ContextSelectionReleaseOptionViewModel> releases)
    {
        var releaseId = SelectedReleaseId ?? ReleaseIdInput;
        if (string.IsNullOrWhiteSpace(releaseId) ||
            !long.TryParse(releaseId, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedReleaseId) ||
            parsedReleaseId <= 0)
        {
            return Invalid("Bitte wählen Sie einen gültigen Release aus.");
        }

        var release = releases.FirstOrDefault(option => option.Id == releaseId);
        return release is null
            ? Result<ReadContext>.Success(new ReadContext())
            : Result<ReadContext>.Success(new ReadContext(SnapshotId: new SnapshotId(release.SnapshotId)));
    }

    private Result<ReadContext> BuildTransactionContext()
    {
        var transactionId = SelectedTransactionId ?? TransactionIdInput;
        if (string.IsNullOrWhiteSpace(transactionId) ||
            !Guid.TryParseExact(transactionId, "D", out var parsedTransactionId))
        {
            return Invalid("Bitte wählen Sie eine gültige Transaktions-ID aus (GUID im Format D).");
        }

        return Result<ReadContext>.Success(new ReadContext(TransactionId: new TransactionId(parsedTransactionId)));
    }

    private void AppendSelectedContextQuery(List<string> queryParts)
    {
        switch (SelectedKind)
        {
            case KnowledgeReadContextKind.Snapshot:
                AppendQuery(queryParts, "snapshotId", SnapshotIdInput);
                break;
            case KnowledgeReadContextKind.Release:
                AppendQuery(queryParts, "releaseId", SelectedReleaseId ?? ReleaseIdInput);
                break;
            case KnowledgeReadContextKind.Transaction:
                AppendQuery(queryParts, "transactionId", SelectedTransactionId ?? TransactionIdInput);
                break;
        }
    }

    private static void AppendQuery(List<string> queryParts, string name, string? value)
    {
        if (value is not null)
        {
            queryParts.Add($"{name}={Uri.EscapeDataString(value)}");
        }
    }

    private static void AppendPreservedContextQuery(Uri uri, List<string> queryParts)
    {
        var query = QueryHelpers.ParseQuery(uri.Query);
        foreach (var name in new[] { "snapshotId", "releaseId", "transactionId" })
        {
            if (query.TryGetValue(name, out var value))
            {
                queryParts.Add($"{name}={Uri.EscapeDataString(value[0]!)}");
                return;
            }
        }
    }

    private static Result<ReadContext> Invalid(string message) => Result<ReadContext>.Failure(new DomainError(
        ReadContextErrorCodes.InvalidReadContext,
        message));
}
