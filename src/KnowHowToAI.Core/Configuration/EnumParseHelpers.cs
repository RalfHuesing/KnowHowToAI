namespace KnowHowToAI.Core.Configuration;

internal static class EnumParseHelpers
{
    public static TEnum Parse<TEnum>(string value) where TEnum : struct, Enum
        => Enum.TryParse<TEnum>(value, ignoreCase: true, out var result)
            ? result
            : throw new InvalidOperationException(
                $"Ungültiger Wert '{value}' für {typeof(TEnum).Name}. " +
                $"Erlaubt: {string.Join(", ", Enum.GetNames<TEnum>())}.");
}
