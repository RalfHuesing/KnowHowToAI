namespace KnowHowToAI.Core.Application.Mutations.Roles;

/// <summary>Stabile Fehlercodes für Rollen-Mutations-Use-Cases.</summary>
public static class RoleMutationErrorCodes
{
    public const string RoleNotFound = "RoleNotFound";
    public const string RoleInUse = "RoleInUse";
    public const string RoleNameRequired = "RoleNameRequired";
    public const string RoleIdRequired = "RoleIdRequired";

    public const string RoleIdDetail = "roleId";
    public const string RoleNameDetail = "roleName";
    public const string BlockingContentCountDetail = "blockingContentCount";
    public const string BlockingDependencyCountDetail = "blockingDependencyCount";
    public const string BlockingResolutionCountDetail = "blockingResolutionCount";
}
