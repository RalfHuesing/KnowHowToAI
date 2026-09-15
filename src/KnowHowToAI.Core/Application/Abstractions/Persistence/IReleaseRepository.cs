using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.Abstractions.Persistence;

/// <summary>Read-Port für unveränderliche Release-Metadaten.</summary>
public interface IReleaseRepository
{
    Task<Release?> FindAsync(ReleaseId releaseId, CancellationToken cancellationToken = default);
}
