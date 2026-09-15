using Dapper;
using KnowHowToAI.Storage.SqlServer.Configuration;

namespace KnowHowToAI.Storage.SqlServer.Connections;

/// <summary>
/// Baut Dapper-Commands mit der zentral konfigurierten Timeout- und
/// Cancellation-Policy. Aufrufer übergeben ausschließlich feste SQL-Konstanten;
/// fachliche Eingaben gehören in <paramref name="parameters"/>.
/// </summary>
internal static class SqlCommandFactory
{
    public static CommandDefinition Create(
        string commandText,
        object? parameters,
        SqlStoragePolicy policy,
        CancellationToken cancellationToken) =>
        new(
            commandText,
            parameters,
            commandTimeout: policy.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);
}
