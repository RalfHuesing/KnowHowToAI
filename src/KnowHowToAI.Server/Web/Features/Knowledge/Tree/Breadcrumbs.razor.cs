using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Tree;

/// <summary>
/// Hierarchische Breadcrumbs-Navigation für den ausgewählten Wissenspfad.
/// </summary>
public sealed partial class Breadcrumbs
{
    [Parameter]
    public IReadOnlyList<KnowledgeTreeNodeViewModel>? Items { get; set; }

    [Parameter]
    public EventCallback<Guid> OnSelectNode { get; set; }
}
