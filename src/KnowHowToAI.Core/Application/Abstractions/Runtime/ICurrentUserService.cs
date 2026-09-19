namespace KnowHowToAI.Core.Application.Abstractions.Runtime;

/// <summary>
/// Liefert Informationen über den aktuell agierenden Benutzer.
/// Zentraler Einstiegspunkt für den Actor-Auditwert beim Erstellen von Transactions.
/// </summary>
public interface ICurrentUserService
{
    CurrentUser GetCurrentUser();

    string GetCurrentUserName() => GetCurrentUser().Name;
}
