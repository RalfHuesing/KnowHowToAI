using KnowHowToAI.Core.Application.Navigation;

namespace KnowHowToAI.Server.Web.Components.Layout.Context;

/// <summary>
/// Lädt die im gewählten Lesekontext verfügbaren Rollen für den Selektor.
/// </summary>
public interface IContextSelectionRoleCatalog
{
    Task<ContextSelectionRoleLoadResult> LoadAsync(
        ReadContext readContext,
        CancellationToken cancellationToken = default);
}
