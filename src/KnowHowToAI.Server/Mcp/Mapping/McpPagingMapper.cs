using KnowHowToAI.Core.Application.Policies;

namespace KnowHowToAI.Server.Mcp.Mapping;

/// <summary>
/// Bildet die Paging-Parameter der Listen-, Search- und Diff-Tools ab.
/// Der <c>limit</c>-Wert wird gemäß <see cref="RetrievalPolicy"/> normalisiert:
/// <c>null</c> oder ≤ 0 ergibt <see cref="RetrievalPolicy.DefaultPageSize"/>, Werte oberhalb
/// des Maximums werden auf <see cref="RetrievalPolicy.MaximumPageSize"/> geklemmt.
/// Cursor-Strings werden opak ohne Interpretation weitergereicht.
/// </summary>
internal static class McpPagingMapper
{
    public static int NormalizeLimit(int? limit, int defaultPageSize, int maximumPageSize) =>
        limit is > 0 ? Math.Min(limit.Value, maximumPageSize) : defaultPageSize;
}
