using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Audiences;
using KnowHowToAI.Server.Web.Features.Audiences;

namespace KnowHowToAI.Web.Tests.Features.Audiences;

[Trait("Category", "Unit")]
public sealed class AudienceMapperTests
{
    [Fact]
    public void ToAudienceItemViewModel_MapsAllProperties()
    {
        var audienceId = new AudienceId("architect");
        var audience = new Audience(
            new SnapshotId(1),
            audienceId,
            "Architekt",
            "Software-Architektur-Zielgruppe",
            IsDeleted: false);

        var vm = AudienceMapper.ToAudienceItemViewModel(audience);

        Assert.Equal(audienceId.Value, vm.AudienceId);
        Assert.Equal("Architekt", vm.Name);
        Assert.Equal("Software-Architektur-Zielgruppe", vm.Description);
    }

    [Fact]
    public void ToAudiencePageViewModel_MapsAudiencesAndPreservesCursor()
    {
        var audienceId = new AudienceId("developer");
        var audience = new Audience(new SnapshotId(1), audienceId, "Entwickler", null, IsDeleted: false);
        var page = new AudiencePage(new[] { audience }, NextCursor: "audience-cursor-99");

        var vm = AudienceMapper.ToAudiencePageViewModel(page);

        Assert.Equal("audience-cursor-99", vm.NextCursor);
        var item = Assert.Single(vm.Items);
        Assert.Equal(audienceId.Value, item.AudienceId);
        Assert.Equal("Entwickler", item.Name);
        Assert.Null(item.Description);
    }

    [Fact]
    public void ToAudiencePageResult_PreservesErrorsAndWarnings()
    {
        var error = new DomainError("AudienceNotFound", "Zielgruppe nicht gefunden.");
        var warning = new DomainWarning("WarningCode", "Warnhinweis.");

        var failed = Result<AudiencePage>.Failure(error, new[] { warning });
        var result = AudienceMapper.ToAudiencePageResult(failed);

        Assert.False(result.IsSuccess);
        Assert.Equal("AudienceNotFound", result.Error!.Code);
        Assert.Single(result.Warnings);
    }
}
