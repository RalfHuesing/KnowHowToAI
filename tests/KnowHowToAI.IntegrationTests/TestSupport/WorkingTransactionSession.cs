using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Mutations.Audiences;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Validation;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Storage.SqlServer.Configuration;
using KnowHowToAI.Storage.SqlServer.Repositories.Knowledge;
using KnowHowToAI.Storage.SqlServer.Repositories.Transactions;

namespace KnowHowToAI.IntegrationTests.TestSupport;

/// <summary>
/// Führt eine komplette Working-Transaction ausschließlich über die produktiven
/// Mutation-Pfade (Node-, Content- und Role-Mutation) und schließt sie per Commit ab.
/// Erwartete Fachfehler werden als Testfehler gemeldet, damit Abnahmetests nicht
/// stillschweigend gegen falsch geseedete Zustände laufen.
/// </summary>
public sealed class WorkingTransactionSession : IAsyncDisposable
{
    private WorkingTransactionSession(
        KnowledgeTransaction transaction,
        NodeMutationApplicationService nodes,
        ContentMutationApplicationService contents,
        AudienceMutationService roles)
    {
        Transaction = transaction;
        Nodes = nodes;
        Contents = contents;
        Roles = roles;
    }

    public KnowledgeTransaction Transaction { get; }

    public TransactionId TransactionId => Transaction.TransactionId;

    private NodeMutationApplicationService Nodes { get; }

    private ContentMutationApplicationService Contents { get; }

    private AudienceMutationService Roles { get; }

    private long ExpectedChangeVersion { get; set; }

    public static async Task<WorkingTransactionSession> BeginAsync(
        SqlTestDatabase database,
        TransactionId transactionId,
        IIdentifierGenerator identifierGenerator,
        string purpose = "Abnahmetest")
    {
        var policy = new SqlStoragePolicy { CommandTimeoutSeconds = 60 };
        var validationPolicy = new ValidationPolicy
        {
            ContentSizeWarningBytes = 4096,
            ChildCountWarning = 50,
            HierarchyDepthWarning = 10,
            PossibleEmbeddedHeadingWarning = false
        };
        var transactionRepository = new SqlTransactionRepository(database.ConnectionFactory, policy);
        var transaction = await transactionRepository.BeginAsync(
            new BeginTransactionRequest(transactionId, purpose, "Abnahmetest", "xUnit")).ConfigureAwait(false);

        var session = new WorkingTransactionSession(
            transaction,
            new NodeMutationApplicationService(
                new SqlNodeMutationRepository(database.ConnectionFactory, policy),
                new NodeMutationService(identifierGenerator),
                validationPolicy),
            new ContentMutationApplicationService(
                new SqlContentMutationRepository(database.ConnectionFactory, policy),
                new ContentMutationService(new ContentRevisionService(identifierGenerator)),
                validationPolicy),
            new AudienceMutationService(new SqlRoleMutationRepository(database.ConnectionFactory, policy)));
        session.ExpectedChangeVersion = transaction.ChangeVersion;
        return session;
    }

    public async Task<Node> CreateNodeAsync(NodeId? parentNodeId, string title, string? description, int sortOrder)
    {
        var result = await Nodes.CreateAsync(
            TransactionId,
            new CreateNodeRequest(parentNodeId, title, description, sortOrder)).ConfigureAwait(false);
        var value = Require(result);
        ExpectedChangeVersion = value.ChangeVersion;
        return value.Node;
    }

    public async Task<Node> DeleteNodeSubtreeAsync(NodeId nodeId)
    {
        var result = await Nodes.DeleteAsync(TransactionId, nodeId, deleteSubtree: true).ConfigureAwait(false);
        var value = Require(result);
        ExpectedChangeVersion = value.ChangeVersion;
        return value.Node;
    }

    public async Task<Node> UpdateNodeAsync(NodeId nodeId, string title, string? description)
    {
        var result = await Nodes.UpdateAsync(
            TransactionId,
            new UpdateNodeRequest(nodeId, title, description)).ConfigureAwait(false);
        var value = Require(result);
        ExpectedChangeVersion = value.ChangeVersion;
        return value.Node;
    }

    public async Task<NodeContent> ReplaceIndependentContentAsync(NodeId nodeId, AudienceId roleId, string contentMd) =>
        await ReplaceContentAsync(nodeId, roleId, ContentMode.Independent, contentMd, []).ConfigureAwait(false);

    public async Task<NodeContent> ReplaceDerivedContentAsync(
        NodeId nodeId,
        AudienceId roleId,
        string contentMd,
        IReadOnlyList<ContentDependencySource> sources) =>
        await ReplaceContentAsync(nodeId, roleId, ContentMode.Derived, contentMd, sources).ConfigureAwait(false);

    public async Task<Audience> CreateRoleAsync(string name, string? description)
    {
        var result = await Roles.CreateAudienceMutationAsync(TransactionId, name, description, ExpectedChangeVersion).ConfigureAwait(false);
        var value = Require(result);
        ExpectedChangeVersion = value.ChangeVersion;
        return value.Audience;
    }

    public async Task<Audience> UpdateRoleDescriptionAsync(AudienceId roleId, string name, string? description)
    {
        var result = await Roles.UpdateAudienceMutationAsync(
            TransactionId,
            new UpdateAudienceMutationRequest(roleId, name, description, ExpectedChangeVersion)).ConfigureAwait(false);
        var value = Require(result);
        ExpectedChangeVersion = value.ChangeVersion;
        return value.Audience;
    }

    public async Task DeleteRoleAsync(AudienceId roleId)
    {
        var result = await Roles.DeleteAudienceMutationAsync(TransactionId, roleId, ExpectedChangeVersion).ConfigureAwait(false);
        var value = Require(result);
        ExpectedChangeVersion = value.ChangeVersion;
    }

    public async Task SetResolutionAsync(AudienceId requestedRoleId, params AudienceId[] candidateRoleIds)
    {
        var result = await Roles.SetAudienceResolutionMutationAsync(
            TransactionId, requestedRoleId, candidateRoleIds, ExpectedChangeVersion).ConfigureAwait(false);
        var value = Require(result);
        ExpectedChangeVersion = value.ChangeVersion;
    }

    public async Task<KnowledgeTransaction> CommitAsync(SqlTestDatabase database, string commitMessage)
    {
        var transactionRepository = new SqlTransactionRepository(
            database.ConnectionFactory,
            new SqlStoragePolicy { CommandTimeoutSeconds = 60 });
        var result = await transactionRepository.CommitAsync(
            new CommitTransactionRequest(
                TransactionId,
                commitMessage,
                new QualityWarningThresholds(4096, 50, 10),
                WarnOnPossibleEmbeddedHeading: false)).ConfigureAwait(false);
        if (!result.IsCommitted)
            throw new InvalidOperationException(
                $"Commit abgelehnt: {result.Error?.Code}: {result.Error?.Message}");
        return result.Transaction!;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<NodeContent> ReplaceContentAsync(
        NodeId nodeId,
        AudienceId roleId,
        ContentMode contentMode,
        string contentMd,
        IReadOnlyList<ContentDependencySource> sources)
    {
        var result = await Contents.ReplaceContentAsync(
            TransactionId,
            new ReplaceContentRequest(nodeId, roleId, contentMode, contentMd, sources, ExpectedChangeVersion)).ConfigureAwait(false);
        var value = Require(result);
        ExpectedChangeVersion = value.ChangeVersion;
        return value.Content;
    }

    private static T Require<T>(Result<T> result) =>
        result.IsSuccess
            ? result.Value!
            : throw new InvalidOperationException($"Mutation unerwartet fehlgeschlagen: {result.Code}: {result.Message}");
}
