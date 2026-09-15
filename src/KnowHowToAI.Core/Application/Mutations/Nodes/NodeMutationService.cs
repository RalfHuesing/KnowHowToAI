using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>
/// Wendet Node-Mutationen nur als vollständig validierte, normalisierte Hierarchiezustände an.
/// </summary>
public sealed class NodeMutationService(IIdentifierGenerator identifierGenerator)
{
    private readonly IIdentifierGenerator _identifierGenerator = identifierGenerator ?? throw new ArgumentNullException(nameof(identifierGenerator));

    public Result<HierarchyMutationResult> Create(
        IEnumerable<Node> existingNodes,
        CreateNodeCommand command,
        IEnumerable<NodeId> knownNodeIds)
    {
        ArgumentNullException.ThrowIfNull(existingNodes);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(knownNodeIds);

        var nodes = existingNodes.ToArray();
        var existingError = FindHierarchyError(nodes);
        if (existingError is not null)
            return Result<HierarchyMutationResult>.Failure(existingError);

        if (string.IsNullOrWhiteSpace(command.Title))
            return Result<HierarchyMutationResult>.Failure(CreateTitleRequiredError());

        var parentError = FindParentError(nodes, command.SnapshotId, command.ParentNodeId, nodeId: null);
        if (parentError is not null)
            return Result<HierarchyMutationResult>.Failure(parentError);

        var nodeId = _identifierGenerator.CreateNodeId();
        if (knownNodeIds.Contains(nodeId) || nodes.Any(node => node.NodeId == nodeId))
        {
            return Result<HierarchyMutationResult>.Failure(CreateNodeIdAlreadyUsedError(nodeId));
        }

        var createdNode = new Node(
            command.SnapshotId,
            nodeId,
            command.ParentNodeId,
            command.Title,
            command.Description,
            command.SortOrder,
            IsDeleted: false);

        return FinalizeMutation(
            nodes.Append(createdNode),
            nodeId,
            command.SnapshotId,
            [command.ParentNodeId]);
    }

    public Result<HierarchyMutationResult> Move(IEnumerable<Node> existingNodes, MoveNodeCommand command)
    {
        ArgumentNullException.ThrowIfNull(existingNodes);
        ArgumentNullException.ThrowIfNull(command);

        var nodes = existingNodes.ToArray();
        var existingError = FindHierarchyError(nodes);
        if (existingError is not null)
            return Result<HierarchyMutationResult>.Failure(existingError);

        var node = FindActiveNode(nodes, command.NodeId);
        if (node is null)
            return Result<HierarchyMutationResult>.Failure(CreateNodeNotFoundError(command.NodeId));

        var parentError = FindParentError(nodes, node.SnapshotId, command.ParentNodeId, node.NodeId);
        if (parentError is not null)
            return Result<HierarchyMutationResult>.Failure(parentError);

        return FinalizeMutation(
            Replace(nodes, node with { ParentNodeId = command.ParentNodeId, SortOrder = command.SortOrder }),
            node.NodeId,
            node.SnapshotId,
            [node.ParentNodeId, command.ParentNodeId]);
    }

    public Result<HierarchyMutationResult> Reorder(IEnumerable<Node> existingNodes, ReorderNodeCommand command)
    {
        ArgumentNullException.ThrowIfNull(existingNodes);
        ArgumentNullException.ThrowIfNull(command);

        var nodes = existingNodes.ToArray();
        var existingError = FindHierarchyError(nodes);
        if (existingError is not null)
            return Result<HierarchyMutationResult>.Failure(existingError);

        var node = FindActiveNode(nodes, command.NodeId);
        if (node is null)
            return Result<HierarchyMutationResult>.Failure(CreateNodeNotFoundError(command.NodeId));

        return FinalizeMutation(
            Replace(nodes, node with { SortOrder = command.SortOrder }),
            node.NodeId,
            node.SnapshotId,
            [node.ParentNodeId]);
    }

    public Result<NodeDeletionResult> Delete(
        IEnumerable<Node> existingNodes,
        IEnumerable<NodeContent> existingContents,
        IEnumerable<ContentDependency> existingDependencies,
        DeleteNodeCommand command)
    {
        ArgumentNullException.ThrowIfNull(existingNodes);
        ArgumentNullException.ThrowIfNull(existingContents);
        ArgumentNullException.ThrowIfNull(existingDependencies);
        ArgumentNullException.ThrowIfNull(command);

        var nodes = existingNodes.ToArray();
        var dependencies = existingDependencies.ToArray();
        var hierarchyError = FindHierarchyError(nodes);
        if (hierarchyError is not null)
            return Result<NodeDeletionResult>.Failure(hierarchyError);

        var node = FindActiveNode(nodes, command.NodeId);
        if (node is null)
            return Result<NodeDeletionResult>.Failure(CreateNodeNotFoundError(command.NodeId));

        var directlyAffectedChildren = nodes
            .Where(candidate => !candidate.IsDeleted && candidate.ParentNodeId == node.NodeId)
            .ToArray();
        if (!command.DeleteSubtree && directlyAffectedChildren.Length > 0)
        {
            return Result<NodeDeletionResult>.Failure(new DomainError(
                NodeDeletionErrorCodes.NodeHasActiveChildren,
                "Eine Node mit aktiven Children erfordert eine explizite Subtree-Löschung.",
                new Dictionary<string, string>
                {
                    [HierarchyErrorCodes.NodeIdDetail] = node.NodeId.ToString(),
                    [NodeDeletionErrorCodes.ActiveChildCountDetail] = directlyAffectedChildren.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                }));
        }

        var deletedNodeIds = command.DeleteSubtree
            ? FindActiveSubtreeNodeIds(nodes, node.NodeId)
            : new HashSet<NodeId> { node.NodeId };
        var deletedNodes = SiblingOrderNormalizer.Normalize(
            nodes.Select(candidate =>
                deletedNodeIds.Contains(candidate.NodeId) ? candidate with { IsDeleted = true } : candidate),
            node.SnapshotId,
            [node.ParentNodeId]);
        var deletedContents = existingContents.Select(content =>
            content.SnapshotId == node.SnapshotId && deletedNodeIds.Contains(content.NodeId)
                ? content with { IsDeleted = true }
                : content).ToArray();
        var deletedDependencies = Array.AsReadOnly(dependencies
            .Where(dependency => dependency.SnapshotId != node.SnapshotId
                || !deletedNodeIds.Contains(dependency.TargetNodeId))
            .ToArray());

        return Result<NodeDeletionResult>.Success(new NodeDeletionResult(
            deletedNodes,
            Array.AsReadOnly(deletedContents),
            deletedDependencies));
    }

    private static DomainError? FindHierarchyError(IEnumerable<Node> nodes)
    {
        var report = HierarchyValidator.Validate(nodes);
        return report.Errors.FirstOrDefault();
    }

    private static DomainError? FindParentError(
        IEnumerable<Node> nodes,
        SnapshotId snapshotId,
        NodeId? parentNodeId,
        NodeId? nodeId)
    {
        if (parentNodeId is null)
        {
            if (nodes.Any(candidate =>
                !candidate.IsDeleted
                && candidate.ParentNodeId is null
                && (nodeId is null || candidate.NodeId != nodeId.Value)))
            {
                return new DomainError(
                    HierarchyErrorCodes.RootAlreadyExists,
                    "Ein Snapshot darf höchstens eine aktive Root-Node enthalten.",
                    new Dictionary<string, string>
                    {
                        [HierarchyErrorCodes.SnapshotIdDetail] = snapshotId.ToString()
                    });
            }

            return null;
        }

        if (nodeId is { } currentNodeId && parentNodeId == currentNodeId)
        {
            return new DomainError(
                HierarchyErrorCodes.SelfParentNotAllowed,
                "Eine Node darf nicht ihr eigener Parent sein.",
                new Dictionary<string, string>
                {
                    [HierarchyErrorCodes.NodeIdDetail] = currentNodeId.ToString()
                });
        }

        var parent = FindActiveNode(nodes, parentNodeId.Value);
        if (parent is null || parent.SnapshotId != snapshotId)
        {
            return new DomainError(
                HierarchyErrorCodes.ParentNotFound,
                "Der Parent muss aktiv im selben Snapshot vorhanden sein.",
                new Dictionary<string, string>
                {
                    [HierarchyErrorCodes.ParentNodeIdDetail] = parentNodeId.Value.ToString()
                });
        }

        return null;
    }

    private static Result<HierarchyMutationResult> FinalizeMutation(
        IEnumerable<Node> nodes,
        NodeId changedNodeId,
        SnapshotId snapshotId,
        IEnumerable<NodeId?> affectedParentNodeIds)
    {
        var normalizedNodes = SiblingOrderNormalizer.Normalize(nodes, snapshotId, affectedParentNodeIds);
        var report = HierarchyValidator.Validate(normalizedNodes);
        if (!report.IsValid)
            return Result<HierarchyMutationResult>.Failure(report.Errors[0]);

        var changedNode = FindActiveNode(normalizedNodes, changedNodeId)!;
        return Result<HierarchyMutationResult>.Success(new HierarchyMutationResult(changedNode, normalizedNodes));
    }

    private static Node? FindActiveNode(IEnumerable<Node> nodes, NodeId nodeId) =>
        nodes.FirstOrDefault(node => !node.IsDeleted && node.NodeId == nodeId);

    private static ISet<NodeId> FindActiveSubtreeNodeIds(IEnumerable<Node> nodes, NodeId rootNodeId)
    {
        var childrenByParent = nodes
            .Where(node => !node.IsDeleted && node.ParentNodeId is not null)
            .GroupBy(node => node.ParentNodeId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(node => node.NodeId).ToArray());
        var subtreeNodeIds = new HashSet<NodeId> { rootNodeId };
        var pendingNodeIds = new Queue<NodeId>();
        pendingNodeIds.Enqueue(rootNodeId);

        while (pendingNodeIds.TryDequeue(out var parentNodeId))
        {
            if (!childrenByParent.TryGetValue(parentNodeId, out var childNodeIds))
                continue;

            foreach (var childNodeId in childNodeIds)
            {
                if (subtreeNodeIds.Add(childNodeId))
                    pendingNodeIds.Enqueue(childNodeId);
            }
        }

        return subtreeNodeIds;
    }

    private static IReadOnlyList<Node> Replace(IEnumerable<Node> nodes, Node replacement) =>
        Array.AsReadOnly(nodes.Select(node => node.NodeId == replacement.NodeId ? replacement : node).ToArray());

    private static DomainError CreateTitleRequiredError() =>
        new(
            HierarchyErrorCodes.TitleRequired,
            "Der Titel einer aktiven Node darf nicht leer oder nur Whitespace sein.");

    private static DomainError CreateNodeIdAlreadyUsedError(NodeId nodeId) =>
        new(
            HierarchyErrorCodes.NodeIdAlreadyUsed,
            "Eine NodeId darf nach ihrer Vergabe nicht erneut verwendet werden.",
            new Dictionary<string, string>
            {
                [HierarchyErrorCodes.NodeIdDetail] = nodeId.ToString()
            });

    private static DomainError CreateNodeNotFoundError(NodeId nodeId) =>
        new(
            HierarchyErrorCodes.NodeNotFound,
            "Die angefragte aktive Node existiert nicht.",
            new Dictionary<string, string>
            {
                [HierarchyErrorCodes.NodeIdDetail] = nodeId.ToString()
            });
}
