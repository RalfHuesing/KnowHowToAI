using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Mutations.Nodes;

/// <summary>Beschreibt die vor einer globalen Node-Löschung sichtbaren Auswirkungen.</summary>
public sealed record NodeDeletionPreview(
    NodeId NodeId,
    string Title,
    bool IsRoot,
    int DirectChildCount,
    int SubtreeNodeCount,
    int ContentCount,
    int RemovedDependencyCount,
    int RetainedSourceDependencyCount,
    long ChangeVersion);
