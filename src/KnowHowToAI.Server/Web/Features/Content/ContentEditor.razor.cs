using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.State;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace KnowHowToAI.Server.Web.Features.Content;

/// <summary>
/// Kapselt den Crepe-Editor und hält Markdown als einzige Interop-Nutzlast.
/// Die Persistenz erfolgt ausschließlich über die explizite Speichern-Aktion.
/// </summary>
public sealed partial class ContentEditor : IAsyncDisposable
{
    private const string ModulePath = "./Web/Features/Content/ContentEditor.razor.js";

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    [Inject]
    public WorkspaceState WorkspaceState { get; set; } = default!;

    [Parameter, EditorRequired]
    public Guid NodeId { get; set; }

    [Parameter, EditorRequired]
    public string RoleId { get; set; } = string.Empty;

    [Parameter]
    public string? Markdown { get; set; }

    [Parameter]
    public bool IsReadOnly { get; set; }

    [Parameter]
    public TransactionId? TransactionId { get; set; }

    [Parameter]
    public long? ExpectedChangeVersion { get; set; }

    [Parameter]
    public EventCallback<ContentMutationUseCaseResult> OnMutationSucceeded { get; set; }

    private ElementReference _editorElement;
    private ElementReference _sourceElement;
    private IJSObjectReference? _module;
    private DotNetObjectReference<ContentEditor>? _selfReference;
    private (Guid NodeId, string RoleId, bool IsReadOnly)? _mountedRequest;
    private (Guid NodeId, string RoleId, bool IsReadOnly)? _parameterIdentity;
    private string _editorMarkdown = string.Empty;
    private string _sourceMarkdown = string.Empty;
    private string? _errorMessage;
    private bool _isSaving;
    private bool _pasteWasReduced;
    private bool _mountRequested = true;
    private bool _mountInProgress;
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
                WorkspaceState.SetDirty(true);
                _errorMessage = null;
            }
        }
    }

    protected override void OnParametersSet()
    {
        var identity = (NodeId, RoleId, IsReadOnly);
        var parameterMarkdown = Markdown ?? string.Empty;
        if (_parameterIdentity != identity)
        {
            _parameterIdentity = identity;
            _editorMarkdown = parameterMarkdown;
            _sourceMarkdown = parameterMarkdown;
            _isSourceMode = false;
            _hasLocalEditorValue = false;
            _mountRequested = true;
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
        if (_isDisposed || _mountInProgress)
            return;

        if (_isSourceMode)
        {
            if (_focusSourceAfterRender)
            {
                _focusSourceAfterRender = false;
                await _sourceElement.FocusAsync();
            }

            return;
        }

        if (!_mountRequested)
            return;

        _mountInProgress = true;
        try
        {
            do
            {
                _mountRequested = false;
                await DisposeEditorAsync();

                try
                {
                    _module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
                    _selfReference ??= DotNetObjectReference.Create(this);
                    await _module.InvokeVoidAsync(
                        "mount",
                        _editorElement,
                        _editorMarkdown,
                        _selfReference,
                        IsReadOnly);
                    _mountedRequest = (NodeId, RoleId, IsReadOnly);
                    if (_focusEditorAfterMount)
                    {
                        _focusEditorAfterMount = false;
                        await _module.InvokeVoidAsync("focus", _editorElement);
                    }
                }
                catch (JSDisconnectedException)
                {
                    _mountRequested = true;
                    break;
                }
            }
            while (_mountRequested && !_isDisposed);
        }
        finally
        {
            _mountInProgress = false;
        }
    }

    [JSInvokable]
    public Task NotifyChangedAsync()
    {
        if (!IsReadOnly)
        {
            _hasLocalEditorValue = true;
            WorkspaceState.SetDirty(true);
            _errorMessage = null;
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
        if (_isSaving || IsReadOnly || !TransactionId.HasValue || ExpectedChangeVersion is not { } changeVersion)
            return;

        _isSaving = true;
        _errorMessage = null;
        try
        {
            var markdown = _isSourceMode ? _sourceMarkdown : await ReadMarkdownAsync();
            _editorMarkdown = markdown;
            var result = await ServiceProvider.GetRequiredService<ContentMutationApplicationService>().ReplaceContentAsync(
                TransactionId.Value,
                new ReplaceContentRequest(
                    new NodeId(NodeId),
                    new RoleId(RoleId),
                    ContentMode.Independent,
                    markdown,
                    [],
                    changeVersion),
                CancellationToken.None);

            if (!result.IsSuccess)
            {
                _errorMessage = $"[{result.Error!.Code}] {result.Error.Message}";
                return;
            }

            WorkspaceState.SetDirty(false);
            _hasLocalEditorValue = false;
            _pasteWasReduced = false;
            await OnMutationSucceeded.InvokeAsync(result.Value!);
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
        if (_isSourceMode)
            return;

        _sourceMarkdown = await ReadMarkdownAsync();
        _editorMarkdown = _sourceMarkdown;
        await DisposeEditorAsync();
        _isSourceMode = true;
        _focusSourceAfterRender = true;
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
        await DisposeEditorAsync();
        _selfReference?.Dispose();
        WorkspaceState.SetDirty(false);
    }

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
