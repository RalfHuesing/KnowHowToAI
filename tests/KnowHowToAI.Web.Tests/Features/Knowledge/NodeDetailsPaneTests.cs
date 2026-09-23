using Bunit;
using KnowHowToAI.Core.Application.Mutations.Content;
using KnowHowToAI.Core.Application.Mutations.Nodes;
using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Application.Policies;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Content;
using KnowHowToAI.Core.Domain.Hierarchy;
using KnowHowToAI.Core.Domain.Versioning;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Features.Content;
using KnowHowToAI.Server.Web.Features.Knowledge.Node;
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Workflow;
using KnowHowToAI.TestSupport;
using KnowHowToAI.Web.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Features.Knowledge;

[Trait("Category", "Unit")]
public sealed class NodeDetailsPaneTests : BunitContext
{
    private static readonly SnapshotId SnapshotId = new(1);
    private static readonly NodeId NodeId = new(Guid.Parse("10000000-0000-0000-0000-000000000021"));
    private static readonly AudienceId Developer = new("Developer");
    private static readonly AudienceId Reader = new("Reader");

    [Fact]
    public void IndependentCurrentDocumentStartsEditingInPlace()
    {
        var harness = CreateHarness(ContentMode.Independent, hasOwnContent: true);
        var cut = RenderPane(harness, Developer.Value);

        cut.Find("[data-testid='node-details-edit']").Click();

        Assert.Empty(cut.FindAll("[data-testid='node-edit-dialog']"));
        Assert.NotNull(cut.Find("[data-testid='node-editing-section']"));
        Assert.Equal("Titel", cut.Find("[data-testid='node-metadata-title']").GetAttribute("value"));
        Assert.NotNull(cut.Find("[data-testid='content-editor-save']"));
        Assert.False(cut.FindComponent<ContentEditor>().Instance.IsReadOnly);
    }

    [Theory]
    [InlineData("Fallback")]
    [InlineData("None")]
    public void FallbackAndMissingContentRequireExplicitEmptyIndependentDraft(string availability)
    {
        var harness = CreateHarness(
            ContentMode.Independent,
            hasOwnContent: availability == "Fallback",
            addFallbackReader: availability == "Fallback");
        var audienceId = availability == "Fallback" ? Reader.Value : Developer.Value;
        var cut = RenderPane(harness, audienceId);

        Assert.Equal("Eigene Fassung erstellen", cut.Find("[data-testid='node-details-edit']").TextContent.Trim());
        cut.Find("[data-testid='node-details-edit']").Click();

        var editor = cut.FindComponent<ContentEditor>();
        Assert.Equal(string.Empty, editor.Instance.Markdown);
        Assert.False(editor.Instance.IsReadOnly);
    }

    [Fact]
    public void DerivedAndHistoricalContentRemainReadOnly()
    {
        var derived = CreateHarness(ContentMode.Derived, hasOwnContent: true);
        var derivedPane = RenderPane(derived, Developer.Value);
        Assert.Empty(derivedPane.FindAll("[data-testid='node-details-edit']"));
        Assert.Contains("schreibgeschützt", derivedPane.Find("[data-testid='node-content-derived-context']").TextContent);

        var historical = CreateHarness(ContentMode.Independent, hasOwnContent: true);
        var historicalPane = Render<NodeDetailsPane>(parameters => parameters
            .Add(pane => pane.NodeId, NodeId.Value)
            .Add(pane => pane.ReadContext, new ReadContext(SnapshotId: new SnapshotId(2)))
            .Add(pane => pane.AudienceId, Developer.Value));
        Assert.Empty(historicalPane.FindAll("[data-testid='node-details-edit']"));
    }

    private IRenderedComponent<NodeDetailsPane> RenderPane(NavigationTestHarness harness, string audienceId)
    {
        ConfigureServices(harness, audienceId);
        return Render<NodeDetailsPane>(parameters => parameters
            .Add(pane => pane.NodeId, NodeId.Value)
            .Add(pane => pane.ReadContext, new ReadContext())
            .Add(pane => pane.AudienceId, audienceId));
    }

    private void ConfigureServices(NavigationTestHarness harness, string audienceId)
    {
        var navigation = harness.CreateService();
        var workspace = new WorkspaceState();
        var editState = new WorkspaceEditState();
        workspace.SetAudience(audienceId);
        workspace.SetLoadedSnapshotId(SnapshotId.Value);
        workspace.SetContext(new KnowledgeContextViewModel(KnowledgeReadContextKind.Current), new ReadContext());
        Services.AddSingleton(navigation);
        Services.AddSingleton(workspace);
        Services.AddSingleton(editState);
        Services.AddSingleton<WebWriteCoordinator>(serviceProvider => WebWriteTestServices.CreateCoordinator(
            harness, workspace, serviceProvider.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()));
        Services.AddSingleton(TestNodeMutations.CreateService(new InMemoryNodeMutationRepository(
            new WorkingNodeMutationState(new SnapshotId(2), [new Node(new SnapshotId(2), NodeId, null, "Titel", "Beschreibung", 0, false)], [], [], [NodeId]))));
        Services.AddSingleton(new ContentMutationApplicationService(
            new InMemoryContentMutationRepository(new WorkingContentMutationState(
                new SnapshotId(2),
                [new Node(new SnapshotId(2), NodeId, null, "Titel", "Beschreibung", 0, false)],
                [new Audience(new SnapshotId(2), Developer, "Developer", null, false)],
                [],
                [])),
            new ContentMutationService(new ContentRevisionService(new FixedIdentifierGenerator
            {
                FixedContentRevisionId = new ContentRevisionId(Guid.Parse("10000000-0000-0000-0000-000000000099"))
            })),
            TestPolicies.DefaultValidation));
        JSInterop.SetupModule("./Web/Features/Content/ContentEditor.razor.js").Mode = JSRuntimeMode.Loose;
    }

    private static NavigationTestHarness CreateHarness(ContentMode mode, bool hasOwnContent, bool addFallbackReader = false)
    {
        var harness = new NavigationTestHarness(SnapshotId);
        harness.AddNode(new Node(SnapshotId, NodeId, null, "Titel", "Beschreibung", 0, false));
        if (hasOwnContent)
        {
            harness.AddContent(new NodeContent(
                SnapshotId, NodeId, Developer, new ContentRevisionId(Guid.NewGuid()), mode, "Inhalt", false));
        }

        if (addFallbackReader)
        {
            harness.AddAudience(new Audience(SnapshotId, Reader, "Reader", null, false));
            harness.AddAudienceResolution(new AudienceResolution(SnapshotId, Reader, Developer, 1));
        }

        return harness;
    }
}
