namespace KnowHowToAI.Server.Web.Features.Content;

/// <summary>Beschreibt die Inhaltseingabe samt gelesenen Versionsständen für einen Web-Speichervorgang.</summary>
public sealed record SaveContentCommand(
    Guid NodeId,
    string AudienceId,
    string Markdown,
    long? ExpectedChangeVersion,
    long? LoadedCurrentSnapshotId);
