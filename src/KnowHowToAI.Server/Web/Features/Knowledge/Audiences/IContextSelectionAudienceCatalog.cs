using KnowHowToAI.Core.Application.Navigation;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Audiences;

/// <summary>
/// Lädt die im gewählten Lesekontext verfügbaren Zielgruppen für den Selektor.
/// </summary>
public interface IContextSelectionAudienceCatalog
{
    Task<ContextSelectionAudienceLoadResult> LoadAsync(
        ReadContext readContext,
        CancellationToken cancellationToken = default);
}
