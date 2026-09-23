using Bunit;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Server.Web.Features.Content;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Features.Knowledge;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Workflow;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace KnowHowToAI.Web.Tests.Features.Content;

[Trait("Category", "Unit")]
public sealed class ContentEditorTests : BunitContext
{
    private static readonly SnapshotId SnapshotId = new(42);
    private static readonly TransactionId TransactionId = new(Guid.Parse("8dc6509e-1b47-4dd5-bc29-80e3f8cb7b7d"));
    private static readonly NodeId NodeId = new(Guid.Parse("bf70dbf1-0e6e-44dd-90dd-2dcab09f8cb0"));
    private static readonly AudienceId AudienceId = new("Developer");
    private static readonly ContentRevisionId ExistingRevisionId = new(Guid.Parse("96d88df5-44d5-4db6-98ba-59a0bb5a76f5"));
    private static readonly ContentRevisionId NewRevisionId = new(Guid.Parse("e2ad713e-dcb6-41b5-aea6-69ddcbde3d1b"));

    [Fact]
    public async Task SaveReadsMarkdownThroughInteropAndClearsDirtyAfterAcceptedMutation()
    {
        ConfigureLooseModule();
        var repository = AddServices();
        var workspace = Services.GetRequiredService<WorkspaceEditState>();
        ContentMutationUseCaseResult? mutation = null;
        var cut = Render<ContentEditor>(parameters => parameters
            .Add(editor => editor.NodeId, NodeId.Value)
            .Add(editor => editor.AudienceId, AudienceId.Value)
            .Add(editor => editor.Markdown, "Alter Inhalt")
            .Add(editor => editor.TransactionId, TransactionId)
            .Add(editor => editor.ExpectedChangeVersion, 0L)
            .Add(editor => editor.OnMutationSucceeded,
                EventCallback.Factory.Create<ContentMutationUseCaseResult>(this, value => mutation = value)));

        await cut.Instance.NotifyChangedAsync();
        await cut.InvokeAsync(() => cut.Find("[data-testid='content-editor-save']").Click());

        Assert.NotNull(mutation);
        Assert.False(workspace.IsDirty);
        Assert.Equal("Neuer Inhalt", repository.State.Contents.Single(content => !content.IsDeleted).ContentMd);
        Assert.Equal(1, mutation.ChangeVersion);
    }

    [Fact]
    public void SaveAction_IsRenderedBeforeEditorSurface()
    {
        ConfigureLooseModule();
        AddServices();
        var cut = Render<ContentEditor>(parameters => parameters
            .Add(editor => editor.NodeId, NodeId.Value)
            .Add(editor => editor.AudienceId, AudienceId.Value)
            .Add(editor => editor.Markdown, "Inhalt")
            .Add(editor => editor.TransactionId, TransactionId)
            .Add(editor => editor.ExpectedChangeVersion, 0L));

        var markup = cut.Markup;
        Assert.True(
            markup.IndexOf("data-testid=\"content-editor-save\"", StringComparison.Ordinal)
                < markup.IndexOf("data-testid=\"content-editor-surface\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SaveKeepsDirtyStateAndEditorValueWhenServerRejectsMutation()
    {
        ConfigureLooseModule();
        var repository = AddServices();
        repository.Rejection = new DomainError("ChangeVersionConflict", "Die Version ist veraltet.");
        var workspace = Services.GetRequiredService<WorkspaceEditState>();
        var cut = Render<ContentEditor>(parameters => parameters
            .Add(editor => editor.NodeId, NodeId.Value)
            .Add(editor => editor.AudienceId, AudienceId.Value)
            .Add(editor => editor.Markdown, "Ungespeicherter Inhalt")
            .Add(editor => editor.TransactionId, TransactionId)
            .Add(editor => editor.ExpectedChangeVersion, 0L));

        await cut.Instance.NotifyChangedAsync();
        await cut.InvokeAsync(() => cut.Find("[data-testid='content-editor-save']").Click());

        Assert.True(workspace.IsDirty);
        Assert.Contains("ChangeVersionConflict", cut.Markup, StringComparison.Ordinal);
        Assert.Equal("Alter Inhalt", repository.State.Contents.Single(content => !content.IsDeleted).ContentMd);
    }

    [Fact]
    public async Task SaveRejectionDoesNotRemountOrReplaceTheCompleteEditorValue()
    {
        const string rejectedEditorValue = "<span>vollständiger ungespeicherter Wert</span>\n![Bild](https://example.test/bild.png)";
        var module = ConfigureLooseModule(rejectedEditorValue);
        var repository = AddServices();
        repository.Rejection = new DomainError("RawHtmlNotAllowed", "Raw HTML ist unzulässig.");
        var workspace = Services.GetRequiredService<WorkspaceEditState>();
        var cut = Render<ContentEditor>(parameters => parameters
            .Add(editor => editor.NodeId, NodeId.Value)
            .Add(editor => editor.AudienceId, AudienceId.Value)
            .Add(editor => editor.Markdown, "Ausgangswert")
            .Add(editor => editor.TransactionId, TransactionId)
            .Add(editor => editor.ExpectedChangeVersion, 0L));

        await cut.Instance.NotifyChangedAsync();
        await cut.InvokeAsync(() => cut.Find("[data-testid='content-editor-save']").Click());

        Assert.True(workspace.IsDirty);
        Assert.Contains("RawHtmlNotAllowed", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(1, module.Invocations["mount"].Count);
        Assert.Equal("Alter Inhalt", repository.State.Contents.Single(content => !content.IsDeleted).ContentMd);
    }

    [Fact]
    public async Task ParameterChange_DisposesBeforeRemountingEditor()
    {
        var module = JSInterop.SetupModule("./Web/Features/Content/ContentEditor.razor.js");
        module.Mode = JSRuntimeMode.Strict;
        var callOrder = new List<string>();
        module.SetupVoid("mount", _ =>
        {
            callOrder.Add("mount");
            return true;
        }).SetVoidResult();
        module.SetupVoid("dispose", _ =>
        {
            callOrder.Add("dispose");
            return true;
        }).SetVoidResult();
        AddServices();

        var cut = Render<ContentEditorHost>(parameters => parameters
            .Add(host => host.NodeId, NodeId.Value)
            .Add(host => host.AudienceId, AudienceId.Value)
            .Add(host => host.Markdown, "Erster Stand"));

        cut.WaitForAssertion(() => Assert.Single(module.Invocations["mount"]));
        await cut.InvokeAsync(() => Task.CompletedTask);
        await cut.InvokeAsync(() => cut.Instance.SetMarkdown("Zweiter Stand"));

        cut.WaitForAssertion(() => Assert.Equal(2, module.Invocations["mount"].Count));
        Assert.Equal(
            ["mount", "dispose", "mount"],
            callOrder);
    }

    [Fact]
    public async Task DisconnectedMountLeavesDirtyStateUntilExplicitDispose()
    {
        var module = JSInterop.SetupModule("./Web/Features/Content/ContentEditor.razor.js");
        module.Mode = JSRuntimeMode.Strict;
        module.SetupVoid("mount", _ => true).SetException(new JSDisconnectedException("Circuit geschlossen"));
        module.SetupVoid("dispose", _ => true).SetVoidResult();
        AddServices();
        var workspace = Services.GetRequiredService<WorkspaceEditState>();
        var cut = Render<ContentEditor>(parameters => parameters
            .Add(editor => editor.NodeId, NodeId.Value)
            .Add(editor => editor.AudienceId, AudienceId.Value)
            .Add(editor => editor.Markdown, "Ungespeichert"));

        await cut.Instance.NotifyChangedAsync();
        cut.Render(parameters => parameters.Add(editor => editor.Markdown, "Reconnect"));

        Assert.True(workspace.IsDirty);
        await cut.Instance.DisposeAsync();
        Assert.False(workspace.IsDirty);
    }

    [Fact]
    public async Task ExplicitSourceModeRoundTripsMarkdownAndKeepsDirtyContext()
    {
        var module = ConfigureLooseModule("**WYSIWYG**\n\n- Eintrag");
        AddServices();
        var workspace = Services.GetRequiredService<WorkspaceEditState>();
        var cut = Render<ContentEditor>(parameters => parameters
            .Add(editor => editor.NodeId, NodeId.Value)
            .Add(editor => editor.AudienceId, AudienceId.Value)
            .Add(editor => editor.Markdown, "Ausgangswert")
            .Add(editor => editor.TransactionId, TransactionId)
            .Add(editor => editor.ExpectedChangeVersion, 0L));

        await cut.InvokeAsync(() => cut.Find("[data-testid='content-editor-mode-source']").Click());

        var source = cut.Find("[data-testid='content-editor-source']");
        Assert.Equal("**WYSIWYG**\n\n- Eintrag", source.GetAttribute("value"));
        source.Input("[Link](https://example.test)\n\nUnicode: ä");
        Assert.True(workspace.IsDirty);

        await cut.InvokeAsync(() => cut.Find("[data-testid='content-editor-mode-wysiwyg']").Click());
        cut.WaitForAssertion(() => Assert.Equal(2, module.Invocations["mount"].Count));
        Assert.Equal(
            "[Link](https://example.test)\n\nUnicode: ä",
            module.Invocations["mount"].Last().Arguments[1]);
    }

    [Fact]
    public async Task SourceModeRejectionPreservesCompleteInputAndDirtyState()
    {
        ConfigureLooseModule("Serverwert");
        var repository = AddServices();
        repository.Rejection = new DomainError("RawHtmlNotAllowed", "Raw HTML ist unzulässig.");
        var workspace = Services.GetRequiredService<WorkspaceEditState>();
        var cut = Render<ContentEditor>(parameters => parameters
            .Add(editor => editor.NodeId, NodeId.Value)
            .Add(editor => editor.AudienceId, AudienceId.Value)
            .Add(editor => editor.Markdown, "Ausgangswert")
            .Add(editor => editor.TransactionId, TransactionId)
            .Add(editor => editor.ExpectedChangeVersion, 0L));

        await cut.InvokeAsync(() => cut.Find("[data-testid='content-editor-mode-source']").Click());
        const string rejected = "<h2>Verboten</h2>\n\n![Bild](https://example.test/bild.png)";
        cut.Find("[data-testid='content-editor-source']").Input(rejected);
        await cut.InvokeAsync(() => cut.Find("[data-testid='content-editor-save']").Click());

        Assert.True(workspace.IsDirty);
        Assert.Equal(rejected, cut.Find("[data-testid='content-editor-source']").GetAttribute("value"));
        Assert.Contains("RawHtmlNotAllowed", cut.Markup, StringComparison.Ordinal);
        Assert.Equal("Alter Inhalt", repository.State.Contents.Single(content => !content.IsDeleted).ContentMd);
    }

    private InMemoryContentMutationRepository AddServices()
    {
        var repository = new InMemoryContentMutationRepository(new WorkingContentMutationState(
            SnapshotId,
            [new Node(SnapshotId, NodeId, null, "Titel", null, 0, false)],
            [new Audience(SnapshotId, AudienceId, "Developer", null, false)],
            [new NodeContent(SnapshotId, NodeId, AudienceId, ExistingRevisionId, ContentMode.Independent, "Alter Inhalt", false)],
            []));
        var workspace = new WorkspaceState();
        var editState = new WorkspaceEditState();
        workspace.SetAudience(AudienceId.Value);
        workspace.SetLoadedSnapshotId(SnapshotId.Value);
        workspace.SetChangeVersion(0);
        workspace.SetContext(
            new KnowledgeContextViewModel(KnowledgeReadContextKind.Transaction, TransactionId.Value.ToString("D"), ChangeVersion: 0),
            new ReadContext(TransactionId: TransactionId));
        Services.AddSingleton(workspace);
        Services.AddSingleton(editState);
        var writeHarness = new NavigationTestHarness(SnapshotId);
        Services.AddSingleton<WebWriteCoordinator>(serviceProvider => WebWriteTestServices.CreateCoordinator(
            writeHarness,
            workspace,
            serviceProvider.GetRequiredService<NavigationManager>()));
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

    private BunitJSModuleInterop ConfigureLooseModule(string markdown = "Neuer Inhalt")
    {
        var module = JSInterop.SetupModule("./Web/Features/Content/ContentEditor.razor.js");
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<string>("readMarkdown", _ => true).SetResult(markdown);
        return module;
    }

    private sealed class ContentEditorHost : ComponentBase
    {
        [Parameter] public Guid NodeId { get; set; }
        [Parameter] public string AudienceId { get; set; } = string.Empty;
        [Parameter] public string Markdown { get; set; } = string.Empty;

        public void SetMarkdown(string markdown)
        {
            Markdown = markdown;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<ContentEditor>(0);
            builder.AddAttribute(1, nameof(ContentEditor.NodeId), NodeId);
            builder.AddAttribute(2, nameof(ContentEditor.AudienceId), AudienceId);
            builder.AddAttribute(3, nameof(ContentEditor.Markdown), Markdown);
            builder.AddAttribute(4, nameof(ContentEditor.IsReadOnly), false);
            builder.CloseComponent();
        }
    }
}


