namespace KnowHowToAI.Server.Configuration;

/// <summary>
/// Quality-Policies für Inhaltsgröße, Kindknoten-Anzahl und Hierarchietiefe.
/// Ungültige Werte verhindern den Serverstart. Änderungen erfordern einen Prozessneustart.
/// </summary>
internal sealed record ValidationOptions
{
    /// <summary>Warnschwelle in UTF-8-Bytes für einen einzelnen normalisierten ContentMd (NodeTooLarge).</summary>
    public int ContentSizeWarningBytes { get; init; } = 4096;

    /// <summary>Warnschwelle für die Anzahl direkter Kindknoten eines Parent (TooManyChildren).</summary>
    public int ChildCountWarning { get; init; } = 25;

    /// <summary>Warnschwelle für die globale Node-Tiefe im Baum (HierarchyTooDeep).</summary>
    public int HierarchyDepthWarning { get; init; } = 8;

    /// <summary>Aktiviert die heuristische Warnung für alleinstehende Ersatztitel (PossibleEmbeddedHeading).</summary>
    public bool PossibleEmbeddedHeadingWarning { get; init; } = true;
}
