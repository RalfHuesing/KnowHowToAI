using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;
using KnowHowToAI.Server.Web.Features.Roles;

namespace KnowHowToAI.Web.Tests.Features.Roles;

[Trait("Category", "Unit")]
public sealed class RoleMapperTests
{
    [Fact]
    public void ToRoleItemViewModel_MapsAllProperties()
    {
        var roleId = new RoleId("architect");
        var role = new Role(
            new SnapshotId(1),
            roleId,
            "Architekt",
            "Software-Architektur-Rolle",
            IsDeleted: false);

        var vm = RoleMapper.ToRoleItemViewModel(role);

        Assert.Equal(roleId.Value, vm.RoleId);
        Assert.Equal("Architekt", vm.Name);
        Assert.Equal("Software-Architektur-Rolle", vm.Description);
    }

    [Fact]
    public void ToRolePageViewModel_MapsRolesAndPreservesCursor()
    {
        var roleId = new RoleId("developer");
        var role = new Role(new SnapshotId(1), roleId, "Entwickler", null, IsDeleted: false);
        var page = new RolePage(new[] { role }, NextCursor: "role-cursor-99");

        var vm = RoleMapper.ToRolePageViewModel(page);

        Assert.Equal("role-cursor-99", vm.NextCursor);
        var item = Assert.Single(vm.Items);
        Assert.Equal(roleId.Value, item.RoleId);
        Assert.Equal("Entwickler", item.Name);
        Assert.Null(item.Description);
    }

    [Fact]
    public void ToRolePageResult_PreservesErrorsAndWarnings()
    {
        var error = new DomainError("RoleNotFound", "Rolle nicht gefunden.");
        var warning = new DomainWarning("WarningCode", "Warnhinweis.");

        var failed = Result<RolePage>.Failure(error, new[] { warning });
        var result = RoleMapper.ToRolePageResult(failed);

        Assert.False(result.IsSuccess);
        Assert.Equal("RoleNotFound", result.Error!.Code);
        Assert.Single(result.Warnings);
    }
}
