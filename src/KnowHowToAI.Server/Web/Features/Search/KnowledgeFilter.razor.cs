using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Search;

/// <summary>Erfasst den unpersistierten Facettenzustand einer Trefferliste.</summary>
public sealed partial class KnowledgeFilter
{
    private const string Explicit = "Explicit";
    private const string Fallback = "Fallback";
    private const string None = "None";
    private const string Current = "Current";
    private const string Stale = "Stale";
    private const string Unknown = "Unknown";
    private const string StaleDerivedContent = "StaleDerivedContent";

    [Parameter]
    public SearchFilterViewModel Filter { get; set; } = SearchFilterViewModel.Empty;

    [Parameter]
    public IReadOnlyList<SearchFilterOptionViewModel> Audiences { get; set; } = [];

    [Parameter]
    public bool IsSearching { get; set; }

    [Parameter]
    public EventCallback<SearchFilterViewModel> OnChanged { get; set; }

    private Task ToggleAudienceAsync(string value) =>
        UpdateAsync(Filter with { ResolvedAudienceIds = Toggle(Filter.ResolvedAudienceIds, value) });

    private Task ToggleAvailabilityAsync(string value) =>
        UpdateAsync(Filter with { Availabilities = Toggle(Filter.Availabilities, value) });

    private Task ToggleFreshnessAsync(string value) =>
        UpdateAsync(Filter with { Freshnesses = Toggle(Filter.Freshnesses, value) });

    private Task ToggleFindingAsync(string value) =>
        UpdateAsync(Filter with { FindingCodes = Toggle(Filter.FindingCodes, value) });

    private Task UpdateAsync(SearchFilterViewModel filter) => OnChanged.InvokeAsync(filter);

    private static IReadOnlyList<string> Toggle(IReadOnlyList<string> values, string value) => values.Contains(value)
        ? values.Where(candidate => !string.Equals(candidate, value, StringComparison.Ordinal)).ToArray()
        : values.Append(value).Distinct(StringComparer.Ordinal).ToArray();
}
