using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Dashboard;

public sealed partial class OpenTransactionList
{
    [Parameter, EditorRequired]
    public IReadOnlyList<OpenTransactionItemViewModel> Transactions { get; set; } = [];
}
