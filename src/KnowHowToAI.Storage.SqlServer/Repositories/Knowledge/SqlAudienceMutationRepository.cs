using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Mutations.Audiences;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Connections;
using KnowHowToAI.Storage.SqlServer.Mapping;
using KnowHowToAI.Storage.SqlServer.Repositories;

namespace KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;

/// <summary>Persistiert Zielgruppen und Auflösungsreihenfolgen atomar im gesperrten Working Snapshot.</summary>
internal sealed class SqlAudienceMutationRepository : SqlRepository, IAudienceMutationRepository
{
    private const string AudiencesSql = "SELECT SnapshotId, AudienceId, Name, Description, IsDeleted FROM dbo.KnowHowToAI_Audience WHERE SnapshotId = @snapshotId;";
    private const string ResolutionsSql = "SELECT SnapshotId, RequestedAudienceId, CandidateAudienceId, Priority FROM dbo.KnowHowToAI_AudienceResolution WHERE SnapshotId = @snapshotId;";
    private const string ContentsSql = "SELECT SnapshotId, NodeId, AudienceId, ContentRevisionId, ContentMode, ContentMd, IsDeleted FROM dbo.KnowHowToAI_NodeContent WHERE SnapshotId = @snapshotId;";
    private const string DependenciesSql = "SELECT SnapshotId, TargetNodeId, TargetAudienceId, SourceNodeId, SourceAudienceId, SourceContentRevisionId FROM dbo.KnowHowToAI_ContentDependency WHERE SnapshotId = @snapshotId;";
    private const string InsertAudienceSql = "INSERT INTO dbo.KnowHowToAI_Audience (SnapshotId, AudienceId, Name, Description, IsDeleted) VALUES (@snapshotId, @audienceId, @name, @description, @isDeleted);";
    private const string UpdateAudienceSql = "UPDATE dbo.KnowHowToAI_Audience SET Name=@name, Description=@description, IsDeleted=@isDeleted WHERE SnapshotId=@snapshotId AND AudienceId=@audienceId;";
    private const string DeleteResolutionsSql = "DELETE FROM dbo.KnowHowToAI_AudienceResolution WHERE SnapshotId=@snapshotId;";
    private const string InsertResolutionSql = "INSERT INTO dbo.KnowHowToAI_AudienceResolution (SnapshotId, RequestedAudienceId, CandidateAudienceId, Priority) VALUES (@snapshotId,@requestedAudienceId,@candidateAudienceId,@priority);";

    public SqlAudienceMutationRepository(SqlConnectionFactory connectionFactory, SqlStoragePolicy storagePolicy) : base(connectionFactory, storagePolicy) { }

    public async Task<Result<WorkingAudienceMutationExecution<T>>> ExecuteAsync<T>(TransactionId transactionId, Func<WorkingAudienceMutationState, Result<WorkingAudienceMutationDecision<T>>> mutate, long expectedChangeVersion, CancellationToken cancellationToken = default)
    {
        WorkingAudienceMutationState? previous = null;
        try
        {
            var execution = await ExecuteWorkingSnapshotMutationAsync(transactionId, async (context, token) =>
            {
                previous = await ReadAsync(context, token).ConfigureAwait(false);
                var decisionResult = mutate(previous);
                if (!decisionResult.IsSuccess)
                    return new SqlWorkingSnapshotMutationResult<Result<WorkingAudienceMutationDecision<T>>>(Result<WorkingAudienceMutationDecision<T>>.Failure(decisionResult.Error!), false);
                var decision = decisionResult.Value!;
                var changed = !SetEquals(previous.Audiences, decision.State.Audiences) || !SetEquals(previous.Resolutions, decision.State.Resolutions);
                if (changed) await SaveAsync(context, previous, decision.State, token).ConfigureAwait(false);
                return new SqlWorkingSnapshotMutationResult<Result<WorkingAudienceMutationDecision<T>>>(Result<WorkingAudienceMutationDecision<T>>.Success(decision), changed);
            }, cancellationToken: cancellationToken, expectedChangeVersion: expectedChangeVersion).ConfigureAwait(false);
            if (!execution.Value.IsSuccess) return Result<WorkingAudienceMutationExecution<T>>.Failure(execution.Value.Error!);
            var decision = execution.Value.Value!;
            return Result<WorkingAudienceMutationExecution<T>>.Success(new WorkingAudienceMutationExecution<T>(decision.Value, decision.State.SnapshotId, execution.ChangeVersion, previous!, decision.State));
        }
        catch (WorkingSnapshotMutationRejectedException exception)
        {
            var details = new Dictionary<string, string> { ["transactionId"] = transactionId.ToString() };
            if (exception.ExpectedChangeVersion is { } expected)
                details[TransactionValidationErrorCodes.ExpectedChangeVersionDetail] = expected.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (exception.ActualChangeVersion is { } actual)
                details[TransactionValidationErrorCodes.ActualChangeVersionDetail] = actual.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return Result<WorkingAudienceMutationExecution<T>>.Failure(new DomainError(exception.Code, exception.Message, details));
        }
    }

    private static async Task<WorkingAudienceMutationState> ReadAsync(SqlWorkingSnapshotMutationContext context, CancellationToken token)
    {
        var p = new { snapshotId = context.WorkingSnapshotId.Value };
        var audiences = await context.QueryAsync<AudienceRow>(AudiencesSql, p, token).ConfigureAwait(false);
        var resolutions = await context.QueryAsync<AudienceResolutionRow>(ResolutionsSql, p, token).ConfigureAwait(false);
        var contents = await context.QueryAsync<NodeContentRow>(ContentsSql, p, token).ConfigureAwait(false);
        var dependencies = await context.QueryAsync<ContentDependencyRow>(DependenciesSql, p, token).ConfigureAwait(false);
        return new WorkingAudienceMutationState(context.WorkingSnapshotId, audiences.Select(SqlRowMapper.ToAudience).ToArray(), resolutions.Select(SqlRowMapper.ToAudienceResolution).ToArray(), contents.Select(SqlRowMapper.ToNodeContent).ToArray(), dependencies.Select(SqlRowMapper.ToContentDependency).ToArray());
    }

    private static async Task SaveAsync(SqlWorkingSnapshotMutationContext context, WorkingAudienceMutationState previous, WorkingAudienceMutationState current, CancellationToken token)
    {
        var previousById = previous.Audiences.ToDictionary(audience => audience.AudienceId);
        var inserts = current.Audiences.Where(audience => !previousById.ContainsKey(audience.AudienceId)).Select(ToAudienceParameters).ToArray();
        var updates = current.Audiences.Where(audience => previousById.TryGetValue(audience.AudienceId, out var old) && old != audience).Select(ToAudienceParameters).ToArray();
        if (inserts.Length > 0) await context.ExecuteAsync(InsertAudienceSql, inserts, token).ConfigureAwait(false);
        if (updates.Length > 0) await context.ExecuteAsync(UpdateAudienceSql, updates, token).ConfigureAwait(false);
        if (!SetEquals(previous.Resolutions, current.Resolutions))
        {
            await context.ExecuteAsync(DeleteResolutionsSql, new { snapshotId = context.WorkingSnapshotId.Value }, token).ConfigureAwait(false);
            if (current.Resolutions.Count > 0) await context.ExecuteAsync(InsertResolutionSql, current.Resolutions.Select(ToResolutionParameters), token).ConfigureAwait(false);
        }
    }

    private static bool SetEquals<T>(IReadOnlyList<T> left, IReadOnlyList<T> right) where T : notnull => left.Count == right.Count && new HashSet<T>(left).SetEquals(right);
    private static object ToAudienceParameters(Audience audience) => new { snapshotId = audience.SnapshotId.Value, audienceId = audience.AudienceId.Value, audience.Name, audience.Description, audience.IsDeleted };
    private static object ToResolutionParameters(AudienceResolution resolution) => new { snapshotId = resolution.SnapshotId.Value, requestedAudienceId = resolution.RequestedAudienceId.Value, candidateAudienceId = resolution.CandidateAudienceId.Value, resolution.Priority };
}
