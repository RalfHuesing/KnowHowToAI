using System.Text;

namespace KnowHowToAI.Core.Application.Retrieval.Search;

/// <summary>
/// Sichere Escape-Logik für SQL-LIKE-Sonderzeichen (%, _, [, \).
/// Verhindert Wildcard-Injection und unerwartetes Pattern-Matching.
/// </summary>
public static class LikeEscaping
{
    /// <summary>
    /// Maskiert die Sonderzeichen %, _, [ und \ für die Verwendung in LIKE ... ESCAPE '\'.
    /// </summary>
    public static string Escape(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length + 8);
        foreach (var ch in text)
        {
            if (ch is '%' or '_' or '[' or '\\')
            {
                sb.Append('\\');
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }
}
