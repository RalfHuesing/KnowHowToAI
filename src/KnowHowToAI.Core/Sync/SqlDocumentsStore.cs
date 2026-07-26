using System.Diagnostics;
using System.Text.Json;
using Dapper;
using KnowHowToAI.Core.Documents;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace KnowHowToAI.Core.Sync;

// Einziger Ort mit echtem SQL-Zugriff für den Doku-Loop. ImportService/ExportService kennen
// nur die Methoden-Delegates (ReplaceAllAsync/GetAllAsync), nicht diese Klasse selbst — siehe
// docs/04-Datenmodell-Validierung-Edgecases.md, Abschnitt 4.3 und .agents/rules/01-code-style.mdc.
public sealed class SqlDocumentsStore
{
    private readonly string _connectionString;
    private readonly string _table;
    private readonly ILogger<SqlDocumentsStore> _logger;

    public SqlDocumentsStore(string connectionString, string documentsTableName, ILogger<SqlDocumentsStore> logger)
    {
        SqlIdentifierValidator.EnsureValid(documentsTableName);
        _connectionString = connectionString;
        _table = $"dbo.{documentsTableName}";
        _logger = logger;
    }

    public async Task ReplaceAllAsync(IReadOnlyList<Document> documents, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ReplaceAll startet: {DocumentCount} Dokumente in Tabelle {Table}",
            documents.Count, _table);
        var sw = Stopwatch.StartNew();
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
        _logger.LogInformation(
            "ReplaceAll abgeschlossen: {DocumentCount} Dokumente in {ElapsedMs}ms",
            documents.Count, sw.ElapsedMilliseconds);
    }

    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAll startet");
        var sw = Stopwatch.StartNew();
        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<DocumentRow>(new CommandDefinition(
            $"SELECT slug AS Slug, title AS Title, content AS Content, tags AS Tags, synonyms AS Synonyms FROM {_table};",
            cancellationToken: cancellationToken));

        var result = rows.Select(ToDocument).ToList();
        _logger.LogInformation(
            "GetAll abgeschlossen: {DocumentCount} Dokumente in {ElapsedMs}ms",
            result.Count, sw.ElapsedMilliseconds);
        return result;
    }

    public async Task<IReadOnlyList<DocumentSummary>> ListChildrenAsync(string? parentSlug, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ListChildren(parentSlug={ParentSlug})", parentSlug);
        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<DocumentSummary>(new CommandDefinition(
            $"""
            SELECT slug AS Slug, title AS Title FROM {_table}
            WHERE (@ParentSlug IS NULL AND parent_slug IS NULL) OR parent_slug = @ParentSlug
            ORDER BY slug;
            """,
            new { ParentSlug = parentSlug },
            cancellationToken: cancellationToken));

        var result = rows.ToList();
        _logger.LogInformation("ListChildren abgeschlossen: {ResultCount} Kinder", result.Count);
        return result;
    }

    public async Task<SearchResult> SearchDocsAsync(string query, int maxQueryLength, int maxResults, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "SearchDocs(query='{Query}', maxQueryLength={MaxQueryLength}, maxResults={MaxResults})",
            query, maxQueryLength, maxResults);
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
        var searchResult = new SearchResult(results, Truncated: totalCount > results.Count);
        _logger.LogInformation(
            "SearchDocs abgeschlossen: {ResultCount} Treffer, truncated={Truncated}",
            searchResult.Results.Count, searchResult.Truncated);
        return searchResult;
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
        _logger.LogInformation("GetDoc(slug='{Slug}')", slug);
        await using var connection = new SqlConnection(_connectionString);
        var result = await connection.QuerySingleOrDefaultAsync<DocumentDetail>(new CommandDefinition(
            $"SELECT title AS Title, content AS Content FROM {_table} WHERE slug = @Slug;",
            new { Slug = slug },
            cancellationToken: cancellationToken));
        _logger.LogInformation(
            "GetDoc abgeschlossen: {ResultState}",
            result is null ? "null" : $"content length={result.Content?.Length ?? 0}");
        return result;
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
