namespace KnowHowToAI.Server.Web.Endpoints;

/// <summary>HTTP-Querywerte für einen Markdown-Teilbaumdownload.</summary>
internal sealed class MarkdownDownloadRequest
{
    public string? NodeId { get; init; }
    public string? AudienceId { get; init; }
    public string? TransactionId { get; init; }
    public string? SnapshotId { get; init; }
    public string? ReleaseId { get; init; }
}
