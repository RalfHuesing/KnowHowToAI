namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Immutable Vertragsschicht für die globale Wissenskontextleiste: Art des
/// Lese-Kontexts, optionale ID/Bezeichnung des angesprochenen Standes,
/// optional ausgewählte Rolle und Änderungszustand. Fachseiten liefern genau
/// diesen Vertrag über <see cref="PageRegionState"/>; Domain-Typen und
/// Serverzustand erscheinen bewusst nicht im Markup. Für nicht vorhandene
/// Angaben bleiben die Felder leer – die Leiste erfindet keine Dummy-IDs.
/// </summary>
/// <param name="ReadContext">Art des Lese-Kontexts der Seite.</param>
/// <param name="ContextId">Optionale stabile ID des Standes, falls die Seite einen besitzt.</param>
/// <param name="DisplayName">Optionale Bezeichnung des Standes für die Anzeige.</param>
/// <param name="RoleName">Optional ausgewählte Rolle; ohne Auswahl bleibt der Platz neutral.</param>
/// <param name="IsDirty">Gibt an, ob die Seite ungespeicherte Änderungen hält.</param>
/// <param name="BaseSnapshotId">Optionale ID des Basis-Snapshots bei Working Transactions.</param>
public sealed record KnowledgeContextViewModel(
    KnowledgeReadContextKind ReadContext,
    string? ContextId = null,
    string? DisplayName = null,
    string? RoleName = null,
    bool IsDirty = false,
    long? BaseSnapshotId = null);
