namespace KnowHowToAI.Core.Domain.Hierarchy;

/// <summary>Unveränderliche Persistenzgrenzen und Fehlercodes der Node-Stammdaten.</summary>
public static class NodeMetadataValidation
{
    public const string TitleTooLong = "TitleTooLong";
    public const string DescriptionTooLong = "DescriptionTooLong";
    public const int TitleMaximumLength = 200;
    public const int DescriptionMaximumLength = 1000;
}
