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

        var viewModel = KnowledgeNavigationMapper.ToNodeDetailsViewModel(result.Value, request.Read.ChangeVersion);
        if (viewModel is not null && viewModel.SourceRevisions.Count > 0)
        {
            var sources = await ResolveSourceNodeTitlesAsync(
                navigationService,
                request.Read.ReadContext,
                viewModel.SourceRevisions);
            viewModel = viewModel with { SourceRevisions = sources };
        }

        return new NodeDocumentLoadResult(
            viewModel,
            IsNotFound: false,
            ErrorMessage: null);
    }

    private static async Task<IReadOnlyList<SourceRevisionViewModel>> ResolveSourceNodeTitlesAsync(
        NavigationService navigationService,
        ReadContext readContext,
        IReadOnlyList<SourceRevisionViewModel> sourceRevisions)
    {
        var titles = new Dictionary<(Guid NodeId, string AudienceId), string>();
        foreach (var source in sourceRevisions)
        {
            var key = (source.SourceNodeId, source.SourceAudienceId);
            if (titles.ContainsKey(key))
                continue;

            try
            {
                var result = await navigationService.GetNodeAsync(
                    new NodeId(source.SourceNodeId),
                    readContext,
                    new AudienceId(source.SourceAudienceId),
                    CancellationToken.None);
                titles[key] = result.IsSuccess && result.Value?.Node is { } node
                    ? node.Title
                    : "Quellknoten nicht verfügbar";
            }
            catch (Exception)
            {
                titles[key] = "Quellknoten nicht verfügbar";
            }
        }

        return sourceRevisions
            .Select(source => source with
            {
                SourceNodeTitle = titles[(source.SourceNodeId, source.SourceAudienceId)]
            })
            .ToArray();
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
