namespace KnowHowToAI.Server.Mcp.Contracts;

/// <summary>
/// Zentraler, stabiler Fehler- und Warncode-Katalog für V1
/// Neue Codes dürfen ergänzt werden; veröffentlichte Codes werden nicht beiläufig umbenannt.
/// </summary>
public static class McpErrorCatalog
{
    private static readonly string[] ContextStateErrorCodes =
    [
        "InvalidReadContext", "SnapshotNotFound", "SnapshotNotCommitted", "TransactionNotFound",
        "TransactionClosed", "SnapshotConflict", "ChangeVersionConflict", "SnapshotMutationConflict", "InvalidCursor", "CursorExpired",
        "WorkingSnapshotNotOpen", "TransactionDiscarded"
    ];

    private static readonly string[] StructureRoleErrorCodes =
    [
        "NodeNotFound", "InvalidNodeId", "RootAlreadyExists", "ParentNodeNotFound", "InvalidHierarchy", "NodeHasChildren",
        "RoleNotFound", "RoleInUse", "RoleResolutionNotConfigured", "InvalidRoleResolution",
        "DuplicateNodeId", "HierarchyCycle", "NodeIdAlreadyUsed", "SelfParentNotAllowed", "SnapshotMismatch",
        "TitleRequired", "TitleTooLong", "DescriptionTooLong", "RoleNameRequired", "RoleIdRequired", "CandidateRoleDeleted",
        "CandidateRoleNotFound", "DuplicateCandidateRole", "DuplicatePriority", "InvalidPriority",
        "RequestedRoleDeleted", "RequestedRoleNotFound"
    ];

    private static readonly string[] ContentErrorCodes =
    [
        "ExplicitContentNotFound", "HeadingNotAllowed", "FrontMatterNotAllowed", "RawHtmlNotAllowed",
        "LinkTargetNotAllowed", "ExternalImageNotAllowed", "TextNotFound",
        "MultipleTextMatches", "InvalidDependency", "DependencyCycle"
    ];

    private static readonly string[] MigrationReleaseErrorCodes =
    [
        "MigrationChecksumMismatch", "MigrationFailed", "ReleaseNotFound", "ReleaseNameConflict",
        "ReleaseNameRequired"
    ];

    private static readonly string[] WarningCodes =
    [
        "NodeTooLarge", "PossibleEmbeddedHeading", "TooManyChildren", "HierarchyTooDeep",
        "LargeContentReplace", "StaleDerivedContent"
    ];

    public static IReadOnlyList<string> KnownErrorCodes { get; } =
    [
        .. ContextStateErrorCodes,
        .. StructureRoleErrorCodes,
        .. ContentErrorCodes,
        .. MigrationReleaseErrorCodes
    ];

    public static IReadOnlyList<string> KnownWarningCodes { get; } = WarningCodes;
}
