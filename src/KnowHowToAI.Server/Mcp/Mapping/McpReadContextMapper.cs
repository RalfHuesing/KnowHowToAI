using System.Globalization;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Mcp.Contracts;

namespace KnowHowToAI.Server.Mcp.Mapping;

/// <summary>
/// Mappt die gemeinsamen Selektor-Felder der Read-Tools auf den Application-Read-Kontext.
/// Validiert den gegenseitigen Ausschluss von transactionId und snapshotId und akzeptiert
/// ID-Strings exakt im Format der Tool-Antworten (Round-Trip ohne Umformatierung).
/// </summary>
internal static class McpReadContextMapper
{
    public static Result<ReadContext> ToApplicationContext(McpReadContextRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TransactionId is not null && request.SnapshotId is not null)
            return Result<ReadContext>.Failure(InvalidSelector(
                "transactionId und snapshotId dürfen nicht gleichzeitig gesetzt sein.",
                new Dictionary<string, string>
                {
                    [NavigationErrorCodes.TransactionIdDetail] = request.TransactionId,
                    [NavigationErrorCodes.SnapshotIdDetail] = request.SnapshotId
                }));

        var transactionIdResult = ParseOptionalTransactionId(request.TransactionId);
        if (!transactionIdResult.IsSuccess)
            return Result<ReadContext>.Failure(transactionIdResult.Error!);

        var snapshotIdResult = ParseOptionalSnapshotId(request.SnapshotId);
        if (!snapshotIdResult.IsSuccess)
            return Result<ReadContext>.Failure(snapshotIdResult.Error!);

        return Result<ReadContext>.Success(new ReadContext(
            transactionIdResult.Value,
            snapshotIdResult.Value,
            request.IncludeDeleted ?? false));
    }

    private static Result<TransactionId?> ParseOptionalTransactionId(string? transactionId)
    {
        if (transactionId is null)
            return Result<TransactionId?>.Success(null);

        if (!Guid.TryParseExact(transactionId, "D", out var parsed))
            return Result<TransactionId?>.Failure(InvalidSelector(
                $"Der Wert des Selektors '{NavigationErrorCodes.TransactionIdDetail}' ist keine gültige ID.",
                new Dictionary<string, string>
                {
                    [NavigationErrorCodes.TransactionIdDetail] = transactionId
                }));

        return Result<TransactionId?>.Success(new TransactionId(parsed));
    }

    private static Result<SnapshotId?> ParseOptionalSnapshotId(string? snapshotId)
    {
        if (snapshotId is null)
            return Result<SnapshotId?>.Success(null);

        if (!long.TryParse(snapshotId, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
            || parsed <= 0)
            return Result<SnapshotId?>.Failure(InvalidSelector(
                $"Der Wert des Selektors '{NavigationErrorCodes.SnapshotIdDetail}' ist keine gültige ID.",
                new Dictionary<string, string>
                {
                    [NavigationErrorCodes.SnapshotIdDetail] = snapshotId
                }));

        return Result<SnapshotId?>.Success(new SnapshotId(parsed));
    }

    private static DomainError InvalidSelector(string message, Dictionary<string, string> details) =>
        new(ReadContextErrorCodes.InvalidReadContext, message, details);
}
