using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Domain.Validation;

/// <summary>
/// Aggregiert die bereits unabhängigen Fachvalidatoren zu einem deterministischen Befund
/// für einen vollständigen Working Snapshot.
/// </summary>
public static class TransactionValidator
{
    public static TransactionValidationReport Validate(TransactionValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Nodes);
        ArgumentNullException.ThrowIfNull(request.Roles);
        ArgumentNullException.ThrowIfNull(request.RoleResolutions);
        ArgumentNullException.ThrowIfNull(request.Contents);
        ArgumentNullException.ThrowIfNull(request.Dependencies);
        ArgumentNullException.ThrowIfNull(request.QualityWarningThresholds);

        var nodes = request.Nodes.ToArray();
        var roles = request.Roles.ToArray();
        var roleResolutions = request.RoleResolutions.ToArray();
        var contents = request.Contents.ToArray();
        var dependencies = request.Dependencies.ToArray();
        var errors = new List<DomainError>();
        var warnings = new List<DomainWarning>();

        AddReport(HierarchyValidator.Validate(nodes), errors, warnings);
        AddReport(DependencyValidator.ValidateSnapshot(contents, dependencies), errors, warnings);
        ValidateRoleResolutions(roles, roleResolutions, contents, errors);

        var activeNodesById = nodes
            .Where(node => !node.IsDeleted)
            .GroupBy(node => node.NodeId)
            .ToDictionary(group => group.Key, group => group.First());
        AddContentFindings(
            contents,
            activeNodesById,
            request.QualityWarningThresholds,
            request.WarnOnPossibleEmbeddedHeading,
            errors,
            warnings);
        AddHierarchyWarnings(nodes, request.QualityWarningThresholds, warnings);

        var staleContents = FindStaleContents(contents, dependencies);
        AddWarnings(staleContents.Select(CreateStaleWarning), warnings);

        var distinctWarnings = DistinctAndSort(warnings);
        return new TransactionValidationReport(
            DistinctAndSort(errors),
            distinctWarnings,
            staleContents,
            CreateRefactoringCandidates(distinctWarnings));
    }

    private static void ValidateRoleResolutions(
        IReadOnlyCollection<Role> roles,
        IReadOnlyCollection<RoleResolution> resolutions,
        IReadOnlyCollection<NodeContent> contents,
        ICollection<DomainError> errors)
    {
        var requestedRoleIds = roles.Where(role => !role.IsDeleted).Select(role => role.RoleId)
            .Concat(resolutions.Select(resolution => resolution.RequestedRoleId))
            .Distinct()
            .OrderBy(roleId => roleId.Value, StringComparer.Ordinal);
        var snapshotId = roles.Select(role => role.SnapshotId)
            .Concat(resolutions.Select(resolution => resolution.SnapshotId))
            .Concat(contents.Select(content => content.SnapshotId))
            .DefaultIfEmpty()
            .First();

        foreach (var requestedRoleId in requestedRoleIds)
        {
            var resolution = RoleResolver.Resolve(new RoleResolutionRequest(
                snapshotId,
                default,
                requestedRoleId,
                roles,
                resolutions,
                contents));
            if (!resolution.IsSuccess)
                errors.Add(resolution.Error!);
        }
    }

    private static void AddContentFindings(
        IEnumerable<NodeContent> contents,
        IReadOnlyDictionary<NodeId, Node> activeNodesById,
        QualityWarningThresholds thresholds,
        bool warnOnPossibleEmbeddedHeading,
        ICollection<DomainError> errors,
        ICollection<DomainWarning> warnings)
    {
        foreach (var content in contents.Where(content => !content.IsDeleted)
                     .OrderBy(content => content.NodeId.Value)
                     .ThenBy(content => content.RoleId.Value, StringComparer.Ordinal))
        {
            if (!activeNodesById.TryGetValue(content.NodeId, out var node))
                continue;

            var structureReport = MarkdownStructureValidator.Validate(
                content.ContentMd,
                node.Title,
                warnOnPossibleEmbeddedHeading);
            AddReport(structureReport, errors, warnings, content.NodeId, content.RoleId);
            AddWarnings(
                QualityWarningEvaluator.EvaluateContentSize(content.ContentMd, thresholds)
                    .Select(warning => WithContext(warning, content.NodeId, content.RoleId)),
                warnings);
        }
    }

    private static void AddHierarchyWarnings(
        IReadOnlyCollection<Node> nodes,
        QualityWarningThresholds thresholds,
        ICollection<DomainWarning> warnings)
    {
        var activeNodes = nodes.Where(node => !node.IsDeleted).ToArray();
        var nodesById = activeNodes
            .GroupBy(node => node.NodeId)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var node in activeNodes.OrderBy(node => node.NodeId.Value))
        {
            var children = activeNodes.Where(child => child.ParentNodeId == node.NodeId);
            AddWarnings(
                QualityWarningEvaluator.EvaluateChildCount(children, thresholds)
                    .Select(warning => WithContext(warning, node.NodeId)),
                warnings);

            var depth = CalculateDepth(node, nodesById);
            if (depth is not null)
            {
                AddWarnings(
                    QualityWarningEvaluator.EvaluateHierarchyDepth(depth.Value, thresholds)
                        .Select(warning => WithContext(warning, node.NodeId)),
                    warnings);
            }
        }
    }

    private static int? CalculateDepth(Node node, IReadOnlyDictionary<NodeId, Node> nodesById)
    {
        var visited = new HashSet<NodeId>();
        var depth = 1;
        var current = node;
        while (current.ParentNodeId is { } parentNodeId)
        {
            if (!visited.Add(current.NodeId) || !nodesById.TryGetValue(parentNodeId, out current))
                return null;

            depth++;
        }

        return depth;
    }

    private static IReadOnlyList<StaleContent> FindStaleContents(
        IEnumerable<NodeContent> contents,
        IEnumerable<ContentDependency> dependencies) =>
        contents.Where(content => !content.IsDeleted && content.ContentMode == ContentMode.Derived)
            .Where(content => FreshnessEvaluator.Evaluate(content, contents, dependencies) == Freshness.Stale)
            .Select(content => new StaleContent(content.NodeId, content.RoleId, content.ContentRevisionId))
            .Distinct()
            .OrderBy(content => content.NodeId.Value)
            .ThenBy(content => content.RoleId.Value, StringComparer.Ordinal)
            .ThenBy(content => content.ContentRevisionId.Value)
            .ToArray();

    private static IReadOnlyList<RefactoringCandidate> CreateRefactoringCandidates(
        IEnumerable<DomainWarning> warnings) =>
        warnings.Where(warning => warning.Code is QualityWarningCodes.NodeTooLarge
                or QualityWarningCodes.TooManyChildren
                or QualityWarningCodes.HierarchyTooDeep)
            .Where(warning => warning.Details.TryGetValue(TransactionValidationCodes.NodeIdDetail, out _))
            .GroupBy(warning => new NodeId(Guid.Parse(warning.Details[TransactionValidationCodes.NodeIdDetail])))
            .Select(group => new RefactoringCandidate(
                group.Key,
                Array.AsReadOnly(group.Select(warning => warning.Code)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(code => code, StringComparer.Ordinal)
                    .ToArray())))
            .OrderBy(candidate => candidate.NodeId.Value)
            .ToArray();

    private static DomainWarning CreateStaleWarning(StaleContent content) =>
        new(
            QualityWarningCodes.StaleDerivedContent,
            "Abgeleiteter Content basiert nicht mehr auf aktuellen Quellen.",
            new Dictionary<string, string>
            {
                [TransactionValidationCodes.NodeIdDetail] = content.NodeId.ToString(),
                [TransactionValidationCodes.RoleIdDetail] = content.RoleId.ToString(),
                [TransactionValidationCodes.ContentRevisionIdDetail] = content.ContentRevisionId.ToString()
            });

    private static void AddReport(
        ValidationReport report,
        ICollection<DomainError> errors,
        ICollection<DomainWarning> warnings,
        NodeId? nodeId = null,
        RoleId? roleId = null)
    {
        AddErrors(report.Errors.Select(error => WithContext(error, nodeId, roleId)), errors);
        AddWarnings(report.Warnings.Select(warning => WithContext(warning, nodeId, roleId)), warnings);
    }

    private static DomainError WithContext(DomainError error, NodeId? nodeId, RoleId? roleId) =>
        new(error.Code, error.Message, AddContext(error.Details, nodeId, roleId));

    private static DomainWarning WithContext(DomainWarning warning, NodeId? nodeId, RoleId? roleId = null) =>
        new(warning.Code, warning.Message, AddContext(warning.Details, nodeId, roleId));

    private static void AddErrors(IEnumerable<DomainError> source, ICollection<DomainError> destination)
    {
        foreach (var error in source)
            destination.Add(error);
    }

    private static void AddWarnings(IEnumerable<DomainWarning> source, ICollection<DomainWarning> destination)
    {
        foreach (var warning in source)
            destination.Add(warning);
    }

    private static IReadOnlyDictionary<string, string> AddContext(
        IReadOnlyDictionary<string, string> details,
        NodeId? nodeId,
        RoleId? roleId)
    {
        var contextualDetails = new Dictionary<string, string>(details, StringComparer.Ordinal);
        if (nodeId is { } value)
            contextualDetails[TransactionValidationCodes.NodeIdDetail] = value.ToString();
        if (roleId is { } role)
            contextualDetails[TransactionValidationCodes.RoleIdDetail] = role.ToString();
        return contextualDetails;
    }

    private static IReadOnlyList<TIssue> DistinctAndSort<TIssue>(IEnumerable<TIssue> issues)
        where TIssue : DomainIssue =>
        issues.GroupBy(CreateIssueKey, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(issue => issue.Code, StringComparer.Ordinal)
            .ThenBy(issue => issue.Message, StringComparer.Ordinal)
            .ThenBy(CreateIssueDetailsKey, StringComparer.Ordinal)
            .ToArray();

    private static string CreateIssueKey(DomainIssue issue) =>
        string.Concat(issue.Code, "\u001f", issue.Message, "\u001f", CreateIssueDetailsKey(issue));

    private static string CreateIssueDetailsKey(DomainIssue issue) =>
        string.Join("\u001e", issue.Details
            .OrderBy(detail => detail.Key, StringComparer.Ordinal)
            .ThenBy(detail => detail.Value, StringComparer.Ordinal)
            .Select(detail => string.Concat(detail.Key, "\u001d", detail.Value)));
}
