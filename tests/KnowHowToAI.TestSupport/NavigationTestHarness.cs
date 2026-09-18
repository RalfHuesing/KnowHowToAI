using KnowHowToAI.Core.Application.Abstractions.Persistence;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Application.Retrieval.Export;
using KnowHowToAI.Core.Application.Retrieval.Search;
using KnowHowToAI.Core.Application.Transactions;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Dependencies;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Core.Domain.Versioning;

namespace KnowHowToAI.TestSupport;

/// <summary>
/// Feature-spezifischer Harness für Navigation-, Export- und Search-Szenarien auf einem
/// gemeinsamen <see cref="InMemoryKnowledgeStore"/>: kapselt den Szenario-Aufbau
/// (Snapshots, Transactions, Nodes, Rollen, Contents) und erzeugt die Services über die
/// gemeinsamen In-Memory-Port-Fakes. Wird von Unit- und Integrationstests geteilt.
/// </summary>
public sealed class NavigationTestHarness
{
    private static readonly DateTimeOffset FixedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly RoleId DefaultRoleId = new("Developer");

    private readonly InMemoryKnowledgeStore _store = new();

    /// <summary>Erzeugt einen Harness mit einem Committed Current Snapshot.</summary>
    public NavigationTestHarness(SnapshotId currentSnapshotId)
    {
        _store.CurrentSnapshotId = currentSnapshotId;
        _store.Snapshots.Add(new Snapshot(currentSnapshotId, null, SnapshotState.Committed, FixedTimestamp, FixedTimestamp));
        EnsureDefaultRole(currentSnapshotId);
    }

    /// <summary>Setzt den Current Snapshot neu und legt ihn mit Standard-Rolle an.</summary>
    public void SetCurrentSnapshot(SnapshotId snapshotId)
    {
        _store.CurrentSnapshotId = snapshotId;
        _store.Snapshots.Add(new Snapshot(snapshotId, null, SnapshotState.Committed, FixedTimestamp, FixedTimestamp));
        EnsureDefaultRole(snapshotId);
    }

    /// <summary>Nimmt einen historischen Snapshot in den Store auf.</summary>
    public void AddHistoricalSnapshot(Snapshot snapshot)
    {
        _store.Snapshots.Add(snapshot);
        EnsureDefaultRole(snapshot.SnapshotId);
    }

    /// <summary>Registriert eine Transaction und legt ihren Working Snapshot mit Standard-Rolle an.</summary>
    public void SetTransaction(KnowledgeTransaction transaction)
    {
        _store.Transactions[transaction.TransactionId] = transaction;
        _store.Snapshots.RemoveAll(s => s.SnapshotId == transaction.WorkingSnapshotId);
        _store.Snapshots.Add(new Snapshot(transaction.WorkingSnapshotId, transaction.BaseSnapshotId, SnapshotState.Working, transaction.CreatedAtUtc, null));
        EnsureDefaultRole(transaction.WorkingSnapshotId);
    }

    /// <summary>Fügt einen Node hinzu.</summary>
    public void AddNode(Node node) => _store.Nodes.Add(node);

    /// <summary>Fügt eine Rolle hinzu und ersetzt eine bestehende Rolle derselben Kennung.</summary>
    public void AddRole(Role role)
    {
        _store.Roles.RemoveAll(r => r.SnapshotId == role.SnapshotId && r.RoleId == role.RoleId);
        _store.Roles.Add(role);
    }

    /// <summary>Fügt eine Auflösungsreihenfolge hinzu und ersetzt eine bestehende gleicher Konfiguration.</summary>
    public void AddRoleResolution(RoleResolution resolution)
    {
        _store.Resolutions.RemoveAll(r => r.SnapshotId == resolution.SnapshotId
            && r.RequestedRoleId == resolution.RequestedRoleId
            && r.CandidateRoleId == resolution.CandidateRoleId);
        _store.Resolutions.Add(resolution);
    }

    /// <summary>Entfernt alle Rollen und Auflösungsreihenfolgen eines Snapshots.</summary>
    public void ClearRoles(SnapshotId snapshotId)
    {
        _store.Roles.RemoveAll(r => r.SnapshotId == snapshotId);
        _store.Resolutions.RemoveAll(r => r.SnapshotId == snapshotId);
    }

    /// <summary>Fügt einen Rollen-Content hinzu.</summary>
    public void AddContent(NodeContent content) => _store.Contents.Add(content);

    /// <summary>Fügt eine Content-Dependency hinzu.</summary>
    public void AddDependency(ContentDependency dependency) => _store.Dependencies.Add(dependency);

    /// <summary>Baut die Snapshot-Read-Repositories über die gemeinsamen In-Memory-Fakes.</summary>
    public SnapshotReadRepositories CreateRepositories() => new(
        new InMemorySnapshotRepository(_store),
        new InMemoryTransactionRepository(_store),
        new InMemoryHierarchyRepository(_store),
        new InMemoryContentRepository(_store),
        new InMemoryRoleRepository(_store),
        new InMemoryDependencyRepository(_store),
        new InMemoryWorkingSnapshotReadRepository(_store));

    /// <summary>Erzeugt den NavigationService mit konfigurierbarer Seitengröße.</summary>
    public NavigationService CreateService(int defaultPageSize = 10, int maximumPageSize = 100) => new(
        CreateRepositories(),
        CreatePolicy(
            defaultPageSize,
            maximumPageSize,
            searchPageSize: 10,
            searchMaximumPageSize: 100,
            snippetMaximumCharacters: 100));

    /// <summary>Erzeugt einen SearchService über den Store und das übergebene Retrieval-Repository.</summary>
    public SearchService CreateSearchService(
        IRetrievalRepository retrievalRepository,
        int searchPageSize = 10,
        int searchMaximumPageSize = 100) => new(
        new SearchRepositories(
            CreateRepositories().Snapshots,
            CreateRepositories().Transactions,
            retrievalRepository),
        CreatePolicy(
            defaultPageSize: 10,
            maximumPageSize: 100,
            searchPageSize,
            searchMaximumPageSize,
            snippetMaximumCharacters: 100));

    /// <summary>Erzeugt den MarkdownExportService über den Store.</summary>
    public MarkdownExportService CreateExportService() => new(CreateRepositories());

    /// <summary>Stellt sicher, dass jeder Snapshot die Default-Rolle mit Selbst-Auflösung trägt.</summary>
    private void EnsureDefaultRole(SnapshotId snapshotId)
    {
        if (!_store.Roles.Any(r => r.SnapshotId == snapshotId && r.RoleId == DefaultRoleId))
        {
            _store.Roles.Add(new Role(snapshotId, DefaultRoleId, "Developer", null, false));
        }

        if (!_store.Resolutions.Any(r => r.SnapshotId == snapshotId
            && r.RequestedRoleId == DefaultRoleId && r.CandidateRoleId == DefaultRoleId))
        {
            _store.Resolutions.Add(new RoleResolution(snapshotId, DefaultRoleId, DefaultRoleId, 1));
        }
    }

    private static RetrievalPolicy CreatePolicy(
        int defaultPageSize,
        int maximumPageSize,
        int searchPageSize,
        int searchMaximumPageSize,
        int snippetMaximumCharacters) => new()
    {
        DefaultPageSize = defaultPageSize,
        MaximumPageSize = maximumPageSize,
        SearchPageSize = searchPageSize,
        SearchMaximumPageSize = searchMaximumPageSize,
        SnippetMaximumCharacters = snippetMaximumCharacters
    };
}
