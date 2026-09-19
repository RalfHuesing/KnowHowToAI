using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Server.Web.Features.Search;

/// <summary>
/// Ergänzt eine bereits gemappte Trefferseite um die schlanken, aus der
/// bestehenden Navigation gelesenen Pfade. Im Razor-Rendering erscheinen
/// ausschließlich ViewModels, keine Domain-Typen.
/// </summary>
internal sealed class SearchBreadcrumbLoader
{
    private readonly NavigationService _navigationService;

    public SearchBreadcrumbLoader(NavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    public async Task<SearchPageViewModel> LoadAsync(
        SearchPageViewModel page,
        ReadContext readContext,
        string roleId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var items = new SearchHitViewModel[page.Items.Count];
        for (var index = 0; index < page.Items.Count; index++)
        {
            var item = page.Items[index];
            var breadcrumb = await LoadPathAsync(item, readContext, roleId, cancellationToken).ConfigureAwait(false);
            items[index] = item with { Breadcrumb = breadcrumb };
        }

        return page with { Items = items };
    }

    private async Task<IReadOnlyList<string>> LoadPathAsync(
        SearchHitViewModel hit,
        ReadContext readContext,
        string roleId,
        CancellationToken cancellationToken)
    {
        var path = new List<string>();
        var currentNodeId = hit.NodeId;

        while (true)
        {
            var result = await _navigationService.GetNodeAsync(
                new NodeId(currentNodeId),
                readContext,
                new RoleId(roleId),
                cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess || result.Value?.Node is not { } node)
            {
                return path.Count == 0 ? [hit.Title] : path;
            }

            path.Insert(0, node.Title);
            if (node.ParentNodeId is not { } parentNodeId)
            {
                return path;
            }

            currentNodeId = parentNodeId.Value;
        }
    }
}
