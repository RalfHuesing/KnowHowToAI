using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Mcp.Contracts;

namespace KnowHowToAI.Server.Mcp.Mapping;

/// <summary>
/// Bildet den transportneutralen Application-/Domain-Result-Vertrag auf den
/// gemeinsamen MCP-Antwort-Envelope ab. Enthält keine Fachlogik.
/// </summary>
internal static class McpResultMapper
{
    /// <summary>Mappt ein <see cref="Result{T}"/> inklusive Fehler, Details und Warnungen auf den Envelope.</summary>
    public static McpToolEnvelope<TData> ToEnvelope<TData>(Result<TData> result) where TData : class
    {
        ArgumentNullException.ThrowIfNull(result);
        var warnings = result.Warnings.Select(ToWarning).ToArray();
        return result.IsSuccess
            ? McpToolEnvelope<TData>.Success(result.Value, warnings)
            : McpToolEnvelope<TData>.Failure(result.Error!, warnings);
    }

    /// <summary>Mappt einen <see cref="DomainWarning"/> auf den MCP-Warncode-Vertrag.</summary>
    public static McpWarning ToWarning(DomainWarning warning) =>
        new(warning.Code, warning.Message, warning.Details);
}
