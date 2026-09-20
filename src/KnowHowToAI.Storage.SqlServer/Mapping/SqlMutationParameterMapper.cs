using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;

namespace KnowHowToAI.Storage.SqlServer.Mapping;

/// <summary>Erzeugt ausschließlich parametrisierte Werte für gemeinsame Mutationszeilen.</summary>
internal static class SqlMutationParameterMapper
{
    public static object ToContentParameters(NodeContent content) => new
    {
        snapshotId = content.SnapshotId.Value,
        nodeId = content.NodeId.Value,
        roleId = content.AudienceId.Value,
        contentRevisionId = content.ContentRevisionId.Value,
        contentMode = content.ContentMode.ToString(),
        content.ContentMd,
        content.IsDeleted
    };

    public static object ToDependencyParameters(ContentDependency dependency) => new
    {
        snapshotId = dependency.SnapshotId.Value,
        targetNodeId = dependency.TargetNodeId.Value,
        targetRoleId = dependency.TargetAudienceId.Value,
        sourceNodeId = dependency.SourceNodeId.Value,
        sourceRoleId = dependency.SourceAudienceId.Value,
        sourceContentRevisionId = dependency.SourceContentRevisionId.Value
    };
}
