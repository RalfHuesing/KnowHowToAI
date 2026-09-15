using Dapper;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.Storage.SqlServer.Repositories;

/// <summary>
/// Stellt einer Repository-Mutation ausschließlich den bereits geprüften Working
/// Snapshot und die kurze SQL-Transaction bereit.
/// </summary>
internal sealed class SqlWorkingSnapshotMutationContext
{
    private readonly SqlConnection _connection;
    private readonly SqlTransaction _transaction;
    private readonly SqlStoragePolicy _storagePolicy;

    internal SqlWorkingSnapshotMutationContext(
        SqlConnection connection,
        SqlTransaction transaction,
        SqlStoragePolicy storagePolicy,
        SnapshotId workingSnapshotId)
    {
        _connection = connection;
        _transaction = transaction;
        _storagePolicy = storagePolicy;
        WorkingSnapshotId = workingSnapshotId;
    }

    public SnapshotId WorkingSnapshotId { get; }

    /// <summary>
    /// Führt eine Repository-eigene SQL-Konstante innerhalb der geprüften kurzen
    /// Transaction aus. Fachliche Eingaben gehören ausschließlich in Parameter.
    /// </summary>
    public Task<int> ExecuteAsync(
        string commandText,
        object? parameters,
        CancellationToken cancellationToken) =>
        _connection.ExecuteAsync(
            SqlCommandFactory.Create(
                commandText,
                parameters,
                _storagePolicy,
                cancellationToken,
                _transaction));

    public Task<IEnumerable<T>> QueryAsync<T>(
        string commandText,
        object? parameters,
        CancellationToken cancellationToken) =>
        _connection.QueryAsync<T>(
            SqlCommandFactory.Create(
                commandText,
                parameters,
                _storagePolicy,
                cancellationToken,
                _transaction));
}

/// <summary>Beschreibt, ob die ausgeführte Repository-Operation Zustand geändert hat.</summary>
internal readonly record struct SqlWorkingSnapshotMutationResult<TResult>(TResult Value, bool StateChanged);

/// <summary>Gibt das Mutationsergebnis samt danach gültiger ChangeVersion zurück.</summary>
internal readonly record struct SqlWorkingSnapshotMutationExecution<TResult>(TResult Value, long ChangeVersion);

/// <summary>Signalisiert einen abgelehnten Zugriff auf keinen mehr bearbeitbaren Snapshot.</summary>
internal sealed class WorkingSnapshotMutationRejectedException : InvalidOperationException
{
    public WorkingSnapshotMutationRejectedException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
