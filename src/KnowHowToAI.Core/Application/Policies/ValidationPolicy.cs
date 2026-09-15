namespace KnowHowToAI.Core.Application.Policies;

/// <summary>
/// Immutable Policy-Record für Content-Größen-, Struktur- und Heading-Warnungen.
/// Wird vom Composition Root aus der App-Konfiguration übergeben.
/// Domain und Application kennen weder IConfiguration noch IOptions.
/// </summary>
public sealed record ValidationPolicy
{
    /// <summary>Warnschwelle in UTF-8-Bytes für einen einzelnen normalisierten ContentMd (NodeTooLarge).</summary>
    public int ContentSizeWarningBytes { get; init; }

    /// <summary>Warnschwelle für die Anzahl direkter Kindknoten eines Parent (TooManyChildren).</summary>
    public int ChildCountWarning { get; init; }

    /// <summary>Warnschwelle für die globale Node-Tiefe im Baum (HierarchyTooDeep).</summary>
    public int HierarchyDepthWarning { get; init; }

    /// <summary>Aktiviert die heuristische Warnung für alleinstehende Ersatztitel (PossibleEmbeddedHeading).</summary>
    public bool PossibleEmbeddedHeadingWarning { get; init; }
}

