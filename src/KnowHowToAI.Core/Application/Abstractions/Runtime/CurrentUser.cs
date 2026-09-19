namespace KnowHowToAI.Core.Application.Abstractions.Runtime;

/// <summary>
/// Repräsentiert den aktuellen Benutzer für Audit-Zwecke.
/// </summary>
public sealed record CurrentUser(string Id, string Name);
