using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Versioning;
using Microsoft.AspNetCore.Components;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Node;

/// <summary>Dokumentgrenze für die Auswahl und Bearbeitung des aktiven Wissensknotens.</summary>
public sealed partial class NodeDocument
{
    [Parameter]
    public Guid? NodeId { get; set; }

    [Parameter]
    public ReadContext? ReadContext { get; set; }

    [Parameter]
    public string? AudienceId { get; set; }

    [Parameter]
    public long? ChangeVersion { get; set; }

    [Parameter]
    public TransactionId? TransactionId { get; set; }

    [Parameter]
    public EventCallback<NodeMutationResult> OnMutationSucceeded { get; set; }
}
