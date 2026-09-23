using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Server.Web.Features.Content;

/// <summary>Schreibvertrag zwischen ContentEditor und transaktionsbewusstem Inhaltsworkflow.</summary>
public interface IContentWriteWorkflow
{
    Task<Result<ContentMutationUseCaseResult>> SaveAsync(
        SaveContentCommand command,
        CancellationToken cancellationToken = default);
}
