namespace KnowHowToAI.Server.Web.Features.Knowledge;

/// <summary>Beschreibt eine vom nativen Baum angeforderte, serverbestätigte Verschiebung.</summary>
public sealed record TreeMoveRequest(
    Guid SourceNodeId,
    Guid TargetNodeId,
    Guid? TargetParentNodeId,
    int TargetSortOrder,
    TreeMovePosition Position);

/// <summary>Die drei fachlich zulässigen Zielpositionen des nativen Wissensbaums.</summary>
public enum TreeMovePosition
{
    Parent,
    Before,
    After
}
