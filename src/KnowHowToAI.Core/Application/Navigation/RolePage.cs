using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.Navigation;

/// <summary>Paginiertes Ergebnis für list_roles.</summary>
public sealed record RolePage(
    IReadOnlyList<Role> Items,
    string? NextCursor,
    long? ChangeVersion = null);
