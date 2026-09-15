using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.Transactions;

/// <summary>Liest alle für eine vollständige Working-Snapshot-Validierung benötigten Fachbereiche.</summary>
public sealed class WorkingSnapshotValidationDataReader
{
    private readonly IHierarchyRepository _hierarchyRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IContentRepository _contentRepository;
    private readonly IDependencyRepository _dependencyRepository;

    public WorkingSnapshotValidationDataReader(
        IHierarchyRepository hierarchyRepository,
        IRoleRepository roleRepository,
        IContentRepository contentRepository,
        IDependencyRepository dependencyRepository)
    {
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _contentRepository = contentRepository ?? throw new ArgumentNullException(nameof(contentRepository));
        _dependencyRepository = dependencyRepository ?? throw new ArgumentNullException(nameof(dependencyRepository));
    }

    public async Task<WorkingSnapshotValidationData> ReadAsync(
        SnapshotId snapshotId,
        CancellationToken cancellationToken = default)
    {
        var nodesTask = _hierarchyRepository.ListBySnapshotAsync(snapshotId, cancellationToken);
        var rolesTask = _roleRepository.ListBySnapshotAsync(snapshotId, cancellationToken);
        var resolutionsTask = _roleRepository.ListResolutionsBySnapshotAsync(snapshotId, cancellationToken);
        var contentsTask = _contentRepository.ListBySnapshotAsync(snapshotId, cancellationToken);
        var dependenciesTask = _dependencyRepository.ListBySnapshotAsync(snapshotId, cancellationToken);
        await Task.WhenAll(nodesTask, rolesTask, resolutionsTask, contentsTask, dependenciesTask).ConfigureAwait(false);

        return new WorkingSnapshotValidationData(
            await nodesTask.ConfigureAwait(false),
            await rolesTask.ConfigureAwait(false),
            await resolutionsTask.ConfigureAwait(false),
            await contentsTask.ConfigureAwait(false),
            await dependenciesTask.ConfigureAwait(false));
    }
}

/// <summary>Unveränderlicher, vollständiger Lesestand für den Domain-Validator.</summary>
public sealed record WorkingSnapshotValidationData(
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<Role> Roles,
    IReadOnlyList<RoleResolution> RoleResolutions,
    IReadOnlyList<NodeContent> Contents,
    IReadOnlyList<ContentDependency> Dependencies);
