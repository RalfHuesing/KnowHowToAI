using KnowHowToAI.Core.Application.Navigation;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Tree;

/// <summary>
/// Schmale Präsentationsgrenze zwischen Wissensseite, Baum und dem internen
/// Lazy-Loading-Adapter. Sie erlaubt den Komponenten, ihre Zusammenarbeit
/// ohne Kenntnis von Cache, Navigation oder Request-Cancellation zu testen.
/// </summary>
public interface IKnowledgeTreeWorkspace
{
    KnowledgeTreeNodeViewModel? VisualRootNode { get; }

    Guid? SelectedNodeId { get; }

    string? StatusMessage { get; }

    bool IsLoading { get; }

    string? RootError { get; }

    IReadOnlyList<KnowledgeTreeNodeViewModel> Breadcrumbs { get; }

    bool HasContext(ReadContext readContext, string audienceId);

    Task InitializeAsync(ReadContext readContext, string audienceId, CancellationToken cancellationToken = default);

    Task SelectNodeAsync(Guid? nodeId, CancellationToken cancellationToken = default);

    Task ExpandNodeAsync(Guid nodeId, CancellationToken cancellationToken = default);

    void CollapseNode(Guid nodeId);

    Task PageNextAsync(Guid nodeId, CancellationToken cancellationToken = default);

    Task PagePreviousAsync(Guid nodeId, CancellationToken cancellationToken = default);

    event Action? Changed;
}
