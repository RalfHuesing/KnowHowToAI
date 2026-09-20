namespace KnowHowToAI.Core.Application.Mutations.Audiences;

/// <summary>Stabile Fehlercodes für Zielgruppen-Mutations-Use-Cases.</summary>
public static class AudienceMutationErrorCodes
{
    public const string AudienceNotFound = "AudienceNotFound";
    public const string AudienceInUse = "AudienceInUse";
    public const string AudienceNameRequired = "AudienceNameRequired";
    public const string AudienceIdRequired = "AudienceIdRequired";

    public const string AudienceIdDetail = "audienceId";
    public const string AudienceNameDetail = "audienceName";
    public const string BlockingContentCountDetail = "blockingContentCount";
    public const string BlockingDependencyCountDetail = "blockingDependencyCount";
    public const string BlockingResolutionCountDetail = "blockingResolutionCount";
}
