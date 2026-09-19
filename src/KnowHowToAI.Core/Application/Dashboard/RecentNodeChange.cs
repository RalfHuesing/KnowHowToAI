using KnowHowToAI.Core.Application.History;
using KnowHowToAI.Core.Domain.Common;

namespace KnowHowToAI.Core.Application.Dashboard;

/// <summary>
/// Zuletzt geänderter Node aus dem Diff zwischen Current Snapshot und seinem direkten committed Vorgänger.
/// </summary>
public sealed record RecentNodeChange(
    NodeId NodeId,
    string Title,
    DiffChangeKind Kind);
