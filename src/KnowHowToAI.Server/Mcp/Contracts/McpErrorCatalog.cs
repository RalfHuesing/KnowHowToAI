namespace KnowHowToAI.Server.Mcp.Contracts;

/// <summary>
/// Zentraler, stabiler Fehler- und Warncode-Katalog für V1
/// (verbindlich: docs/Roadmap.md, Abschnitt 'Stabiler Fehlercode-Katalog für V1').
/// Neue Codes dürfen ergänzt werden; veröffentlichte Codes werden nicht beiläufig umbenannt.
/// </summary>
public static class McpErrorCatalog
{
    private static readonly string[] ContextStateErrorCodes =
    [
        "InvalidReadContext", "SnapshotNotFound", "SnapshotNotCommitted", "TransactionNotFound",
        "TransactionClosed", "SnapshotConflict", "InvalidCursor", "CursorExpired"
    ];

    private static readonly string[] StructureRoleErrorCodes =
    [
        "NodeNotFound", "RootAlreadyExists", "ParentNodeNotFound", "InvalidHierarchy", "NodeHasChildren",
        "RoleNotFound", "RoleInUse", "RoleResolutionNotConfigured", "InvalidRoleResolution"
    ];

    private static readonly string[] ContentErrorCodes =
    [
        "ExplicitContentNotFound", "HeadingNotAllowed", "FrontMatterNotAllowed", "TextNotFound",
        "MultipleTextMatches", "InvalidDependency", "DependencyCycle"
    ];

    private static readonly string[] MigrationReleaseErrorCodes =
    [
        "MigrationChecksumMismatch", "MigrationFailed", "ReleaseNotFound", "ReleaseNameConflict"
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
