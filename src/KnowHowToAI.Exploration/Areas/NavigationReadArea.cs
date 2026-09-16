using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Server.Mcp.Tools.Navigation;

namespace KnowHowToAI.Exploration.Areas;

/// <summary>
/// Beispiel-Bereich: Read-Round-Trips über die originalen Navigation-Handler
/// ohne stdio-Transport. Deckt Normalfall, Paging mit Cursor und Fehlerbilder ab.
/// </summary>
public sealed class NavigationReadArea : IExplorationArea
{
    public string Id => "navigation-read";

    public string Description =>
        "Read-Round-Trips über die originalen MCP-Handler (list_roles, get_root, list_children, get_node) ohne stdio-Transport.";

    public IReadOnlyList<ExplorationScenario> CreateScenarios(ExplorationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return
        [
            new("roles-and-root", "list_roles, dann get_root mit der ersten Rolle", async cancellationToken =>
            {
                var tools = context.Tools<NavigationTools>();
                var roles = await tools.ListRoles(cancellationToken: cancellationToken).ConfigureAwait(false);
                context.ReportEnvelope("list_roles", roles);

                var roleId = roles.Data?.Items is { Count: > 0 } items ? items[0].RoleId : null;
                if (roleId is null)
                {
                    context.Info(
                        "Keine aktiven Rollen vorhanden; get_root wird mit einer erfundenen roleId aufgerufen, um das Fehlerbild zu dokumentieren.");
                    roleId = "exploration-unknown-role";
                }

                var root = await tools.GetRoot(roleId, cancellationToken: cancellationToken).ConfigureAwait(false);
                context.ReportEnvelope("get_root", root);
                if (!root.IsSuccess)
                    context.Info($"get_root endet mit code={root.Code} und message='{root.Message}'.");
            }),

            new("children-paging", "get_root → list_children (limit 2) → Cursor-Fortsetzung", async cancellationToken =>
            {
                var tools = context.Tools<NavigationTools>();
                var roles = await tools.ListRoles(cancellationToken: cancellationToken).ConfigureAwait(false);
                var roleId = roles.Data?.Items is { Count: > 0 } items ? items[0].RoleId : "exploration-unknown-role";
                var root = await tools.GetRoot(roleId, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (root.Data is not { } rootNode)
                {
                    context.Info("Kein Root vorhanden – Paging ohne Datenlage nicht prüfbar.");
                    return;
                }

                var page = await tools
                    .ListChildren(roleId, parentNodeId: rootNode.NodeId, limit: 2, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                context.ReportEnvelope("list_children", page);

                if (page.Data?.NextCursor is { } cursor)
                {
                    var nextPage = await tools
                        .ListChildren(roleId, parentNodeId: rootNode.NodeId, limit: 2, cursor: cursor,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    context.ReportEnvelope("list_children (Cursor-Fortsetzung)", nextPage);
                }
                else
                {
                    context.Info("Kein nextCursor in der Antwort – Cursor-Fortsetzung nicht prüfbar (weniger Kinder als limit).");
                }
            }),

            new("invalid-inputs", "get_node mit ungültiger nodeId – Fehler-Envelope-Form", async cancellationToken =>
            {
                var tools = context.Tools<NavigationTools>();
                var result = await tools
                    .GetNode("not-a-guid", "exploration-unknown-role", cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                context.ReportEnvelope("get_node (ungültige nodeId)", result);

                if (result.Code != NavigationErrorCodes.InvalidNodeId)
                    context.Bug($"Ungültige nodeId wird mit code={result.Code} statt InvalidNodeId quittiert.");
                else
                    context.Info("Ungültige nodeId wird als Parameterfehler InvalidNodeId mit Parametername in details abgelehnt.");
            })
        ];
    }
}
