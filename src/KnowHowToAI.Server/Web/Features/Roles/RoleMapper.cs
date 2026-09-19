using KnowHowToAI.Core.Application.Navigation;
using KnowHowToAI.Core.Domain.Common;
using KnowHowToAI.Core.Domain.Roles;

namespace KnowHowToAI.Server.Web.Features.Roles;

/// <summary>
/// Statische Mapper-Methoden zur Überführung von Rollenergebnissen in UI-ViewModels.
/// Stellt sicher, dass Domain-Typen nicht im Rendering verwendet werden und Fehler,
/// Warnungen und Cursor vollständig erhalten bleiben.
/// </summary>
public static class RoleMapper
{
    public static RoleItemViewModel ToRoleItemViewModel(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        return new RoleItemViewModel(
            role.RoleId.Value,
            role.Name,
            role.Description);
    }

    public static RolePageViewModel ToRolePageViewModel(RolePage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        var items = page.Items.Select(ToRoleItemViewModel).ToArray();
        return new RolePageViewModel(items, page.NextCursor);
    }

    public static Result<RolePageViewModel> ToRolePageResult(Result<RolePage> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSuccess)
            return Result<RolePageViewModel>.Failure(result.Error!, result.Warnings);

        return Result<RolePageViewModel>.Success(
            result.Value is null ? null : ToRolePageViewModel(result.Value),
            result.Warnings);
    }
}
