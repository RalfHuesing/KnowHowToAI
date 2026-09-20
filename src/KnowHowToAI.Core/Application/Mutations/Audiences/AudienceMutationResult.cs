using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;

namespace KnowHowToAI.Core.Application.Mutations.Audiences;

/// <summary>Ergebnis einer Rollenmutation mit dem danach gültigen Working-Stand.</summary>
public sealed record AudienceMutationResult(Audience Audience, SnapshotId SnapshotId, long ChangeVersion)
{
    public AudienceId AudienceId => Audience.AudienceId;

    public string Name => Audience.Name;

    public string? Description => Audience.Description;

    public bool IsDeleted => Audience.IsDeleted;
}

/// <summary>Transportneutrale Eingabe für eine Rollenänderung.</summary>
public sealed record UpdateAudienceMutationRequest(
    AudienceId AudienceId,
    string Name,
    string? Description,
    long ExpectedChangeVersion);

/// <summary>Ergebnis einer Resolution-Order-Mutation mit dem danach gültigen Working-Stand.</summary>
public sealed record AudienceResolutionMutationResult(
    AudienceId RequestedAudienceId,
    IReadOnlyList<AudienceResolution> Resolutions,
    SnapshotId SnapshotId,
    long ChangeVersion);
