using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// Geteiltes In-Memory-Modell des versionierten Wissensstands: hält Snapshots,
/// Knowledge-Transactions, Hierarchie, Rollen, Contents und Dependencies als
/// veränderliche Listen, auf die alle In-Memory-Port-Fakes desselben Stores lesend
/// zugreifen. Die Filterung nach SnapshotId erfolgt in den Fakes, nicht im Store.
/// </summary>
public sealed class InMemoryKnowledgeStore
{
    /// <summary>Alle bekannten Snapshots inklusive Working- und Historien-Ständen.</summary>
    public List<Snapshot> Snapshots { get; } = [];

    /// <summary>Bekannte Knowledge-Transactions nach Transaktions-ID.</summary>
    public Dictionary<TransactionId, KnowledgeTransaction> Transactions { get; } = [];

    /// <summary>Node-Hierarchie über alle Snapshots.</summary>
    public List<Node> Nodes { get; } = [];

    /// <summary>Rollen über alle Snapshots.</summary>
    public List<Role> Roles { get; } = [];

    /// <summary>Rollen-Auflösungsreihenfolgen über alle Snapshots.</summary>
    public List<RoleResolution> Resolutions { get; } = [];

    /// <summary>Rollen-Contents über alle Snapshots.</summary>
    public List<NodeContent> Contents { get; } = [];

    /// <summary>Gespeicherte Content-Provenienz über alle Snapshots.</summary>
    public List<ContentDependency> Dependencies { get; } = [];

    /// <summary>
    /// Aktueller Snapshot für <c>GetCurrentAsync</c>. Ist die Kennung nicht gesetzt,
    /// wählen die Port-Fakes den ersten Committed-Stand des Stores.
    /// </summary>
    public SnapshotId? CurrentSnapshotId { get; set; }

    /// <summary>
    /// Erzeugt einen Store, dessen Current Snapshot als Committed-Stand zum
    /// angegebenen Zeitpunkt angelegt ist – das übliche Grund-Setup für Read-Szenarien.
    /// </summary>
    public static InMemoryKnowledgeStore WithCurrentCommittedSnapshot(
        SnapshotId snapshotId,
        DateTimeOffset timestampUtc)
    {
        var store = new InMemoryKnowledgeStore { CurrentSnapshotId = snapshotId };
        store.Snapshots.Add(new Snapshot(snapshotId, null, SnapshotState.Committed, timestampUtc, timestampUtc));
        return store;
    }
}
