namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Art des Lese-Kontexts, aus dem eine Fachseite den dargestellten
/// Wissensstand bezieht.
/// </summary>
public enum KnowledgeReadContextKind
{
    Current,
    Snapshot,
    Transaction,
    Release
}
