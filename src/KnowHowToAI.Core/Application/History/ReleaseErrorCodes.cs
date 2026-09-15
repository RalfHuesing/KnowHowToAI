namespace KnowHowToAI.Core.Application.History;

/// <summary>Stabile Fehlercodes fuer Release-Use-Cases.</summary>
public static class ReleaseErrorCodes
{
    public const string SnapshotNotFound = "SnapshotNotFound";
    public const string SnapshotNotCommitted = "SnapshotNotCommitted";
    public const string ReleaseNotFound = "ReleaseNotFound";
    public const string ReleaseNameConflict = "ReleaseNameConflict";
    public const string ReleaseNameRequired = "ReleaseNameRequired";
    public const string InvalidCursor = "InvalidCursor";

    public const string ReleaseIdDetail = "releaseId";
    public const string ReleaseNameDetail = "releaseName";
    public const string SnapshotIdDetail = "snapshotId";
}
