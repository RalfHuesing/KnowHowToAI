using Bunit;
using KnowHowToAI.Server.Web.Components.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.Web.Tests.Components.Navigation;

[Trait("Category", "Unit")]
public sealed class KnowledgeRedirectTests : BunitContext
{
    [Fact]
    public void NavigatesTheRootRouteToTheKnowledgeWorkspace()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();

        Render<KnowledgeRedirect>();

        Assert.EndsWith("/knowledge", navigation.Uri, StringComparison.Ordinal);
    }
}
