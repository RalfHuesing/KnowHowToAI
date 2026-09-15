namespace KnowHowToAI.Core.Domain.Content;

/// <summary>
/// Normalisiert gespeicherten Markdown auf die kanonischen LF-Zeilenenden.
/// </summary>
public static class ContentNormalizer
{
    public static string Normalize(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return content.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }
}
