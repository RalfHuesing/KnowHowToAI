namespace KnowHowToAI.Core.Domain.Common;

/// <summary>
/// Kennzeichnet versionierte Fachobjekte, die per Tombstone aus aktiven Reads ausgeblendet werden können.
/// </summary>
public interface ITombstoned
{
    bool IsDeleted { get; }
}
