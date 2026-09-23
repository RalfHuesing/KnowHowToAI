using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Server.Web.Workflow;

namespace KnowHowToAI.Server.Web.Features.Content;

/// <summary>Schreibt bearbeiteten Markdown-Inhalt als koordinierte Web-Mutation.</summary>
public sealed class ContentWriteWorkflow(
    ContentMutationApplicationService mutationService,
    WebWriteCoordinator writeCoordinator) : IContentWriteWorkflow
{
    public async Task<Result<ContentMutationUseCaseResult>> SaveAsync(
        SaveContentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var write = await writeCoordinator.WriteAsync(
            command.LoadedCurrentSnapshotId,
            (transactionId, changeVersion, token) =>
                mutationService.ReplaceContentAsync(
                    transactionId,
                    new ReplaceContentRequest(
                        new NodeId(command.NodeId),
                        new AudienceId(command.AudienceId),
                        ContentMode.Independent,
                        command.Markdown,
                        [],
                        changeVersion ?? command.ExpectedChangeVersion
                            ?? throw new InvalidOperationException("Die Arbeitskopie enthält keine Änderungsversion.")),
                    token),
            value => value.ChangeVersion,
            cancellationToken);

        return write.Mutation;
    }
}
