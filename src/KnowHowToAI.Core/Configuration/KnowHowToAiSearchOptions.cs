namespace KnowHowToAI.Core.Configuration;

// Bindung an den "KnowHowToAi:Search"-Abschnitt in appsettings.json.
public sealed record KnowHowToAiSearchOptions
{
    public int MaxQueryLength { get; init; } = 200;
    public int MaxResults { get; init; } = 50;
}
