using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Drafts;

public sealed partial class TransactionConflictResolution : ComponentBase
{
    [Parameter]
    public long BaseSnapshotId { get; set; }

    [Parameter]
    public long CurrentSnapshotId { get; set; }

    [Parameter]
    public bool CanStartManualReapply { get; set; }

    [Parameter]
    public bool IsStartingManualReapply { get; set; }

    [Parameter]
    public EventCallback OnStartManualReapply { get; set; }
}
