namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Liefert die im Kontextselektor auswählbaren Releases und offenen Transaktionen
/// als UI-Daten, ohne Application-Typen in die Formular-Komponente zu tragen.
/// </summary>
public interface IContextSelectionCatalog
{
    Task<ContextSelectionOptionsViewModel> LoadAsync(CancellationToken cancellationToken = default);
}
