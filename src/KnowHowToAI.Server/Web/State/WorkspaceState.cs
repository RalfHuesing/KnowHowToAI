using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.Components.Layout.Context;

namespace KnowHowToAI.Server.Web.State;

/// <summary>
/// Flüchtiger Circuit-State für die Blazor-Sitzung.
/// Hält den aus Route und Query rekonstruierten aktuellen Arbeits- und Lesekontext
/// (Node, Rolle, Read Context und ChangeVersion) ohne fachliche Wahrheit.
/// </summary>
public sealed class WorkspaceState
{
    public Guid? CurrentNodeId { get; private set; }

    public string? CurrentRoleId { get; private set; }

    public long? CurrentChangeVersion { get; private set; }

    public KnowledgeContextViewModel CurrentContext { get; private set; } =
        new(KnowledgeReadContextKind.Current);

    public ReadContext CurrentReadContext { get; private set; } = new();

    public bool HasActiveTransaction => CurrentReadContext.TransactionId.HasValue;

    public TransactionId? ActiveTransactionId => CurrentReadContext.TransactionId;

    public bool IsDirty => CurrentContext.IsDirty;

    public event Action? Changed;

    public void SetDirty(bool isDirty)
    {
        if (CurrentContext.IsDirty == isDirty)
            return;

        CurrentContext = CurrentContext with { IsDirty = isDirty };
        Changed?.Invoke();
    }

    public void SetNode(Guid? nodeId)
    {
        if (CurrentNodeId == nodeId)
            return;

        CurrentNodeId = nodeId;
        Changed?.Invoke();
    }

    public void SetRole(string? roleId)
    {
        if (string.Equals(CurrentRoleId, roleId, StringComparison.Ordinal))
            return;

        CurrentRoleId = roleId;
        Changed?.Invoke();
    }

    public void SetChangeVersion(long? changeVersion)
    {
        if (CurrentChangeVersion == changeVersion)
            return;

        CurrentChangeVersion = changeVersion;
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
        Changed?.Invoke();
    }

    public void Reset()
    {
        CurrentNodeId = null;
        CurrentRoleId = null;
        CurrentChangeVersion = null;
        CurrentContext = new KnowledgeContextViewModel(KnowledgeReadContextKind.Current);
        CurrentReadContext = new ReadContext();
        Changed?.Invoke();
    }
}
