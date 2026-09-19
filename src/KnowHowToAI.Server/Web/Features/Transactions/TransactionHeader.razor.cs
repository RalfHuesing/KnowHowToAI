using KnowHowToAI.Core.Domain.Versioning;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Transactions;

public sealed partial class TransactionHeader : ComponentBase
{
    [Parameter, EditorRequired]
    public KnowledgeTransaction Transaction { get; set; } = default!;

    [Parameter]
    public bool IsOlderThan7Days { get; set; }
}
