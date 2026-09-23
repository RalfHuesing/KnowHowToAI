using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Workflow;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Server.Web.Features.Knowledge.Components;

/// <summary>Erfasst einen Root- oder Unterknoten und beginnt den sichtbaren Entwurf erst beim Speichern.</summary>
public sealed partial class RootNodeEditor : IDisposable
{
    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    [Inject]
    private WorkspaceState WorkspaceState { get; set; } = default!;

    [Inject]
    private WorkspaceEditState WorkspaceEditState { get; set; } = default!;

    [Parameter]
    public Guid? ParentNodeId { get; set; }

    [Parameter]
    public string? ParentTitle { get; set; }

    [Parameter]
    public EventCallback<NodeMutationResult> OnMutationSucceeded { get; set; }

    private string _title = string.Empty;
    private string? _description;
    private string? _errorMessage;
    private bool _isSubmitting;

    private async Task SubmitAsync()
    {
        if (_isSubmitting)
            return;

        _isSubmitting = true;
        _errorMessage = null;
        try
        {
            var nodeMutationService = ServiceProvider.GetService<NodeMutationApplicationService>();
            var writeCoordinator = ServiceProvider.GetService<WebWriteCoordinator>();
            if (nodeMutationService is null || writeCoordinator is null)
            {
                _errorMessage = "Die Knotenerstellung ist in diesem Kontext nicht verfügbar.";
                return;
            }

            var result = await writeCoordinator.WriteAsync(
                WorkspaceState.LoadedSnapshotId,
                (transactionId, changeVersion, cancellationToken) => nodeMutationService.CreateAsync(
                    transactionId,
                    new CreateNodeRequest(ParentNodeId is { } parent ? new NodeId(parent) : null, _title, _description, SortOrder: 0),
                    changeVersion,
                    cancellationToken),
                mutation => mutation.ChangeVersion);
            if (!result.Mutation.IsSuccess)
            {
                _errorMessage = $"[{result.Mutation.Error!.Code}] {result.Mutation.Error.Message}";
                return;
            }

            WorkspaceEditState.SetDirty(false);
            await OnMutationSucceeded.InvokeAsync(result.Mutation.Value!);
        }
        catch (Exception exception)
        {
            _errorMessage = $"[{exception.GetType().Name}] {exception.Message}";
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void UpdateDirty() => WorkspaceEditState.SetDirty(
        !string.IsNullOrWhiteSpace(_title) || !string.IsNullOrWhiteSpace(_description));

    private void Cancel()
    {
        _title = string.Empty;
        _description = null;
        _errorMessage = null;
        WorkspaceEditState.SetDirty(false);
    }

    public void Dispose() => WorkspaceEditState.SetDirty(false);
}
