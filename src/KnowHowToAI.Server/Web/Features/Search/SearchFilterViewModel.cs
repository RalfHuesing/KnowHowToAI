namespace KnowHowToAI.Server.Web.Features.Search;

/// <summary>Darstellungsdaten der ausschließlich featurelokalen Suchfacetten.</summary>
public sealed record SearchFilterViewModel(
    IReadOnlyList<string> ResolvedRoleIds,
    IReadOnlyList<string> Availabilities,
    IReadOnlyList<string> Freshnesses,
    IReadOnlyList<string> FindingCodes)
{
    public static readonly SearchFilterViewModel Empty = new([], [], [], []);

    public bool IsEmpty => ResolvedRoleIds.Count == 0
        && Availabilities.Count == 0
        && Freshnesses.Count == 0
        && FindingCodes.Count == 0;
}

/// <summary>Beschriftete Auswahl einer rollenbezogenen Filterfacette.</summary>
public sealed record SearchFilterOptionViewModel(string Value, string Label);
