using KnowHowToAI.Core.Application.Abstractions.Runtime;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// Deterministischer <see cref="IIdentifierGenerator"/> für Tests: liefert pro
/// Identifier-Typ den konfigurierten Festwert; nicht konfigurierte Typen werfen eine
/// <see cref="NotSupportedException"/>, damit Tests den erwarteten Aufrufpfad absichern.
/// </summary>
public sealed class FixedIdentifierGenerator : IIdentifierGenerator
{
    /// <summary>Festwert für neu erzeugte Transaction-IDs.</summary>
    public TransactionId? FixedTransactionId { get; init; }

    /// <summary>Festwert für neu erzeugte Node-IDs.</summary>
    public NodeId? FixedNodeId { get; init; }

    /// <summary>Festwert für neu erzeugte Content-Revision-IDs.</summary>
    public ContentRevisionId? FixedContentRevisionId { get; init; }

    public TransactionId CreateTransactionId() =>
        FixedTransactionId ?? throw new NotSupportedException("CreateTransactionId ist nicht konfiguriert.");

    public NodeId CreateNodeId() =>
        FixedNodeId ?? throw new NotSupportedException("CreateNodeId ist nicht konfiguriert.");

    public ContentRevisionId CreateContentRevisionId() =>
        FixedContentRevisionId ?? throw new NotSupportedException(
            "CreateContentRevisionId ist nicht konfiguriert.");
}
