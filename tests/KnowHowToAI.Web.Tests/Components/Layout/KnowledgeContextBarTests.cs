using Bunit;
using KnowHowToAI.Server.Web.Components.Layout.Context;
using KnowHowToAI.Server.Web.Components.Shared.Feedback;

namespace KnowHowToAI.Web.Tests.Components.Layout;

[Trait("Category", "Unit")]
public sealed class KnowledgeContextBarTests : BunitContext
{
    [Theory]
    [InlineData(KnowledgeReadContextKind.Current, "Current Snapshot")]
    [InlineData(KnowledgeReadContextKind.Snapshot, "Snapshot")]
    [InlineData(KnowledgeReadContextKind.Transaction, "Transaction")]
    [InlineData(KnowledgeReadContextKind.Release, "Release")]
    public void RendersEveryReadContextKindAsPlainTextWithoutSelectorsLinksOrMutation(
        KnowledgeReadContextKind kind, string expectedText)
    {
        var cut = RenderBar(new KnowledgeContextViewModel(kind));

        Assert.Contains(expectedText, cut.Find(".knowledge-context").TextContent, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".knowledge-context a, .knowledge-context select, .knowledge-context button"));
    }

    [Theory]
    [InlineData(null, "Keine Rolle ausgewählt")]
    [InlineData("Developer", "Developer")]
    public void RendersTheSelectedRoleOrANeutralMissingState(string? roleName, string expectedText)
    {
        var cut = RenderBar(new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Current, RoleName: roleName));

        Assert.Contains(expectedText, cut.Find(".knowledge-context").TextContent, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void ShowsTheUnsavedChangeStateOnlyWhenDirty(bool isDirty, int expectedCount)
    {
        var cut = RenderBar(new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Current, IsDirty: isDirty));

        Assert.Equal(isDirty ? "true" : "false", cut.Find(".knowledge-context").GetAttribute("data-ktai-dirty"));
        Assert.Equal(expectedCount, cut.FindAll(".app-status--ungespeichert").Count);
        if (isDirty)
        {
            var status = cut.Find(".app-status--ungespeichert");
            Assert.Contains("Ungespeicherte Änderungen", status.TextContent, StringComparison.Ordinal);

            var icon = status.QuerySelector(".app-status__icon");
            Assert.NotNull(icon);
            Assert.Equal("true", icon!.GetAttribute("aria-hidden"));
        }
    }

    [Fact]
    public void PrefersTheProvidedNameOverTheProvidedIdentifier()
    {
        var cut = RenderBar(new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Release,
            ContextId: "42",
            DisplayName: "Freigabe Herbst"));

        var group = cut.Find(".knowledge-context");
        Assert.Contains("Freigabe Herbst", group.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("42", group.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ShowsTheProvidedIdentifierWhenNoNameExists()
    {
        var cut = RenderBar(new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Snapshot, ContextId: "42"));

        Assert.Contains("42", cut.Find(".knowledge-context__detail").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersNoContextDetailAndNoInventedDataWithoutProvidedNameOrId()
    {
        var cut = RenderBar(new KnowledgeContextViewModel(KnowledgeReadContextKind.Current));

        Assert.Empty(cut.FindAll(".knowledge-context__detail"));
        Assert.DoesNotContain("Keine Bezeichnung", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void GroupsTheValuesWithAnAccessibleNameInsteadOfColorCoding()
    {
        var cut = RenderBar(new KnowledgeContextViewModel(KnowledgeReadContextKind.Current));

        var group = cut.Find(".knowledge-context");
        Assert.Equal("group", group.GetAttribute("role"));
        Assert.Equal("Wissenskontext", group.GetAttribute("aria-label"));
    }

    [Fact]
    public void RendersTheBaseSnapshotWhenProvided()
    {
        var cut = RenderBar(new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Transaction,
            ContextId: "tx-1",
            BaseSnapshotId: 42L));

        var baseSnapElement = cut.Find("[data-testid='context-base-snapshot']");
        Assert.NotNull(baseSnapElement);
        Assert.Contains("Base-Snapshot:", baseSnapElement.TextContent, StringComparison.Ordinal);
        Assert.Contains("42", baseSnapElement.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void RendersTheWorkingChangeVersionWhenProvided()
    {
        var cut = RenderBar(new KnowledgeContextViewModel(
            KnowledgeReadContextKind.Transaction,
            ChangeVersion: 7L));

        Assert.Contains("7", cut.Find("[data-testid='context-change-version']").TextContent, StringComparison.Ordinal);
    }

    private IRenderedComponent<KnowledgeContextBar> RenderBar(KnowledgeContextViewModel context) =>
        Render<KnowledgeContextBar>(parameters => parameters.Add(parameter => parameter.Context, context));
}
