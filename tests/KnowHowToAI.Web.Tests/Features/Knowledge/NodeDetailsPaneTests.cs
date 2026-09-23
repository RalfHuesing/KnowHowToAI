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
using KnowHowToAI.Server.Web.State;
using KnowHowToAI.Server.Web.Features.Content;
using KnowHowToAI.Server.Web.Features.Knowledge.Node;
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
    private static readonly NodeId OtherNodeId = new(Guid.Parse("10000000-0000-0000-0000-000000000022"));
    private static readonly AudienceId Developer = new("Developer");
    private static readonly AudienceId Reader = new("Reader");

    [Fact]
    public void PaneStartsOnReadAndShowsFourExclusiveViews()
    {
        var harness = CreateHarness(ContentMode.Independent, hasOwnContent: true);
        var cut = RenderPane(harness, Developer.Value);

        Assert.Equal(4, cut.FindAll("[data-testid^='node-view-']").Count(element => element.TagName == "BUTTON"));
        Assert.Equal("true", cut.Find("[data-testid='node-view-read']").GetAttribute("aria-pressed"));
        Assert.Single(cut.FindAll("h1"));
        Assert.Equal(
            ["Lesen", "Titel und Beschreibung", "Editor", "Technische Details"],
            cut.FindAll("[data-testid^='node-view-']").Where(element => element.TagName == "BUTTON").Select(element => element.TextContent.Trim()));
        Assert.Empty(cut.FindAll("[data-testid='node-metadata-editor']"));
        Assert.Empty(cut.FindAll("[data-testid='content-editor']"));

        cut.Find("[data-testid='node-view-metadata']").Click();
        Assert.NotNull(cut.Find("[data-testid='node-metadata-editor']"));
        Assert.Empty(cut.FindAll("[data-testid='content-editor']"));
        Assert.Single(cut.FindAll("h1"));

        cut.Find("[data-testid='node-view-editor']").Click();
        Assert.Equal("true", cut.Find("[data-testid='node-view-editor']").GetAttribute("aria-pressed"));
        Assert.NotNull(cut.Find("[data-testid='content-editor-save']"));
        Assert.False(cut.FindComponent<ContentEditor>().Instance.IsReadOnly);
        Assert.Single(cut.FindAll("h1"));
        Assert.True(cut.Find("[data-testid='node-view-panel-metadata']").HasAttribute("hidden"));
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

        cut.Find("[data-testid='node-view-editor']").Click();
        var editor = cut.FindComponent<ContentEditor>();
        Assert.Equal(string.Empty, editor.Instance.Markdown);
        Assert.False(editor.Instance.IsReadOnly);
    }

    [Fact]
    public void DerivedMetadataIsWritableWhileDerivedContentStaysReadOnly()
    {
        var derived = CreateHarness(ContentMode.Derived, hasOwnContent: true);
        var derivedPane = RenderPane(derived, Developer.Value);
        Assert.Equal(4, derivedPane.FindAll("[data-testid^='node-view-']").Count(element => element.TagName == "BUTTON"));
        derivedPane.Find("[data-testid='node-view-metadata']").Click();
        Assert.NotNull(derivedPane.Find("[data-testid='node-metadata-editor']"));
        derivedPane.Find("[data-testid='node-view-editor']").Click();
        Assert.Contains("schreibgeschützt", derivedPane.Find("[data-testid='node-editor-derived-readonly']").TextContent);
        Assert.Empty(derivedPane.FindAll("[data-testid='content-editor-save']"));
        Assert.Single(derivedPane.FindAll("h1"));
    }

    [Fact]
    public async Task LocalMetadataAndContentSurviveTabSwitches()
    {
        var harness = CreateHarness(ContentMode.Independent, hasOwnContent: true);
        var cut = RenderPane(harness, Developer.Value);
        cut.Find("[data-testid='node-view-metadata']").Click();
        await cut.Find("[data-testid='node-metadata-title']").InputAsync("Ungespeicherter Titel");
        cut.Find("[data-testid='node-view-editor']").Click();
        await cut.Find("[data-testid='content-editor-mode-source']").ClickAsync();
        await cut.Find("[data-testid='content-editor-source']").InputAsync("Ungespeicherter Inhalt");

        cut.Find("[data-testid='node-view-technical']").Click();
        cut.Find("[data-testid='node-view-metadata']").Click();
        Assert.Equal("Ungespeicherter Titel", cut.Find("[data-testid='node-metadata-title']").GetAttribute("value"));
        cut.Find("[data-testid='node-view-editor']").Click();
        Assert.Equal("Ungespeicherter Inhalt", cut.Find("[data-testid='content-editor-source']").GetAttribute("value"));
        Assert.True(Services.GetRequiredService<WorkspaceEditState>().IsDirty);
        Assert.Single(cut.FindAll("h1"));
    }

    [Fact]
    public void SelectingAnotherNodeRestartsOnRead()
    {
        var harness = CreateHarness(ContentMode.Independent, hasOwnContent: true);
        var cut = RenderPane(harness, Developer.Value);
        cut.Find("[data-testid='node-view-technical']").Click();

        cut.Render(parameters => parameters
            .Add(pane => pane.NodeId, OtherNodeId.Value)
            .Add(pane => pane.ReadContext, new ReadContext())
            .Add(pane => pane.AudienceId, Developer.Value));

        Assert.Equal("true", cut.Find("[data-testid='node-view-read']").GetAttribute("aria-pressed"));
        Assert.Single(cut.FindAll("h1"));
        Assert.Empty(cut.FindAll("[data-testid='node-view-panel-metadata']"));
        Assert.Equal("Zweiter Knoten", cut.Find("[data-testid='node-details-title']").TextContent.Trim());
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
        harness.AddNode(new Node(SnapshotId, OtherNodeId, NodeId, "Zweiter Knoten", null, 1, false));
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
