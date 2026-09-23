using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KnowHowToAI.Server.Web.Features.Content;

/// <summary>
/// Kapselt den Crepe-Editor und hält Markdown als einzige Interop-Nutzlast.
/// Lokale Eingaben bleiben bis zur expliziten Speichern-Aktion beim Editor.
/// </summary>
public sealed partial class ContentEditor : IAsyncDisposable
{
    private const string ModulePath = "./Web/Features/Content/ContentEditor.razor.js";
    private readonly SemaphoreSlim _editorLifecycle = new(1, 1);

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private IContentWriteWorkflow ContentWriteWorkflow { get; set; } = default!;

    [Inject]
    private WorkspaceEditState WorkspaceEditState { get; set; } = default!;

    [Parameter, EditorRequired]
    public Guid NodeId { get; set; }

    [Parameter, EditorRequired]
    public string AudienceId { get; set; } = string.Empty;

    [Parameter]
    public string? Markdown { get; set; }

    [Parameter]
    public bool IsReadOnly { get; set; }

    [Parameter]
    public long? ExpectedChangeVersion { get; set; }

    [Parameter]
    public long? LoadedCurrentSnapshotId { get; set; }

    [Parameter]
    public bool FocusOnMount { get; set; }

    [Parameter]
    public bool IsWorkspaceEditor { get; set; }

    [Parameter]
    public EventCallback<ContentMutationUseCaseResult> OnMutationSucceeded { get; set; }

    private ElementReference _editorElement;
    private ElementReference _sourceElement;
    private IJSObjectReference? _module;
    private DotNetObjectReference<ContentEditor>? _selfReference;
    private (Guid NodeId, string AudienceId, bool IsReadOnly)? _mountedRequest;
    private (Guid NodeId, string AudienceId, bool IsReadOnly)? _parameterIdentity;
    private string _editorMarkdown = string.Empty;
    private string _sourceMarkdown = string.Empty;
    private string? _errorMessage;
    private IReadOnlyList<DomainWarning> _warnings = [];
    private bool _isSaving;
    private bool _pasteWasReduced;
    private bool _mountRequested = true;
    private bool _isSourceMode;
    private bool _hasLocalEditorValue;
    private bool _focusSourceAfterRender;
    private bool _focusEditorAfterMount;
    private bool _isDisposed;

    private bool IsSourceMode => _isSourceMode;

    private string SourceMarkdown
    {
        get => _sourceMarkdown;
        set
        {
            if (_sourceMarkdown == value)
                return;

            _sourceMarkdown = value;
            _hasLocalEditorValue = true;
            if (!IsReadOnly)
            {
                WorkspaceEditState.SetDirty(true, DirtySource);
                _errorMessage = null;
                _warnings = [];
            }
        }
    }

    protected override void OnParametersSet()
    {
        var identity = (NodeId, AudienceId, IsReadOnly);
        var parameterMarkdown = Markdown ?? string.Empty;
        if (_parameterIdentity != identity)
        {
            _parameterIdentity = identity;
            WorkspaceEditState.SetDirty(false, DirtySource);
            _editorMarkdown = parameterMarkdown;
            _sourceMarkdown = parameterMarkdown;
            _isSourceMode = false;
            _hasLocalEditorValue = false;
            _mountRequested = true;
            _focusEditorAfterMount = FocusOnMount;
        }
        else if (!_hasLocalEditorValue && _editorMarkdown != parameterMarkdown)
        {
            _editorMarkdown = parameterMarkdown;
            _sourceMarkdown = parameterMarkdown;
            _mountRequested = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_isDisposed)
            return;

        if (_isSourceMode)
        {
            await FocusSourceAfterRenderAsync();
            return;
        }

        if (!_mountRequested)
            return;

        await _editorLifecycle.WaitAsync();
        try
        {
            await MountRequestedEditorsAsync();
        }
        finally
        {
            _editorLifecycle.Release();
        }
    }

    private async Task FocusSourceAfterRenderAsync()
    {
        if (!_focusSourceAfterRender)
            return;

        _focusSourceAfterRender = false;
        await _sourceElement.FocusAsync();
    }

    private async Task MountRequestedEditorsAsync()
    {
        if (_isDisposed || _isSourceMode || !_mountRequested)
            return;

        do
        {
            _mountRequested = false;
            await DisposeEditorAsync();
            if (_isDisposed || _isSourceMode || !await TryMountEditorAsync())
                break;
        }
        while (_mountRequested && !_isDisposed && !_isSourceMode);
    }

    private async Task<bool> TryMountEditorAsync()
    {
        try
        {
            _module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
            if (_isDisposed || _isSourceMode)
                return false;

            _selfReference ??= DotNetObjectReference.Create(this);
            await _module.InvokeVoidAsync("mount", _editorElement, _editorMarkdown, _selfReference, IsReadOnly);
            _mountedRequest = (NodeId, AudienceId, IsReadOnly);
            await FocusEditorAfterMountAsync();
            return true;
        }
        catch (JSDisconnectedException)
        {
            _mountRequested = true;
            return false;
        }
    }

    private async Task FocusEditorAfterMountAsync()
    {
        if (!_focusEditorAfterMount || _isDisposed || _isSourceMode)
            return;

        _focusEditorAfterMount = false;
        await _module!.InvokeVoidAsync("focus", _editorElement);
    }

    internal async Task ActivateAsync()
    {
        if (_isDisposed)
            return;

        if (_isSourceMode)
        {
            await _sourceElement.FocusAsync();
            return;
        }

        if (_module is not null && _mountedRequest is not null)
            await _module.InvokeVoidAsync("activate", _editorElement);
    }

    [JSInvokable]
    public Task NotifyChangedAsync()
    {
        if (!IsReadOnly)
        {
            _hasLocalEditorValue = true;
            WorkspaceEditState.SetDirty(true, DirtySource);
            _errorMessage = null;
            _warnings = [];
        }

        return InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public Task NotifyPasteReducedAsync()
    {
        if (IsReadOnly)
            return Task.CompletedTask;

        _pasteWasReduced = true;
        return InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public Task NotifyFocusAsync() => Task.CompletedTask;

    private async Task SaveAsync()
    {
        if (_isSaving || IsReadOnly)
            return;

        _isSaving = true;
        _errorMessage = null;
        _warnings = [];
        try
        {
            string markdown;
            await _editorLifecycle.WaitAsync();
            try
            {
                if (_isDisposed)
                    return;

                markdown = _isSourceMode ? _sourceMarkdown : await ReadMarkdownAsync();
            }
            finally
            {
                _editorLifecycle.Release();
            }

            _editorMarkdown = markdown;
            var result = await ContentWriteWorkflow.SaveAsync(new SaveContentCommand(
                NodeId,
                AudienceId,
                markdown,
                ExpectedChangeVersion,
                LoadedCurrentSnapshotId));

            if (!result.IsSuccess)
            {
                _errorMessage = $"[{result.Error!.Code}] {result.Error.Message}";
                _warnings = result.Warnings;
                return;
            }

            _warnings = result.Warnings;
            WorkspaceEditState.SetDirty(false, DirtySource);
            _hasLocalEditorValue = false;
            _pasteWasReduced = false;
            await OnMutationSucceeded.InvokeAsync(result.Value!);
        }
        catch (Exception exception)
        {
            _errorMessage = $"[{exception.GetType().Name}] {exception.Message}";
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task<string> ReadMarkdownAsync()
    {
        if (_module is null)
            return _editorMarkdown;

        return await _module.InvokeAsync<string>("readMarkdown", _editorElement);
    }

    private async Task ShowSourceAsync()
    {
        await _editorLifecycle.WaitAsync();
        try
        {
            if (_isDisposed || _isSourceMode)
                return;

            _sourceMarkdown = await ReadMarkdownAsync();
            _editorMarkdown = _sourceMarkdown;
            await DisposeEditorAsync();
            _isSourceMode = true;
            _focusSourceAfterRender = true;
        }
        finally
        {
            _editorLifecycle.Release();
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task ShowWysiwygAsync()
    {
        if (!_isSourceMode)
            return;

        _editorMarkdown = _sourceMarkdown;
        _isSourceMode = false;
        _mountRequested = true;
        _focusEditorAfterMount = true;
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        await _editorLifecycle.WaitAsync();
        try
        {
            await DisposeEditorAsync();
            _selfReference?.Dispose();
            WorkspaceEditState.SetDirty(false, DirtySource);
        }
        finally
        {
            _editorLifecycle.Release();
        }
    }

    private string DirtySource => $"content:{NodeId:D}:{AudienceId}";

    private async Task DisposeEditorAsync()
    {
        if (_module is null)
            return;

        try
        {
            await _module.InvokeVoidAsync("dispose", _editorElement);
        }
        catch (JSDisconnectedException)
        {
            // The browser-side editor is gone with the disconnected circuit.
        }

        _mountedRequest = null;
    }
}
