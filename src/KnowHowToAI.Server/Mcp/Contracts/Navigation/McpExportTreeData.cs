using System.Text.Json.Serialization;

namespace KnowHowToAI.Server.Mcp.Contracts.Navigation;

/// <summary>
/// export_tree-Ergebnis im MCP-Vertrag: der Teilbaum als Markdown. Überschriften
/// entstehen ausschließlich aus der Node-Hierarchie; der ausgewählte Root ist H1.
/// </summary>
public sealed record McpExportTreeData(
    [property: JsonPropertyName("markdown")] string Markdown);
