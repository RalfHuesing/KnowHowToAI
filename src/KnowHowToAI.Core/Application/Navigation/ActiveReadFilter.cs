using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>
/// Wendet die Tombstone-Policy des Read-Kontexts auf versionierte Fachobjekte an.
/// </summary>
public static class ActiveReadFilter
{
    public static IReadOnlyList<T> Apply<T>(IEnumerable<T> entities, ResolvedReadContext context)
        where T : ITombstoned
    {
        ArgumentNullException.ThrowIfNull(entities);
        ArgumentNullException.ThrowIfNull(context);

        return Array.AsReadOnly(
            (context.IncludeDeleted ? entities : entities.Where(entity => !entity.IsDeleted)).ToArray());
    }
}
