using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.State;

namespace KnowHowToAI.Server.Web.State;

/// <summary>
/// Flüchtiger Circuit-State für die Blazor-Sitzung.
/// Hält den aus Route und Query rekonstruierten aktuellen Arbeits- und Lesekontext
/// (Node, Zielgruppe, geladener Snapshot, Read Context und ChangeVersion) ohne fachliche Wahrheit.
/// </summary>
public sealed class WorkspaceState
{
    public long? CurrentChangeVersion { get; private set; }

    public Guid? CurrentNodeId { get; private set; }

    public string? CurrentAudienceId { get; private set; }

    public long? LoadedSnapshotId { get; private set; }

    public string? RequestedInitialView { get; set; }

    public KnowledgeContextViewModel CurrentContext { get; private set; } =
        new(KnowledgeReadContextKind.Current);

    public ReadContext CurrentReadContext { get; private set; } = new();

    public TransactionId? ActiveTransactionId => CurrentReadContext.TransactionId;

    public event Action? Changed;

    public void SetNode(Guid? nodeId)
    {
        if (CurrentNodeId == nodeId)
            return;

        CurrentNodeId = nodeId;
        Changed?.Invoke();
    }

    public void SetAudience(string? audienceId)
    {
        if (string.Equals(CurrentAudienceId, audienceId, StringComparison.Ordinal))
            return;

        CurrentAudienceId = audienceId;
        Changed?.Invoke();
    }

    public void SetChangeVersion(long? changeVersion)
    {
        if (CurrentChangeVersion == changeVersion)
            return;

        CurrentChangeVersion = changeVersion;
        if (CurrentReadContext.TransactionId is not null)
            CurrentContext = CurrentContext with { ChangeVersion = CurrentChangeVersion };
        Changed?.Invoke();
    }

    public void SetLoadedSnapshotId(long? snapshotId)
    {
        if (LoadedSnapshotId == snapshotId)
            return;

        LoadedSnapshotId = snapshotId;
        Changed?.Invoke();
    }

    public void SetContext(KnowledgeContextViewModel context, ReadContext readContext)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(readContext);

        if (CurrentContext == context && CurrentReadContext == readContext)
            return;

        CurrentContext = context;
        CurrentReadContext = readContext;
        CurrentChangeVersion = context.ChangeVersion;
        Changed?.Invoke();
    }

    public void Reset()
    {
        CurrentNodeId = null;
        CurrentAudienceId = null;
        CurrentChangeVersion = null;
        LoadedSnapshotId = null;
        RequestedInitialView = null;
        CurrentContext = new KnowledgeContextViewModel(KnowledgeReadContextKind.Current);
        CurrentReadContext = new ReadContext();
        Changed?.Invoke();
    }
}
