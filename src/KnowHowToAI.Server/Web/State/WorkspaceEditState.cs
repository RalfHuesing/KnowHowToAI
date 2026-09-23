using KnowHowToAI.Core.Application.Navigation;

namespace KnowHowToAI.Server.Web.State;

/// <summary>Koordiniert lokale Dirty-Eingaben und die Versionsgrenze einer Workspace-Transaktion.</summary>
public sealed class WorkspaceEditState
{
    private readonly HashSet<string> _dirtySources = new(StringComparer.Ordinal);

    public bool IsDirty => _dirtySources.Count > 0;

    public event Action? Changed;

    public bool SetDirty(bool isDirty, string? source = null)
    {
        var wasDirty = IsDirty;
        if (string.IsNullOrWhiteSpace(source))
        {
            if (isDirty)
                _dirtySources.Add("workspace");
            else
                _dirtySources.Clear();
        }
        else if (isDirty)
        {
            _dirtySources.Add(source);
        }
        else
        {
            _dirtySources.Remove(source);
        }

        var changed = wasDirty != IsDirty;
        if (changed)
            Changed?.Invoke();
        return changed;
    }

    public void Reset()
    {
        _dirtySources.Clear();
        if (_dirtySources.Count == 0)
            return;

        _dirtySources.Clear();
        Changed?.Invoke();
    }
}
