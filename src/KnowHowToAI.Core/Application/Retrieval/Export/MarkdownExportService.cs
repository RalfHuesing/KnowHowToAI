using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Core.Application.Retrieval.Export;

/// <summary>
/// Transportneutraler Export-Use-Case (export_tree): wandelt einen Teilbaum in Markdown um.
/// Überschriften entstehen ausschließlich aus der Node-Hierarchie; ContentMd darf keine enthalten.
/// Die eigentliche Traversal- und Render-Logik wird in M5.2 implementiert.
/// </summary>
public sealed class MarkdownExportService
{
    private readonly HistoryRepositories _repos;

    public MarkdownExportService(HistoryRepositories repositories)
    {
        _repos = repositories ?? throw new ArgumentNullException(nameof(repositories));
    }

    /// <summary>
    /// Exportiert den Teilbaum ab <paramref name="rootNodeId"/> als Markdown.
    /// Der ausgewählte Root-Node wird zu H1, Nachfahren erhalten relative Heading-Level.
    /// </summary>
    public Task<Result<string>> ExportTreeAsync(
        NodeId rootNodeId,
        ReadContext context,
        RoleId roleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Implementierung folgt in M5.2
        throw new NotImplementedException("ExportTreeAsync wird in M5.2 implementiert.");
    }
}
