using System.Text.Json;
using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using ModelContextProtocol.Client;

namespace KnowHowToAI.IntegrationTests.TestSupport;

/// <summary>
/// Geteilte HTTP-Vertragshilfsmittel für die MCP-HTTP-Integrationstests:
/// Envelope-Auflösung eines Tool-Aufrufs über den offiziellen SDK-McpClient
/// sowie ein gültigen Working-Snapshot-Zustand lieferndes Validierungs-Double.
/// </summary>
public static class McpHttpToolCalls
{
    public static async Task<JsonDocument> CallAsync(
        McpClient client,
        string toolName,
        Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(toolName, arguments);
        return JsonDocument.Parse(result.Content.Single().ToString()!);
    }
}

public sealed class ValidatingWorkingSnapshotRepository : IWorkingSnapshotValidationDataRepository
{
    public Task<Result<WorkingSnapshotValidationData>> ReadOpenWorkingAsync(
        TransactionId transactionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<WorkingSnapshotValidationData>.Success(
            new WorkingSnapshotValidationData([], [], [], [], [])));
}
