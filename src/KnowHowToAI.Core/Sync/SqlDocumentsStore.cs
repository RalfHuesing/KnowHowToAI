using System.Text.Json;
using Dapper;
using KnowHowToAI.Core.Documents;
using Microsoft.Data.SqlClient;

namespace KnowHowToAI.Core.Sync;

// Einziger Ort mit echtem SQL-Zugriff für den Doku-Loop. ImportService/ExportService kennen
// nur die Methoden-Delegates (ReplaceAllAsync/GetAllAsync), nicht diese Klasse selbst — siehe
// docs/04-Datenmodell-Validierung-Edgecases.md, Abschnitt 4.3 und .agents/rules/01-code-style.mdc.
public sealed class SqlDocumentsStore
{
    private readonly string _connectionString;
    private readonly string _table;

    public SqlDocumentsStore(string connectionString, string documentsTableName)
    {
        SqlIdentifierValidator.EnsureValid(documentsTableName);
        _connectionString = connectionString;
        _table = $"dbo.{documentsTableName}";
    }

    public async Task ReplaceAllAsync(IReadOnlyList<Document> documents, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            $"DELETE FROM {_table};", transaction: transaction, cancellationToken: cancellationToken));

        foreach (var document in documents.OrderBy(document => document.Slug.Count(c => c == '/')))
        {
            await connection.ExecuteAsync(new CommandDefinition(
                $"""
                INSERT INTO {_table} (slug, parent_slug, title, content, tags, synonyms)
                VALUES (@Slug, @ParentSlug, @Title, @Content, @Tags, @Synonyms);
                """,
                new
                {
                    document.Slug,
                    document.ParentSlug,
                    document.Title,
                    document.Content,
                    Tags = JsonSerializer.Serialize(document.Tags),
                    Synonyms = JsonSerializer.Serialize(document.Synonyms),
                },
                transaction,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<DocumentRow>(new CommandDefinition(
            $"SELECT slug AS Slug, title AS Title, content AS Content, tags AS Tags, synonyms AS Synonyms FROM {_table};",
            cancellationToken: cancellationToken));

        return [.. rows.Select(ToDocument)];
    }

    public async Task<IReadOnlyList<DocumentSummary>> ListChildrenAsync(string? parentSlug, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<DocumentSummary>(new CommandDefinition(
            $"""
            SELECT slug AS Slug, title AS Title FROM {_table}
            WHERE (@ParentSlug IS NULL AND parent_slug IS NULL) OR parent_slug = @ParentSlug;
            """,
            new { ParentSlug = parentSlug },
            cancellationToken: cancellationToken));

        return [.. rows];
    }

    public async Task<SearchResult> SearchDocsAsync(string query, int maxQueryLength, int maxResults, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query)) return new SearchResult([], Truncated: false);
        if (query.Length > maxQueryLength)
        {
            throw new ArgumentException(
                $"search_docs query ist {query.Length} Zeichen lang, max {maxQueryLength}.",
                nameof(query));
        }

        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<SearchRow>(new CommandDefinition(
            $"""
            SELECT TOP (@MaxResults) slug AS Slug, title AS Title,
                   COUNT(*) OVER() AS TotalCount
            FROM {_table}
            WHERE title LIKE @Pattern OR content LIKE @Pattern OR tags LIKE @Pattern OR synonyms LIKE @Pattern
            ORDER BY
                CASE WHEN title LIKE @Pattern THEN 0 ELSE 1 END,
                title;
            """,
            new { Pattern = BuildLikePattern(query), MaxResults = maxResults },
            cancellationToken: cancellationToken));

        var rowList = rows.AsList();
        var results = rowList.Select(r => new DocumentSummary(r.Slug, r.Title)).ToList();
        var totalCount = rowList.Count > 0 ? rowList[0].TotalCount : 0;
        return new SearchResult(results, Truncated: totalCount > results.Count);
    }

    internal static string BuildLikePattern(string query)
    {
        var escaped = query
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
        return $"%{escaped}%";
    }

    public async Task<DocumentDetail?> GetDocAsync(string slug, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<DocumentDetail>(new CommandDefinition(
            $"SELECT title AS Title, content AS Content FROM {_table} WHERE slug = @Slug;",
            new { Slug = slug },
            cancellationToken: cancellationToken));
    }

    private static Document ToDocument(DocumentRow row) => new(
        row.Slug,
        row.Title,
        row.Content,
        SlugRules.GetParentSlug(row.Slug),
        row.Tags is null ? [] : JsonSerializer.Deserialize<List<string>>(row.Tags)!,
        row.Synonyms is null ? [] : JsonSerializer.Deserialize<List<string>>(row.Synonyms)!);

    private sealed record DocumentRow(string Slug, string Title, string Content, string? Tags, string? Synonyms);
    private sealed record SearchRow(string Slug, string Title, int TotalCount);
}
