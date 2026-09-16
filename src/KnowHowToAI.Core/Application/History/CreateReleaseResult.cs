using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.Core.Application.History;

/// <summary>
/// Ergebnis einer Release-Erstellung inklusive transparenter Qualitätsbefunde des Snapshots.
/// </summary>
public sealed record CreateReleaseResult(
    Release Release,
    IReadOnlyList<DomainWarning> Findings);
