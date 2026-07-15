namespace KnowHowToAI.Core.Configuration;

// Bindung an den "KnowHowToAi:Validation"-Abschnitt in appsettings.json.
public sealed record KnowHowToAiValidationOptions
{
    public int MaxContentLengthWarning { get; init; } = 8000;
}
