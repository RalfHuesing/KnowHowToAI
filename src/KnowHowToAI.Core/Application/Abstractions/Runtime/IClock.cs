namespace KnowHowToAI.Core.Application.Abstractions.Runtime;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
