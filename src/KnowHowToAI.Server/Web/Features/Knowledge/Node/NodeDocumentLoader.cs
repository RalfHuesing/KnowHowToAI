using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Node;

internal static class NodeDocumentLoader
{
    public static async Task<NodeDocumentLoadResult> LoadAsync(
        NavigationService navigationService,
        NodeDocumentRequest request)
    {
        if (request.Read.NodeId is not { } id
            || request.Read.ReadContext is null
            || string.IsNullOrWhiteSpace(request.Read.AudienceId))
            return NodeDocumentLoadResult.Empty;

        var result = await navigationService.GetNodeAsync(
            new NodeId(id),
            request.Read.ReadContext,
            new AudienceId(request.Read.AudienceId),
            CancellationToken.None);
        if (!result.IsSuccess)
        {
            return string.Equals(result.Error!.Code, "NodeNotFound", StringComparison.Ordinal)
                ? NodeDocumentLoadResult.NotFound
                : NodeDocumentLoadResult.Failed(result.Error.Message);
        }

        return new NodeDocumentLoadResult(
            KnowledgeNavigationMapper.ToNodeDetailsViewModel(result.Value, request.Read.ChangeVersion),
            IsNotFound: false,
            ErrorMessage: null);
    }
}

internal sealed record NodeDocumentRequest(
    NodeReadRequest Read);

internal sealed record NodeReadRequest(
    Guid? NodeId,
    ReadContext? ReadContext,
    string? AudienceId,
    long? ChangeVersion);

internal sealed record NodeDocumentLoadResult(
    NodeDetailsViewModel? ViewModel,
    bool IsNotFound,
    string? ErrorMessage)
{
    public static NodeDocumentLoadResult Empty { get; } = new(null, false, null);
    public static NodeDocumentLoadResult NotFound { get; } = new(null, true, null);
    public static NodeDocumentLoadResult Failed(string errorMessage) => new(null, false, errorMessage);
}
