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
    private IJSObjectReference? _module;
    private DotNetObjectReference<ContentEditor>? _selfReference;
    private (Guid NodeId, string RoleId, string Markdown, bool IsReadOnly)? _mountedRequest;
    private string? _errorMessage;
    private bool _isSaving;
    private bool _pasteWasReduced;
    private bool _mountRequested = true;
    private bool _mountInProgress;
    private bool _isDisposed;

    protected override void OnParametersSet()
    {
        var request = (NodeId, RoleId, Markdown ?? string.Empty, IsReadOnly);
        if (_mountedRequest != request)
        {
            _mountRequested = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_isDisposed || !_mountRequested || _mountInProgress)
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
                        Markdown ?? string.Empty,
                        _selfReference,
                        IsReadOnly);
                    _mountedRequest = (NodeId, RoleId, Markdown ?? string.Empty, IsReadOnly);
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
            var markdown = await ReadMarkdownAsync();
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
            return Markdown ?? string.Empty;

        return await _module.InvokeAsync<string>("readMarkdown", _editorElement);
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
