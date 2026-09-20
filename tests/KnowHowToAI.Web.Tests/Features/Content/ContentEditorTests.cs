using Bunit;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Server.Web.Features.Content;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Content;

[Trait("Category", "Unit")]
public sealed class ContentEditorTests : BunitContext
{
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly TransactionId TransactionId = new(Guid.Parse("8dc6509e-1b47-4dd5-bc29-80e3f8cb7b7d"));
    private static readonly NodeId NodeId = new(Guid.Parse("bf70dbf1-0e6e-44dd-90dd-2dcab09f8cb0"));
    private static readonly RoleId RoleId = new("Developer");
    private static readonly ContentRevisionId ExistingRevisionId = new(Guid.Parse("96d88df5-44d5-4db6-98ba-59a0bb5a76f5"));
    private static readonly ContentRevisionId NewRevisionId = new(Guid.Parse("e2ad713e-dcb6-41b5-aea6-69ddcbde3d1b"));

    public ContentEditorTests()
    {
        var module = JSInterop.SetupModule("./Web/Features/Content/ContentEditor.razor.js");
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<string>("readMarkdown", _ => true).SetResult("Neuer Inhalt");
    }

    [Fact]
    public async Task SaveReadsMarkdownThroughInteropAndClearsDirtyAfterAcceptedMutation()
    {
        var repository = AddServices();
        var workspace = Services.GetRequiredService<WorkspaceState>();
        ContentMutationUseCaseResult? mutation = null;
        var cut = Render<ContentEditor>(parameters => parameters
            .Add(editor => editor.NodeId, NodeId.Value)
            .Add(editor => editor.RoleId, RoleId.Value)
            .Add(editor => editor.Markdown, "Alter Inhalt")
            .Add(editor => editor.TransactionId, TransactionId)
            .Add(editor => editor.ExpectedChangeVersion, 0L)
            .Add(editor => editor.OnMutationSucceeded,
                EventCallback.Factory.Create<ContentMutationUseCaseResult>(this, value => mutation = value)));

        workspace.SetDirty(true);
        await cut.InvokeAsync(() => cut.Find("[data-testid='content-editor-save']").Click());

        Assert.NotNull(mutation);
        Assert.False(workspace.IsDirty);
        Assert.Equal("Neuer Inhalt", repository.State.Contents.Single(content => !content.IsDeleted).ContentMd);
        Assert.Equal(1, mutation.ChangeVersion);
    }

    [Fact]
    public async Task SaveKeepsDirtyStateAndEditorValueWhenServerRejectsMutation()
    {
        var repository = AddServices();
        repository.Rejection = new DomainError("ChangeVersionConflict", "Die Version ist veraltet.");
        var workspace = Services.GetRequiredService<WorkspaceState>();
        var cut = Render<ContentEditor>(parameters => parameters
            .Add(editor => editor.NodeId, NodeId.Value)
            .Add(editor => editor.RoleId, RoleId.Value)
            .Add(editor => editor.Markdown, "Ungespeicherter Inhalt")
            .Add(editor => editor.TransactionId, TransactionId)
            .Add(editor => editor.ExpectedChangeVersion, 0L));

        workspace.SetDirty(true);
        await cut.InvokeAsync(() => cut.Find("[data-testid='content-editor-save']").Click());

        Assert.True(workspace.IsDirty);
        Assert.Contains("ChangeVersionConflict", cut.Markup, StringComparison.Ordinal);
        Assert.Equal("Alter Inhalt", repository.State.Contents.Single(content => !content.IsDeleted).ContentMd);
    }

    private InMemoryContentMutationRepository AddServices()
    {
        var repository = new InMemoryContentMutationRepository(new WorkingContentMutationState(
            SnapshotId,
            [new Node(SnapshotId, NodeId, null, "Titel", null, 0, false)],
            [new Role(SnapshotId, RoleId, "Developer", null, false)],
            [new NodeContent(SnapshotId, NodeId, RoleId, ExistingRevisionId, ContentMode.Independent, "Alter Inhalt", false)],
            []));
        Services.AddSingleton(new WorkspaceState());
        Services.AddSingleton(new ContentMutationApplicationService(
            repository,
            new ContentMutationService(new ContentRevisionService(new FixedIdentifierGenerator
            {
                FixedContentRevisionId = NewRevisionId
            })),
            new ValidationPolicy
            {
                ContentSizeWarningBytes = 4096,
                ChildCountWarning = 25,
                HierarchyDepthWarning = 8,
                PossibleEmbeddedHeadingWarning = true
            }));
        return repository;
    }
}
